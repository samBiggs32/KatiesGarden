using KatiesGarden.Api.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var dbUrl = context.Configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(dbUrl))
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(dbUrl));

        services.AddHttpClient();
    })
    .Build();

// Create tables on first run if they don't exist
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<AppDbContext>();
    db?.Database.EnsureCreated();
}

await host.RunAsync();
