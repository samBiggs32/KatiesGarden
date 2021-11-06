using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Contact
    {
        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IJSRuntime JSRuntime { get; set; }

        ContactUsFormValidator orderValidator = new ContactUsFormValidator();        

        ContactUsForm model = new();
        bool success;
        string[] errors = { };
        MudTextField<string> pwField1;
        MudForm form;

        private async Task HandleSubmit()
        {
            Logger.LogInformation("HandleValidSubmit called");
            
            string email = "eeysb11@gmail.com";

            await JSRuntime.InvokeAsync<object>("blazorExtensions.SendLocalEmail", new object[] { email, model.EmailSubject, model.EmailBody });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("initMap", null);
                StateHasChanged();
            }
        }
    }
}
