using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Request aborted by client. TraceId: {TraceId}", context.TraceIdentifier);
                await WriteProblemDetailsAsync(context, StatusCodes.Status499ClientClosedRequest, "Request canceled", "The request was canceled by the client.");
            }
            catch (HttpRequestException ex)
            {
                var statusCode = ex.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => StatusCodes.Status503ServiceUnavailable,
                    HttpStatusCode.BadRequest => StatusCodes.Status502BadGateway,
                    HttpStatusCode.Unauthorized => StatusCodes.Status502BadGateway,
                    HttpStatusCode.Forbidden => StatusCodes.Status502BadGateway,
                    HttpStatusCode.NotFound => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status503ServiceUnavailable
                };

                _logger.LogError(ex, "External HTTP dependency failed. TraceId: {TraceId}", context.TraceIdentifier);
                await WriteProblemDetailsAsync(context, statusCode, "External service error", "The upstream service request failed.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Application processing failed. TraceId: {TraceId}", context.TraceIdentifier);
                await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "Processing error", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
                await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.");
            }
        }

        private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, SerializerOptions));
        }
    }
}
