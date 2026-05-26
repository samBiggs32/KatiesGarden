using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Email;
using Xunit;

namespace KatiesGarden.Tests.Email;

public class OrderEmailBuilderTests
{
    private static Order SampleOrder(DeliveryType deliveryType = DeliveryType.Collection) => new()
    {
        OrderNumber = "KG-20250526-A3F2",
        CustomerFirstName = "Jane",
        CustomerLastName = "Smith",
        CustomerEmail = "jane@example.com",
        CustomerPhone = "07800 123456",
        DeliveryType = deliveryType,
        DeliveryAddress = deliveryType == DeliveryType.LocalDelivery ? "12 High St, Milverton" : null,
        DeliveryPostcode = deliveryType == DeliveryType.LocalDelivery ? "TA4 1JN" : null,
        Subtotal = 25.00m,
        DeliveryFee = deliveryType == DeliveryType.LocalDelivery ? 5.00m : 0m,
        Total = deliveryType == DeliveryType.LocalDelivery ? 30.00m : 25.00m,
        Status = OrderStatus.Confirmed,
        Lines = [
            new OrderLine
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Summer Wreath",
                UnitPrice = 25.00m,
                Quantity = 1,
                LineTotal = 25.00m
            }
        ]
    };

    [Fact]
    public void CustomerConfirmation_Collection_UsesProvidedAddress_NotProductName()
    {
        var order = SampleOrder(DeliveryType.Collection);
        const string address = "Milverton, Somerset, TA4 1AB";

        var email = OrderEmailBuilder.BuildCustomerConfirmation(order, "noreply@katiesgarden.uk", "Katie's Garden", address);

        email.BodyText.Should().Contain(address, "the collection address must appear in the confirmation");
        email.BodyText.Should().NotContain("Click & Collect from Summer Wreath",
            "regression guard: confirmation must not show the product name as the collection point");
    }

    [Fact]
    public void CustomerConfirmation_Collection_NoAddressProvided_FallsBackToGenericText()
    {
        var order = SampleOrder(DeliveryType.Collection);
        var email = OrderEmailBuilder.BuildCustomerConfirmation(order, "noreply@katiesgarden.uk", "Katie's Garden", null);

        email.BodyText.Should().Contain("our Milverton location");
    }

    [Fact]
    public void CustomerConfirmation_LocalDelivery_ShowsDeliveryAddress()
    {
        var order = SampleOrder(DeliveryType.LocalDelivery);

        var email = OrderEmailBuilder.BuildCustomerConfirmation(order, "noreply@katiesgarden.uk", "Katie's Garden", "ignored");

        email.BodyText.Should().Contain("12 High St, Milverton");
        email.BodyText.Should().Contain("TA4 1JN");
    }

    [Fact]
    public void CustomerConfirmation_IncludesAllItemsAndTotals()
    {
        var order = SampleOrder();
        var email = OrderEmailBuilder.BuildCustomerConfirmation(order, "noreply@katiesgarden.uk", "Katie's Garden", "addr");

        email.BodyText.Should().Contain("Summer Wreath");
        email.BodyText.Should().Contain("£25.00");
        email.BodyText.Should().Contain("KG-20250526-A3F2");
        email.ToAddress.Should().Be("jane@example.com");
        email.Subject.Should().Contain("KG-20250526-A3F2");
    }

    [Fact]
    public void AdminAlert_IncludesOrderLinkAndCustomerDetails()
    {
        var order = SampleOrder();
        var email = OrderEmailBuilder.BuildAdminAlert(order, "team@katiesgarden.uk", "https://www.katiesgarden.uk");

        email.BodyText.Should().Contain("https://www.katiesgarden.uk/admin/orders/");
        email.BodyText.Should().Contain(order.Id.ToString());
        email.BodyText.Should().Contain("Jane Smith");
        email.BodyText.Should().Contain("jane@example.com");
        email.Subject.Should().Contain("£25.00");
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed, "confirmed your order")]
    [InlineData(OrderStatus.ReadyForCollection, "ready")]
    [InlineData(OrderStatus.Dispatched, "dispatched")]
    [InlineData(OrderStatus.Delivered, "delivered")]
    [InlineData(OrderStatus.Cancelled, "cancelled")]
    public void StatusUpdate_ProducesMessageMatchingStatus(OrderStatus status, string expectedFragment)
    {
        var order = SampleOrder();
        order.Status = status;

        var email = OrderEmailBuilder.BuildStatusUpdate(order, "noreply@katiesgarden.uk", "Katie's Garden");

        email.BodyText.ToLowerInvariant().Should().Contain(expectedFragment.ToLowerInvariant());
        email.BodyText.Should().Contain(order.OrderNumber);
    }
}
