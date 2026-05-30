using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Email;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class AdminOrderFunction(
    AppDbContext db,
    IEmailSender emailSender,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<AdminOrderFunction> logger)
{
    [Function("AdminGetOrders")]
    public async Task<HttpResponseData> GetOrders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/orders")] HttpRequestData req)
    {
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var statusFilter = query["status"];

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
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders
            .Include(o => o.Lines)
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
        Guid id)
    {
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var request = await req.ReadFromJsonAsync<UpdateOrderStatusRequest>();
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        if (!Enum.TryParse<OrderStatus>(request.Status, out var newStatus))
            return await Responses.BadRequest(req, $"Unknown status: {request.Status}");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderNumber} status updated to {Status}", order.OrderNumber, newStatus);

        // Send status update email to customer for relevant transitions
        var notifyStatuses = new[] {
            OrderStatus.Confirmed, OrderStatus.ReadyForCollection,
            OrderStatus.Dispatched, OrderStatus.Delivered, OrderStatus.Cancelled
        };

        if (notifyStatuses.Contains(newStatus))
        {
            try
            {
                var smtp = smtpOptions.Value;
                var email = OrderEmailBuilder.BuildStatusUpdate(order, smtp.EffectiveSenderEmail, "Katie's Garden");
                await emailSender.SendAsync(email, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send status update email for order {OrderNumber}", order.OrderNumber);
            }
        }

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("AdminUpdateOrderNotes")]
    public async Task<HttpResponseData> UpdateNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/orders/{id:guid}/notes")] HttpRequestData req,
        Guid id)
    {
        if (!SwaAuth.IsAdmin(req)) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;
        var order = await db.Orders.FindAsync([id], ct);
        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var request = await req.ReadFromJsonAsync<AdminNotesRequest>();
        order.AdminNotes = request?.Notes;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}

internal record AdminNotesRequest(string? Notes);
