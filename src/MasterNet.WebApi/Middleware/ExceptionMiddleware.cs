using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MasterNet.WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env
    )
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected or the request was cancelled: not an application error.
            _logger.LogInformation("Request {Path} was cancelled by the client.", context.Request.Path);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the exception middleware will not modify the response.");
                throw;
            }

            _logger.LogError(ex, ex.Message);

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            // Since validation is already handled by the ValidationBehavior (returning Result<T>.Failure)
            // and/or ApiBehaviorOptions.InvalidModelStateResponseFactory, we do NOT special-case ValidationException here.
            // This middleware is now focused exclusively on unexpected errors.
            var statusCode = StatusCodes.Status500InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = "An unexpected error occurred.",
                Detail = _env.IsDevelopment()
                    ? $"{ex.Message}{Environment.NewLine}{ex.StackTrace}"
                    : null
            };
            problemDetails.Extensions["traceId"] = traceId;

            context.Response.Clear();
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(problemDetails, SerializerOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
