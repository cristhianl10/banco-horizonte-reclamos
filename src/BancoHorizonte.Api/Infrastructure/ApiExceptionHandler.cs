using BancoHorizonte.Api.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BancoHorizonte.Api.Infrastructure;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainRuleException => (StatusCodes.Status400BadRequest, "Regla de negocio no válida"),
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            _ => (StatusCodes.Status500InternalServerError, "Error inesperado")
        };
        if (status == 500) logger.LogError(exception, "Unhandled API exception");
        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status, Title = title,
                Detail = status == 500 ? "Ocurrió un error al procesar la solicitud." : exception.Message,
                Instance = context.Request.Path
            }
        });
    }
}
