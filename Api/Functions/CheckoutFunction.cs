using FluentValidation;
using KatiesGarden.Api.Auth;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Api.Helpers;
using KatiesGarden.Models.Shop;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Net;
using System.Text.Json;

namespace KatiesGarden.Api.Functions;

public class CheckoutFunction(
    AppDbContext db,
    IValidator<CheckoutRequest> validator,
    IOptions<StripeOptions> stripeOptions,
    SessionService sessionService,
    ILogger<CheckoutFunction> logger)
{
    [Function("CreateCheckoutSession")]
    public async Task<HttpResponseData> CreateSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkout/create-session")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        CheckoutRequest? request;
        try { request = await req.ReadFromJsonAsync<CheckoutRequest>(); }
        catch (JsonException) { return await Responses.BadRequest(req, "Invalid request body."); }
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
        var settings = await db.DeliverySettings.FindAsync([1], ct) ?? new DeliverySettings();
        var subtotal = request.Items.Sum(i => products[i.ProductId].Price * i.Quantity);
        var deliveryFee = request.DeliveryType == DeliveryType.Collection
            ? 0m
            : (settings.FreeDeliveryThreshold.HasValue && subtotal >= settings.FreeDeliveryThreshold
                ? 0m
                : settings.LocalDeliveryFee);
        var total = subtotal + deliveryFee;

        // Capture authenticated customer identity so the order appears in "My Orders"
        var authenticatedUser = SwaAuth.GetAuthenticatedUser(req);

        // Store the email normalised (trimmed, lower-cased) so guest order lookup — which
        // matches on a normalised email — finds the order regardless of how it was typed.
        var customerEmail = request.Email.Trim().ToLowerInvariant();

        // Create pending order record
        var orderNumber = OrderNumberHelper.Generate();
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerFirstName = request.FirstName,
            CustomerLastName = request.LastName,
            CustomerEmail = customerEmail,
            CustomerPhone = request.Phone,
            DeliveryType = request.DeliveryType,
            DeliveryAddress = request.DeliveryAddress,
            DeliveryPostcode = request.DeliveryPostcode,
            CustomerNotes = request.Notes,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = total,
            Status = OrderStatus.Pending,
            CustomerId = authenticatedUser?.UserId,
            CustomerIdentityProvider = authenticatedUser?.IdentityProvider,
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

        var stripe = stripeOptions.Value;
        var siteUrl = stripe.SiteUrl;

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

        var sessionCreateOptions = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = $"{siteUrl}/order/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{siteUrl}/cart",
            CustomerEmail = customerEmail,
            Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }
        };

        Stripe.Checkout.Session session;
        try
        {
            session = await sessionService.CreateAsync(sessionCreateOptions, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            // Remove the orphan Pending order so the dashboard's pending count stays accurate
            db.Orders.Remove(order);
            await db.SaveChangesAsync(ct);
            logger.LogError(ex, "Stripe session creation failed for order {OrderNumber} — order removed", orderNumber);
            return await Responses.BadRequest(req, "Payment processor is unavailable. Please try again in a moment.");
        }

        order.StripeSessionId = session.Id;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created Stripe session {SessionId} for order {OrderNumber}", session.Id, orderNumber);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { url = session.Url });
        return response;
    }
}
