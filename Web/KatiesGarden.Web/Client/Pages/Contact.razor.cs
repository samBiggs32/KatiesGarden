using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Contact
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] HttpClient Http { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "subject")]
        public string Subject { get; set; }

        ContactUsFormValidator orderValidator = new ContactUsFormValidator();
        ContactUsForm model = new();
        MudForm form;
        private bool isSubmitting = false;

        protected override void OnInitialized()
        {
            if (!string.IsNullOrEmpty(Subject))
                model.EmailSubject = Subject;

            base.OnInitialized();
        }

        private async Task HandleSubmitAsync()
        {
            await form.Validate();

            if (!form.IsValid)
                return;

            isSubmitting = true;

            try
            {
                Logger.LogInformation("Submitting contact form");

                var response = await Http.PostAsJsonAsync("api/contact", model);

                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Your message has been sent! We'll be in touch soon.", Severity.Success);
                    model = new ContactUsForm();
                    await form.ResetAsync();
                }
                else
                {
                    Logger.LogError("Contact form API returned {StatusCode}", response.StatusCode);
                    Snackbar.Add("There was a problem sending your message. Please try calling or emailing us directly.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error submitting contact form");
                Snackbar.Add("Unable to reach the server. Please try calling or emailing us directly.", Severity.Error);
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private async Task ResetFormAsync()
        {
            model = new ContactUsForm();
            await form.ResetAsync();
        }
    }
}
