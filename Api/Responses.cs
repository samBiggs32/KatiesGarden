using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

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

    /// Reads a JSON request body and runs its FluentValidation validator.
    /// On success returns (value, null); on a malformed/empty body or a validation
    /// failure returns (default, a 400 response) ready to return from the function.
    /// Usage:
    ///   var (request, error) = await Responses.ReadValidatedAsync(req, validator, ct);
    ///   if (error is not null) return error;
    public static async Task<(T? Value, HttpResponseData? Error)> ReadValidatedAsync<T>(
        HttpRequestData req, IValidator<T> validator, CancellationToken ct)
    {
        T? value;
        try { value = await req.ReadFromJsonAsync<T>(); }
        catch (JsonException) { return (default, await BadRequest(req, "Invalid request body.")); }
        if (value is null) return (default, await BadRequest(req, "Request body is required."));

        var validation = await validator.ValidateAsync(value, ct);
        if (!validation.IsValid)
            return (default, await BadRequest(req, validation.Errors.First().ErrorMessage));

        return (value, null);
    }
}
