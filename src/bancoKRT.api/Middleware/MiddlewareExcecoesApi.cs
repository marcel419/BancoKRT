using bancoKRT.api.Application.Exceptions;
using bancoKRT.api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace bancoKRT.api.Middleware;

public sealed class MiddlewareExcecoesApi
{
    private readonly RequestDelegate _next;

    public MiddlewareExcecoesApi(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ExcecaoValidacaoDominio => (StatusCodes.Status400BadRequest, "Dados invalidos"),
            ExcecaoNaoEncontrado => (StatusCodes.Status404NotFound, "Registro nao encontrado"),
            ExcecaoConflito => (StatusCodes.Status409Conflict, "Conflito"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        });
    }
}
