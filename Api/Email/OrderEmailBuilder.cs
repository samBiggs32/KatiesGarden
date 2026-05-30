using KatiesGarden.Api.Data;
using KatiesGarden.Models.Email;

namespace KatiesGarden.Api.Email;

public static class OrderEmailBuilder
{
    public static EmailMessage BuildCustomerConfirmation(Order order, string fromAddress, string fromName, string? collectionAddress = null)
    {
        var deliveryInfo = order.DeliveryType == DeliveryType.Collection
            ? $"Click & Collect from {(string.IsNullOrWhiteSpace(collectionAddress) ? "our Milverton location" : collectionAddress)}"
            : $"Local delivery to {order.DeliveryAddress}, {order.DeliveryPostcode}";

        var itemsList = string.Join("\n", order.Lines.Select(l =>
            $"  • {l.ProductName} x{l.Quantity} — £{l.LineTotal:F2}"));

        var body = $"""
            Thank you for your order, {order.CustomerFirstName}!

            Order number: {order.OrderNumber}
            {deliveryInfo}

            Items ordered:
            {itemsList}

            Subtotal: £{order.Subtotal:F2}
            Delivery: £{order.DeliveryFee:F2}
            Total: £{order.Total:F2}

            We'll be in touch shortly to confirm your order and arrange {(order.DeliveryType == DeliveryType.Collection ? "collection" : "delivery")}.

            If you have any questions, please contact us at sales@katiesgarden.uk or call 07804 784522.

            Thank you for supporting Katie's Garden!
            """;

        return new EmailMessage(
            FromAddress: fromAddress,
            FromName: fromName,
            ToAddress: order.CustomerEmail,
            ToName: $"{order.CustomerFirstName} {order.CustomerLastName}",
            ReplyToAddress: fromAddress,
            ReplyToName: fromName,
            Subject: $"Order Confirmed — {order.OrderNumber} | Katie's Garden",
            BodyText: body);
    }

    public static EmailMessage BuildAdminAlert(Order order, string fromAddress, string siteBaseUrl)
    {
        var itemsList = string.Join("\n", order.Lines.Select(l =>
            $"  • {l.ProductName} x{l.Quantity} — £{l.LineTotal:F2}"));

        var deliveryInfo = order.DeliveryType == DeliveryType.Collection
            ? "Collection"
            : $"Local delivery to {order.DeliveryAddress}, {order.DeliveryPostcode}";

        var body = $"""
            New order received: {order.OrderNumber}

            Customer: {order.CustomerFirstName} {order.CustomerLastName}
            Email: {order.CustomerEmail}
            Phone: {order.CustomerPhone}
            Delivery: {deliveryInfo}
            {(order.CustomerNotes is not null ? $"Notes: {order.CustomerNotes}" : "")}

            Items:
            {itemsList}

            Total: £{order.Total:F2}

            View order: {siteBaseUrl}/admin/orders/{order.Id}
            """;

        return new EmailMessage(
            FromAddress: fromAddress,
            FromName: "Katie's Garden Store",
            ToAddress: fromAddress,
            ToName: "Katie's Garden",
            ReplyToAddress: order.CustomerEmail,
            ReplyToName: $"{order.CustomerFirstName} {order.CustomerLastName}",
            Subject: $"New Order {order.OrderNumber} — £{order.Total:F2}",
            BodyText: body);
    }

    public static EmailMessage BuildStatusUpdate(Order order, string fromAddress, string fromName)
    {
        var (subject, message) = order.Status switch
        {
            OrderStatus.Confirmed =>
                ("We've confirmed your order", $"Great news — we've confirmed your order {order.OrderNumber} and will begin preparing it soon."),
            OrderStatus.Processing =>
                ("Your order is being prepared", $"We're preparing your order {order.OrderNumber} and will let you know when it's ready."),
            OrderStatus.ReadyForCollection =>
                ("Your order is ready to collect!", $"Your order {order.OrderNumber} is ready! Please collect from our address in Milverton, Somerset TA4. Call us on 07804 784522 if you need directions."),
            OrderStatus.Dispatched =>
                ("Your order is on its way", $"Your order {order.OrderNumber} has been dispatched for local delivery. We'll aim to deliver within the agreed timeframe."),
            OrderStatus.Delivered =>
                ("Your order has been delivered", $"Your order {order.OrderNumber} has been delivered. We hope you love it! Please get in touch if you have any questions."),
            OrderStatus.Cancelled =>
                ("Your order has been cancelled", $"Your order {order.OrderNumber} has been cancelled. If you believe this is a mistake or would like a refund, please contact us at sales@katiesgarden.uk"),
            _ => ("Update on your order", $"There's an update on your order {order.OrderNumber}. Please contact us if you have any questions.")
        };

        var body = $"""
            Hi {order.CustomerFirstName},

            {message}

            Order: {order.OrderNumber}
            Total: £{order.Total:F2}

            If you have any questions, please contact us at sales@katiesgarden.uk

            Katie's Garden
            """;

        return new EmailMessage(
            FromAddress: fromAddress,
            FromName: fromName,
            ToAddress: order.CustomerEmail,
            ToName: $"{order.CustomerFirstName} {order.CustomerLastName}",
            ReplyToAddress: fromAddress,
            ReplyToName: fromName,
            Subject: $"{subject} — {order.OrderNumber} | Katie's Garden",
            BodyText: body);
    }
}
