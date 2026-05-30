using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class CustomerFunction(AppDbContext db, ILogger<CustomerFunction> logger)
{
    // Returns orders for the authenticated customer (matched by SWA userId).
    [Function("CustomerGetOrders")]
    public async Task<HttpResponseData> GetOrders(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer/orders")] HttpRequestData req)
    {
        var customer = SwaAuth.GetAuthenticatedUser(req);
        if (customer is null) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;

        var orders = await db.Orders
            .Where(o => o.CustomerId == customer.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new CustomerOrderSummaryDto(
                o.Id, o.OrderNumber, o.Total, o.Status.ToString(), o.DeliveryType.ToString(), o.CreatedAt))
            .ToListAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(orders);
        return response;
    }

    // Returns full order detail including status timeline for authenticated customers.
    [Function("CustomerGetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer/orders/{orderNumber}")] HttpRequestData req,
        string orderNumber)
    {
        var customer = SwaAuth.GetAuthenticatedUser(req);
        if (customer is null) return req.CreateResponse(HttpStatusCode.Unauthorized);

        var ct = req.FunctionContext.CancellationToken;

        var order = await db.Orders
            .Include(o => o.Lines)
            .Include(o => o.StatusHistory.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.CustomerId == customer.UserId, ct);

        if (order is null) return req.CreateResponse(HttpStatusCode.NotFound);

        var dto = MapToDetailDto(order);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dto);
        return response;
    }

    // Guest order lookup by order number + email. Links the order to the authenticated user
    // if they are signed in, so future visits show it in their order history.
    [Function("CustomerLinkOrder")]
    public async Task<HttpResponseData> LinkOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/link-order")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        var request = await req.ReadFromJsonAsync<LinkOrderRequest>();
        if (request is null || string.IsNullOrWhiteSpace(request.OrderNumber) || string.IsNullOrWhiteSpace(request.Email))
            return await Responses.BadRequest(req, "Order number and email are required.");

        var order = await db.Orders.FirstOrDefaultAsync(
            o => o.OrderNumber == request.OrderNumber &&
                 o.CustomerEmail == request.Email.Trim().ToLowerInvariant(),
            ct);

        if (order is null)
        {
            // Return 404 but don't reveal whether the order number or email is wrong
            logger.LogInformation("Guest order lookup failed for {OrderNumber}", request.OrderNumber);
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        // If the caller is authenticated, link the order to their identity
        var customer = SwaAuth.GetAuthenticatedUser(req);
        if (customer is not null && string.IsNullOrWhiteSpace(order.CustomerId))
        {
            order.CustomerId = customer.UserId;
            order.CustomerIdentityProvider = customer.IdentityProvider;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Order {OrderNumber} linked to customer {CustomerId}", order.OrderNumber, customer.UserId);
        }

        // Load full detail for the response
        await db.Entry(order).Collection(o => o.Lines).LoadAsync(ct);
        await db.Entry(order).Collection(o => o.StatusHistory).Query()
            .OrderBy(h => h.ChangedAt).LoadAsync(ct);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(MapToDetailDto(order));
        return response;
    }

    private static CustomerOrderDetailDto MapToDetailDto(Models.Entities.Order order) =>
        new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerFirstName = order.CustomerFirstName,
            CustomerLastName = order.CustomerLastName,
            CustomerEmail = order.CustomerEmail,
            DeliveryType = order.DeliveryType.ToString(),
            DeliveryAddress = order.DeliveryAddress,
            DeliveryPostcode = order.DeliveryPostcode,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            Total = order.Total,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Lines = order.Lines.Select(l => new OrderLineDto(
                l.Id, l.ProductId, l.ProductName, l.ProductImageUrl,
                l.UnitPrice, l.Quantity, l.LineTotal)).ToList(),
            StatusHistory = order.StatusHistory.Select(h => new OrderStatusHistoryDto(
                h.Status.ToString(), h.Note, h.ChangedAt)).ToList()
        };
}

internal record LinkOrderRequest(string OrderNumber, string Email);
