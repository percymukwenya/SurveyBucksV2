using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Middleware
{
    /// <summary>
    /// Global exception handling middleware for production-ready error responses
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
                    context.Request.Path, context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, errorCode) = exception switch
            {
                UnauthorizedAccessException => (
                    HttpStatusCode.Unauthorized,
                    "You are not authorized to perform this action.",
                    "UNAUTHORIZED"
                ),

                ArgumentNullException argEx => (
                    HttpStatusCode.BadRequest,
                    $"Required parameter is missing: {argEx.ParamName}",
                    "BAD_REQUEST"
                ),

                ArgumentException argEx => (
                    HttpStatusCode.BadRequest,
                    argEx.Message,
                    "BAD_REQUEST"
                ),

                InvalidOperationException => (
                    HttpStatusCode.BadRequest,
                    exception.Message,
                    "INVALID_OPERATION"
                ),

                NotImplementedException => (
                    HttpStatusCode.NotImplemented,
                    "This feature is not yet implemented. Please check back later.",
                    "NOT_IMPLEMENTED"
                ),

                KeyNotFoundException => (
                    HttpStatusCode.NotFound,
                    "The requested resource was not found.",
                    "NOT_FOUND"
                ),

                TimeoutException => (
                    HttpStatusCode.RequestTimeout,
                    "The request timed out. Please try again.",
                    "TIMEOUT"
                ),

                _ => (
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again later.",
                    "INTERNAL_ERROR"
                )
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponse
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method
            };

            // Include stack trace in development mode only
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                response.Details = exception.ToString();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonResponse = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// Standard error response format
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
    }
}
