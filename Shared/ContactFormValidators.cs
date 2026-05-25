using FluentValidation;
using System.Text.RegularExpressions;

namespace KatiesGarden.Web.Client.Models.Validators
{
    public class ContactUsFormValidator : AbstractValidator<ContactUsForm>
    {
        private readonly Regex _phoneNumberRegex = new(
            @"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public ContactUsFormValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .Length(1, 100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .Length(1, 100);

            RuleFor(x => x.EmailAddress)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches(EmailRegex.Pattern).WithMessage("Please enter a valid email address.")
                .MaximumLength(254);

            RuleFor(x => x.EmailSubject)
                .NotEmpty()
                .Length(1, 100);

            RuleFor(x => x.EmailBody)
                .NotEmpty()
                .Length(1, 2000);

            RuleFor(x => x.ContactNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Custom((x, context) =>
                {
                    if (!_phoneNumberRegex.IsMatch(x))
                        context.AddFailure("Invalid phone number");
                });
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<ContactUsForm>
                .CreateWithOptions((ContactUsForm)model, x => x.IncludeProperties(propertyName)));

            if (result.IsValid)
                return Array.Empty<string>();

            return result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
