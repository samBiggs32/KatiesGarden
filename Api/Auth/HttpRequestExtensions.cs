using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace KatiesGarden.Api.Auth;

public static class HttpRequestExtensions
{
    /// <summary>
    /// Returns a 401 Unauthorized response if the caller is not an admin, otherwise null.
    /// Usage: if (await req.RequireAdminAsync() is { } deny) return deny;
    /// </summary>
    public static HttpResponseData? RequireAdmin(this HttpRequestData req) =>
        SwaAuth.IsAdmin(req) ? null : req.CreateResponse(HttpStatusCode.Unauthorized);
}
