using KatiesGarden.Models;
using KatiesGarden.Models.Validators;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Contact
    {
        [Inject] HttpClient Http { get; set; } = null!;

        [Parameter]
        [SupplyParameterFromQuery(Name = "subject")]
        public string Subject { get; set; } = string.Empty;

        private static readonly ContactUsFormValidator _validator = new();

        private ContactUsForm model = new();
        private bool isSubmitting;
        private bool _success;
        private string? _error;
        private Dictionary<string, string> _fieldErrors = [];

        protected override void OnInitialized()
        {
            if (!string.IsNullOrEmpty(Subject))
                model.EmailSubject = Subject;
        }

        private async Task HandleSubmitAsync()
        {
            _error = null;
            _fieldErrors = [];

            var result = _validator.Validate(model);
            if (!result.IsValid)
            {
                _fieldErrors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.First().ErrorMessage);
                return;
            }

            isSubmitting = true;
            try
            {
                Logger.LogInformation("Submitting contact form");
                var response = await Http.PostAsJsonAsync("api/contact", model);
                if (response.IsSuccessStatusCode)
                {
                    _success = true;
                }
                else
                {
                    Logger.LogError("Contact form API returned {StatusCode}", response.StatusCode);
                    _error = "There was a problem sending your message. Please try calling or emailing us directly.";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error submitting contact form");
                _error = "Unable to reach the server. Please try calling or emailing us directly.";
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private void ResetForm()
        {
            model = new ContactUsForm();
            _success = false;
            _error = null;
            _fieldErrors = [];
        }

        private bool HasError(string field) => _fieldErrors.ContainsKey(field);
        private string GetError(string field) => _fieldErrors.TryGetValue(field, out var msg) ? msg : string.Empty;
    }
}
