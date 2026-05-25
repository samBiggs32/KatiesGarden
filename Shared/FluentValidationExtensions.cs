using FluentValidation;

namespace KatiesGarden.Models.Validators;

public static class FluentValidationExtensions
{
    // Adapter that turns an IValidator<T> into a per-property validation
    // delegate. Compatible with MudBlazor's MudForm.Validation parameter
    // and any other form library that uses the same shape.
    public static Func<object, string, Task<IEnumerable<string>>> ToFieldValidator<T>(this IValidator<T> validator)
    {
        return async (model, propertyName) =>
        {
            var result = await validator.ValidateAsync(ValidationContext<T>
                .CreateWithOptions((T)model, x => x.IncludeProperties(propertyName)));

            return result.IsValid
                ? Array.Empty<string>()
                : result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
