using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Contact
    {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "subject")]
        public string Subject { get; set; }

        ContactUsFormValidator orderValidator = new ContactUsFormValidator();
        ContactUsForm model = new();
        string[] errors = { };
        MudForm form;
        private bool isSubmitting = false;

        protected override void OnInitialized()
        {
            // If subject is provided via query parameter, set it in the form
            if (!string.IsNullOrEmpty(Subject))
            {
                model.EmailSubject = Subject;
            }

            base.OnInitialized();
        }

        private async Task HandleSubmitAsync()
        {
            await form.Validate();

            if (form.IsValid)
            {
                isSubmitting = true;

                try
                {
                    Logger.LogInformation("Opening email client for contact form submission");
                    const string email = "team@katiesgarden.uk";
                    await JSRuntime.InvokeAsync<object>("blazorExtensions.SendLocalEmail",
                        new object[] { email, model.EmailSubject, model.EmailBody, model.FirstName, model.LastName, model.ContactNumber });

                    Snackbar.Add("Your email client should now open — please send the pre-filled email to reach us.", Severity.Info);

                    model = new ContactUsForm();
                    await form.ResetAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error submitting contact form");
                    Snackbar.Add("There was an error sending your message. Please try again.", Severity.Error);
                }
                finally
                {
                    isSubmitting = false;
                }
            }
        }

        private async Task ResetFormAsync()
        {
            model = new ContactUsForm();
            await form.ResetAsync();
        }
    }
}