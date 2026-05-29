using FluentValidation;

namespace KatiesGarden.Models.Shop;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.").MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.").MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(254);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone number is required.").MaximumLength(30);
        RuleFor(x => x.DeliveryType)
            .NotEmpty()
            .Must(t => t == "Collection" || t == "LocalDelivery")
            .WithMessage("Delivery type must be Collection or LocalDelivery.");
        RuleFor(x => x.DeliveryAddress)
            .NotEmpty().WithMessage("Delivery address is required for local delivery.")
            .When(x => x.DeliveryType == "LocalDelivery");
        RuleFor(x => x.DeliveryPostcode)
            .NotEmpty().WithMessage("Postcode is required for local delivery.")
            .When(x => x.DeliveryType == "LocalDelivery");
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Cart cannot be empty.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be at least 1.");
        });
    }
}
