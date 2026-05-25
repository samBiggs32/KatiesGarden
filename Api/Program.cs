using FluentValidation;
using KatiesGarden.Api.Data;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var dbUrl = context.Configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(dbUrl))
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(dbUrl));

        services.AddHttpClient();

        services.AddSingleton<IValidator<ContactUsForm>, ContactUsFormValidator>();
        services.AddSingleton<IValidator<SubscribeRequest>, SubscribeRequestValidator>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        db?.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialisation failed — subscribe endpoint will be unavailable");
    }
}

await host.RunAsync();
