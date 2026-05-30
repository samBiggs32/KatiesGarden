using KatiesGarden.Api.Auditing;
using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Functions.Orchestration;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class AdminOrderFunction(
    AppDbContext db,
    IAuditService audit,
    RefundService refundService,
    ILogger<AdminOrderFunction> logger)
{
    [Function("AdminGetOrders")]
    public async Task<HttpResponseData> GetOrders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/orders")] HttpRequestData req)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var statusFilter = req.GetQueryParam("status");

        var ordersQuery = db.Orders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, out var status))
            ordersQuery = ordersQuery.Where(o => o.Status == status);

        var orders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Take(200)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerFirstName = o.CustomerFirstName,
                CustomerLastName = o.CustomerLastName,
                CustomerEmail = o.CustomerEmail,
                DeliveryType = o.DeliveryType.ToString(),
                Total = o.Total,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(orders);
        return response;
    }

    [Function("AdminGetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/orders/{id:guid}")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var dto = new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerFirstName = order.CustomerFirstName,
            CustomerLastName = order.CustomerLastName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            DeliveryType = order.DeliveryType.ToString(),
            DeliveryAddress = order.DeliveryAddress,
            DeliveryPostcode = order.DeliveryPostcode,
            CustomerNotes = order.CustomerNotes,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            Status = order.Status.ToString(),
            AdminNotes = order.AdminNotes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Lines = order.Lines.Select(l => new OrderLineDto(
                l.Id, l.ProductId, l.ProductName, l.ProductImageUrl,
                l.UnitPrice, l.Quantity, l.LineTotal)).ToList()
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    [Function("AdminUpdateOrderStatus")]
    public async Task<HttpResponseData> UpdateStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "manage/orders/{id:guid}/status")] HttpRequestData req,
        Guid id,
        [DurableClient] DurableTaskClient durableClient)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var request = await req.ReadFromJsonAsync<UpdateOrderStatusRequest>();
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
            return await Responses.BadRequest(req, $"Unknown status: {request.Status}");

        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderNumber} status updated from {From} to {To}",
            order.OrderNumber, previousStatus, newStatus);

        // Raise external event on the Durable orchestration so it can record history + send email
        if (!string.IsNullOrWhiteSpace(order.OrchestrationInstanceId))
        {
            var actor = SwaAuth.GetPrincipal(req);
            try
            {
                await durableClient.RaiseEventAsync(
                    order.OrchestrationInstanceId,
                    "StatusChanged",
                    new OrderStatusChangedEvent(
                        request.Status,
                        request.Note,
                        actor?.UserDetails ?? "Admin"),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not raise StatusChanged event on orchestration {InstanceId}", order.OrchestrationInstanceId);
            }
        }

        // Audit the status change
        var principal = SwaAuth.GetPrincipal(req);
        await audit.LogAsync("StatusChanged", "Order", order.Id.ToString(),
            principal?.UserDetails, principal?.UserDetails,
            new { from = previousStatus.ToString(), to = newStatus.ToString(), note = request.Note },
            ct);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("AdminUpdateOrderNotes")]
    public async Task<HttpResponseData> UpdateNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/orders/{id:guid}/notes")] HttpRequestData req,
        Guid id)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders.FindAsync([id], ct);
        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var request = await req.ReadFromJsonAsync<AdminNotesRequest>();
        order.AdminNotes = request?.Notes;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var principal = SwaAuth.GetPrincipal(req);
        await audit.LogAsync("NotesUpdated", "Order", order.Id.ToString(),
            principal?.UserDetails, principal?.UserDetails, null, ct);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("AdminRefundOrder")]
    public async Task<HttpResponseData> RefundOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/orders/{id:guid}/refund")] HttpRequestData req,
        Guid id,
        [DurableClient] DurableTaskClient durableClient)
    {
        if (req.RequireAdmin() is { } deny) return deny;

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            return await Responses.BadRequest(req, "This order has no payment intent — it cannot be refunded via Stripe.");

        var refundableStatuses = new[] {
            OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.ReadyForCollection,
            OrderStatus.Dispatched, OrderStatus.Delivered
        };
        if (!refundableStatuses.Contains(order.Status))
            return await Responses.BadRequest(req, $"Orders with status '{order.Status}' cannot be refunded.");

        try
        {
            await refundService.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = order.StripePaymentIntentId
            }, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe refund failed for order {OrderNumber}", order.OrderNumber);
            return await Responses.BadRequest(req, $"Stripe refund failed: {ex.StripeError?.Message ?? ex.Message}");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Refunded;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderNumber} refunded via Stripe", order.OrderNumber);

        if (!string.IsNullOrWhiteSpace(order.OrchestrationInstanceId))
        {
            var actor = SwaAuth.GetPrincipal(req);
            try
            {
                await durableClient.RaiseEventAsync(
                    order.OrchestrationInstanceId,
                    "StatusChanged",
                    new OrderStatusChangedEvent(nameof(OrderStatus.Refunded), "Refunded via Stripe admin", actor?.UserDetails ?? "Admin"),
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not raise StatusChanged event for refund on {InstanceId}", order.OrchestrationInstanceId);
            }
        }

        var principal = SwaAuth.GetPrincipal(req);
        await audit.LogAsync("Refunded", "Order", order.Id.ToString(),
            principal?.UserDetails, principal?.UserDetails,
            new { from = previousStatus.ToString(), stripePaymentIntentId = order.StripePaymentIntentId },
            ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { status = "Refunded", orderNumber = order.OrderNumber });
        return response;
    }
}
