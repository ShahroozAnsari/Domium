using Domium.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Maps domain exceptions to ProblemDetails the frontend can show: not-found → 404,
/// any other rule violation → 409. The exception message is already localized (the
/// domain builds it from resources using the request's CurrentUICulture).
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException)
        {
            return false;
        }

        var status = exception is DomainNotFoundException
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status409Conflict;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = exception.GetType().Name,
                Detail = exception.Message,
            },
            cancellationToken);

        return true;
    }
}
