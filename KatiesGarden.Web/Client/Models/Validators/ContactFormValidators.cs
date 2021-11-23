using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KatiesGarden.Web.Client.Models.Validators
{
    /// <summary>
    /// A standard AbstractValidator which contains multiple rules and can be shared with the back end API
    /// </summary>
    /// <typeparam name="OrderModel"></typeparam>
    public class ContactUsFormValidator : AbstractValidator<ContactUsForm>
    {
        private Regex _phoneNumberRegex = new Regex(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public ContactUsFormValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .Length(1, 100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .Length(1, 100);

            RuleFor(x => x.EmailSubject)
                .NotEmpty()
                .Length(1, 100);

            RuleFor(x => x.EmailBody)
            .NotEmpty()
            .Length(1, 255);

            RuleFor(x => x.ContactNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Custom((x, context) =>
                {
                    var matches = _phoneNumberRegex.Match(x);

                    if(!matches.Success)
                        context.AddFailure($"Invalid phone number");
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
