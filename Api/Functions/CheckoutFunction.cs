using FluentValidation;
using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Helpers;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class CheckoutFunction(
    AppDbContext db,
    IValidator<CheckoutRequest> validator,
    IConfiguration config,
    ILogger<CheckoutFunction> logger)
{
    [Function("CreateCheckoutSession")]
    public async Task<HttpResponseData> CreateSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkout/create-session")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        CheckoutRequest? request;
        try { request = await req.ReadFromJsonAsync<CheckoutRequest>(); }
        catch { return await Responses.BadRequest(req, "Invalid request body."); }
        if (request is null) return await Responses.BadRequest(req, "Request body is required.");

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

        // Load products and validate availability
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return await Responses.BadRequest(req, $"Product not found.");
            if (!product.IsAvailable)
                return await Responses.BadRequest(req, $"{product.Name} is no longer available.");
            if (product.StockQuantity.HasValue && product.StockQuantity < item.Quantity)
                return await Responses.BadRequest(req, $"Insufficient stock for {product.Name}.");
        }

        // Calculate totals
        var settings = await db.DeliverySettings.FindAsync(1) ?? new DeliverySettings();
        var subtotal = request.Items.Sum(i => products[i.ProductId].Price * i.Quantity);
        var deliveryType = Enum.Parse<DeliveryType>(request.DeliveryType);
        var deliveryFee = deliveryType == DeliveryType.Collection
            ? 0m
            : (settings.FreeDeliveryThreshold.HasValue && subtotal >= settings.FreeDeliveryThreshold
                ? 0m
                : settings.LocalDeliveryFee);
        var total = subtotal + deliveryFee;

        // Create pending order record
        var orderNumber = await OrderNumberHelper.GenerateAsync(db, ct);
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerFirstName = request.FirstName,
            CustomerLastName = request.LastName,
            CustomerEmail = request.Email,
            CustomerPhone = request.Phone,
            DeliveryType = deliveryType,
            DeliveryAddress = request.DeliveryAddress,
            DeliveryPostcode = request.DeliveryPostcode,
            CustomerNotes = request.Notes,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = total,
            Status = OrderStatus.Pending,
            Lines = request.Items.Select(i => new OrderLine
            {
                ProductId = i.ProductId,
                ProductName = products[i.ProductId].Name,
                ProductImageUrl = products[i.ProductId].ImageUrls.FirstOrDefault(),
                UnitPrice = products[i.ProductId].Price,
                Quantity = i.Quantity,
                LineTotal = products[i.ProductId].Price * i.Quantity
            }).ToList()
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // Create Stripe Checkout Session
        StripeConfiguration.ApiKey = config["STRIPE_SECRET_KEY"];
        var siteUrl = config["SITE_URL"] ?? "https://www.katiesgarden.uk";

        var lineItems = request.Items.Select(i => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "gbp",
                UnitAmount = (long)(products[i.ProductId].Price * 100),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = products[i.ProductId].Name,
                    Images = products[i.ProductId].ImageUrls.Take(1).ToList()
                }
            },
            Quantity = i.Quantity
        }).ToList();

        if (deliveryFee > 0)
        {
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "gbp",
                    UnitAmount = (long)(deliveryFee * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Local Delivery"
                    }
                },
                Quantity = 1
            });
        }

        var sessionOptions = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = $"{siteUrl}/order/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{siteUrl}/cart",
            CustomerEmail = request.Email,
            Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: ct);

        order.StripeSessionId = session.Id;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created Stripe session {SessionId} for order {OrderNumber}", session.Id, orderNumber);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { url = session.Url });
        return response;
    }
}
