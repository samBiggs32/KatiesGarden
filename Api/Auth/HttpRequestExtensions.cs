using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace KatiesGarden.Api.Auth;

public static class HttpRequestExtensions
{
    /// Returns a 401 Unauthorized response if the caller is not an admin, otherwise null.
    /// Usage: if (req.RequireAdmin() is { } deny) return deny;
    public static HttpResponseData? RequireAdmin(this HttpRequestData req) =>
        SwaAuth.IsAdmin(req) ? null : req.CreateResponse(HttpStatusCode.Unauthorized);

    /// Returns the first value of a query-string parameter, or null if absent.
    public static string? GetQueryParam(this HttpRequestData req, string name) =>
        System.Web.HttpUtility.ParseQueryString(req.Url.Query)[name];
}
