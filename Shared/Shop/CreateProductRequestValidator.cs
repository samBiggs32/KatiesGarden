using FluentValidation;

namespace KatiesGarden.Models.Shop;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must be 200 characters or fewer.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or fewer.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue)
            .WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.HowToBuyNote)
            .MaximumLength(500).When(x => x.HowToBuyNote is not null)
            .WithMessage("How to buy note must be 500 characters or fewer.");
    }
}
