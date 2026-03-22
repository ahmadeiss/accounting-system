using System.Net;
using System.Text.Json;
using Accounting.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Accounting.API.Middleware;

/// <summary>
/// Global exception handler. Converts domain exceptions to structured JSON error responses.
/// Prevents stack traces from leaking to clients in production.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message, errors) = exception switch
        {
            Core.Exceptions.UnauthorizedException uae =>
                (HttpStatusCode.Unauthorized, uae.ErrorCode, uae.Message, (object?)null),

            Core.Exceptions.ForbiddenException =>
                (HttpStatusCode.Forbidden, "FORBIDDEN", exception.Message, (object?)null),

            NotFoundException nfe =>
                (HttpStatusCode.NotFound, "NOT_FOUND", nfe.Message, (object?)null),

            DuplicateEntityException dee =>
                (HttpStatusCode.Conflict, dee.ErrorCode, dee.Message, (object?)null),

            ExpiredBatchException ebe =>
                (HttpStatusCode.UnprocessableEntity, ebe.ErrorCode, ebe.Message, (object?)null),

            InsufficientStockException ise =>
                (HttpStatusCode.UnprocessableEntity, ise.ErrorCode, ise.Message, (object?)null),

            Core.Exceptions.ValidationException ve =>
                (HttpStatusCode.UnprocessableEntity, ve.ErrorCode, ve.Message, (object?)ve.Errors),

            DomainException de =>
                (HttpStatusCode.UnprocessableEntity, de.ErrorCode, de.Message, (object?)null),

            // PostgreSQL serialization failure (SQLSTATE 40001) — raised when SERIALIZABLE
            // transactions conflict. The client should retry the operation.
            DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "40001" } } =>
                (HttpStatusCode.Conflict, "SERIALIZATION_FAILURE",
                    "The operation conflicted with a concurrent transaction. Please retry.",
                    (object?)null),

            // EF concurrency token mismatch (xmin changed between read and write)
            DbUpdateConcurrencyException =>
                (HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT",
                    "The record was modified by another user. Please refresh and retry.",
                    (object?)null),

            _ =>
                (HttpStatusCode.InternalServerError, "INTERNAL_ERROR",
                    _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                    (object?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Domain exception [{Code}]: {Message}", errorCode, message);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            ErrorCode = errorCode,
            Message = message,
            Errors = errors,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}

