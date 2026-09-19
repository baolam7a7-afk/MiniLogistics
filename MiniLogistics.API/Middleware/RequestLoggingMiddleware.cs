using System.Diagnostics;

namespace MiniLogistics.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var requestId = context.TraceIdentifier;

        var method = context.Request.Method;

        var path = context.Request.Path;

        _logger.LogInformation(
            "Request bắt đầu | RequestId: {RequestId} | Method: {Method} | Path: {Path}",
            requestId,
            method,
            path
        );

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;

            _logger.LogInformation(
                "Request kết thúc | RequestId: {RequestId} | Method: {Method} | Path: {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms",
                requestId,
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}