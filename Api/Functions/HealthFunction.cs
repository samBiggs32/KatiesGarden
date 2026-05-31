using KatiesGarden.Api.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KatiesGarden.Api.Functions;

// GET /api/health — lightweight readiness probe. Returns 200 only when the
// process can reach the database. Stays fast (<100ms) by running a single
// SELECT 1 rather than the full multi-service check in /api/diagnostics.
public class HealthFunction(AppDbContext db, ILogger<HealthFunction> logger)
{
    [Function("Health")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain");
            response.WriteString("OK");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check: database unreachable");
            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            response.Headers.Add("Content-Type", "text/plain");
            response.WriteString("Database unavailable");
            return response;
        }
    }
}
