using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace KatiesGarden.Api;

internal static class Responses
{
    public static async Task<HttpResponseData> Plain(HttpRequestData req, HttpStatusCode status, string message)
    {
        var response = req.CreateResponse(status);
        await response.WriteStringAsync(message);
        return response;
    }

    public static Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        => Plain(req, HttpStatusCode.BadRequest, message);

    public static Task<HttpResponseData> InternalError(HttpRequestData req, string message)
        => Plain(req, HttpStatusCode.InternalServerError, message);
}
