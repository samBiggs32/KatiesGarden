using FluentValidation;

namespace KatiesGarden.Models.Validators
{
    public class SubscribeRequestValidator : AbstractValidator<SubscribeRequest>
    {
        public SubscribeRequestValidator()
        {
            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Email address is required.")
                .Matches(EmailRegex.Pattern).WithMessage("Please enter a valid email address.")
                .MaximumLength(254);

            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .When(x => x.FirstName is not null);
        }
    }
}
