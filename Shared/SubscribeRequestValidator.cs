using FluentValidation;

namespace KatiesGarden.Web.Client.Models.Validators
{
    public class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
    {
        public SubscribeRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email address is required.")
                .EmailAddress().WithMessage("Please enter a valid email address.")
                .MaximumLength(254);

            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .When(x => x.FirstName is not null);
        }
    }
}
