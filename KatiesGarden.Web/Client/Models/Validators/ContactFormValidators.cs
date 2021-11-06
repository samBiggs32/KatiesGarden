using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KatiesGarden.Web.Client.Models.Validators
{
    /// <summary>
    /// A standard AbstractValidator which contains multiple rules and can be shared with the back end API
    /// </summary>
    /// <typeparam name="OrderModel"></typeparam>
    public class ContactUsFormValidator : AbstractValidator<ContactUsForm>
    {
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
                .MatchPhoneNumber();                                      
        }



        private async Task<bool> IsUniqueAsync(string email)
        {
            // Simulates a long running http call
            await Task.Delay(2000);
            return email.ToLower() != "test@test.com";
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<ContactUsForm>.CreateWithOptions((ContactUsForm)model, x => x.IncludeProperties(propertyName)));
            if (result.IsValid)
                return Array.Empty<string>();
            return result.Errors.Select(e => e.ErrorMessage);
        };
    }        

    public static class Extensions
    {
        public static IRuleBuilderOptions<T, string> MatchPhoneNumber<T>(this IRuleBuilder<T, string> rule)
            => rule.Matches(@"^(1-)?\d{3}-\d{3}-\d{4}$").WithMessage("Invalid phone number");
    }
}
