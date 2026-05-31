using KatiesGarden.Api.Data;
using KatiesGarden.Api.Email;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Services;
using KatiesGarden.Models.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KatiesGarden.Api.Functions.Orchestration;

public class OrderActivities(
    AppDbContext db,
    IEmailSender emailSender,
    IPushNotificationService pushService,
    IOptions<SmtpOptions> smtpOptions,
    IOptions<StripeOptions> stripeOptions,
    ILogger<OrderActivities> logger)
{
    [Function(nameof(SendCustomerConfirmationEmailActivity))]
    public async Task SendCustomerConfirmationEmailActivity([ActivityTrigger] Guid orderId)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return;

        try
        {
            var smtp = smtpOptions.Value;
            var settings = await db.DeliverySettings.FindAsync([1]);
            var email = OrderEmailBuilder.BuildCustomerConfirmation(
                order, smtp.EffectiveSenderEmail, "Katie's Garden", settings?.CollectionAddress);
            await emailSender.SendAsync(email);
            logger.LogInformation("Customer confirmation email sent for order {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send customer confirmation email for order {OrderId}", orderId);
            throw;
        }
    }

    [Function(nameof(SendAdminAlertEmailActivity))]
    public async Task SendAdminAlertEmailActivity([ActivityTrigger] Guid orderId)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return;

        try
        {
            var smtp = smtpOptions.Value;
            var siteUrl = stripeOptions.Value.SiteUrl;
            var email = OrderEmailBuilder.BuildAdminAlert(order, smtp.RecipientEmail, siteUrl);
            await emailSender.SendAsync(email);
            logger.LogInformation("Admin alert email sent for order {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send admin alert email for order {OrderId}", orderId);
            throw;
        }
    }

    [Function(nameof(SendPushNotificationActivity))]
    public async Task SendPushNotificationActivity([ActivityTrigger] OrderOrchestratorInput input)
    {
        try
        {
            await pushService.SendAsync(
                $"New Order {input.OrderNumber}",
                $"£{input.Total:F2} from {input.CustomerFirstName} {input.CustomerLastName}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send push notification for order {OrderNumber}", input.OrderNumber);
            // Non-fatal — don't rethrow; push failure must not block order processing
        }
    }

    [Function(nameof(DecrementStockActivity))]
    public async Task DecrementStockActivity([ActivityTrigger] Guid orderId)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return;

        foreach (var line in order.Lines)
        {
            var rowsUpdated = await db.Products
                .Where(p => p.Id == line.ProductId && p.StockQuantity != null && p.StockQuantity >= line.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity! - line.Quantity));

            if (rowsUpdated == 0)
            {
                logger.LogWarning(
                    "Stock exhausted for product {ProductId} in order {OrderNumber} — payment taken, requires manual review",
                    line.ProductId, order.OrderNumber);
            }
            else
            {
                await db.Products
                    .Where(p => p.Id == line.ProductId && p.StockQuantity == 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, false));
            }
        }
    }

    [Function(nameof(RecordStatusHistoryActivity))]
    public async Task RecordStatusHistoryActivity([ActivityTrigger] RecordStatusHistoryInput input)
    {
        if (!Enum.TryParse<OrderStatus>(input.Status, out var status))
        {
            logger.LogWarning("Unknown status {Status} for order {OrderId}", input.Status, input.OrderId);
            return;
        }

        db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = input.OrderId,
            Status = status,
            Note = input.Note,
            ChangedBy = input.ChangedBy
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Status history recorded: {Status} for order {OrderId}", status, input.OrderId);
    }

    [Function(nameof(SendStatusUpdateEmailActivity))]
    public async Task SendStatusUpdateEmailActivity([ActivityTrigger] Guid orderId)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return;

        try
        {
            var smtp = smtpOptions.Value;
            var email = OrderEmailBuilder.BuildStatusUpdate(order, smtp.EffectiveSenderEmail, "Katie's Garden");
            await emailSender.SendAsync(email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send status update email for order {OrderId}", orderId);
            throw;
        }
    }
}

public record RecordStatusHistoryInput(Guid OrderId, string Status, string? Note, string? ChangedBy);
