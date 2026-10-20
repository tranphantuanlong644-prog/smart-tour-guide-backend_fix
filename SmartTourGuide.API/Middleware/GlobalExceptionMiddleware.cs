using System.Text.Json;
using SmartTourGuide.Business.Exceptions;

namespace SmartTourGuide.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;

    private readonly ILogger<GlobalExceptionMiddleware>
        _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(
                context,
                exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var errorResponse =
            MapException(exception);

        if (errorResponse.StatusCode >= 500)
        {
            _logger.LogError(
                exception,
                "An unexpected error occurred while " +
                "processing request {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "A business error occurred while " +
                "processing request {Method} {Path}. " +
                "Code: {ErrorCode}. Message: {Message}",
                context.Request.Method,
                context.Request.Path,
                errorResponse.Code,
                errorResponse.Message);
        }

        context.Response.StatusCode =
            errorResponse.StatusCode;

        context.Response.ContentType =
            "application/json";

        var responseBody =
            new
            {
                success = false,

                error =
                    new
                    {
                        code =
                            errorResponse.Code,

                        message =
                            errorResponse.Message
                    },

                traceId =
                    context.TraceIdentifier
            };

        var responseJson =
            JsonSerializer.Serialize(
                responseBody,
                JsonOptions);

        await context.Response.WriteAsync(
            responseJson);
    }

    private static ErrorResponse MapException(
        Exception exception)
    {
        return exception switch
        {
            InvalidRequestException
                businessException =>
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        businessException.Code,
                        businessException.Message),

            InvalidCredentialsException
                businessException =>
                    new ErrorResponse(
                        StatusCodes.Status401Unauthorized,
                        businessException.Code,
                        businessException.Message),

            AccountInactiveException
                businessException =>
                    new ErrorResponse(
                        StatusCodes.Status403Forbidden,
                        businessException.Code,
                        businessException.Message),

            EmailAlreadyExistsException
                businessException =>
                    new ErrorResponse(
                        StatusCodes.Status409Conflict,
                        businessException.Code,
                        businessException.Message),

            BusinessException businessException =>
                new ErrorResponse(
                    StatusCodes.Status400BadRequest,
                    businessException.Code,
                    businessException.Message),

            _ =>
                new ErrorResponse(
                    StatusCodes
                        .Status500InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected server error occurred.")
        };
    }

    private sealed record ErrorResponse(
        int StatusCode,
        string Code,
        string Message);
}