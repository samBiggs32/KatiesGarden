using FluentValidation;

namespace KatiesGarden.Web.Client.Extensions;

public static class ValidatorExtensions
{
    public static Func<object, string, Task<IEnumerable<string>>> ToMudFormValidator<T>(this IValidator<T> validator)
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
