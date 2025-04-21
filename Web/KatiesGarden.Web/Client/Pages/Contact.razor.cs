using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace KatiesGarden.Web.Client.Pages;

public partial class Contact
{
    [Inject] IJSRuntime JSRuntime { get; set; }

    ContactUsFormValidator orderValidator = new ContactUsFormValidator();

    ContactUsForm model = new();
    string[] errors = { };
    MudForm form;

    private async Task HandleSubmit()
    {
        Logger.LogInformation("HandleValidSubmit called");

        string email = "eeysb11@gmail.com";

        await JSRuntime.InvokeAsync<object>("blazorExtensions.SendLocalEmail",
            new object[] { email, model.EmailSubject, model.EmailBody, model.FirstName, model.LastName, model.ContactNumber });
    }
}
