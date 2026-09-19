using System.Text.Json;
using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.Exceptions;

namespace MiniLogistics.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception ex)
    {
        _logger.LogError(
            ex,
            "Exception xảy ra khi xử lý request {Method} {Path}",
            context.Request.Method,
            context.Request.Path
        );

        var statusCode = GetStatusCode(ex);

        var message = GetMessage(ex);

        var response = new ErrorResponseDTO
        {
            StatusCode = statusCode,
            Message = message,
            Detail = _environment.IsDevelopment()
                ? ex.ToString()
                : null,
            Timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = statusCode;

        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            });

        await context.Response.WriteAsync(json);
    }

    private static int GetStatusCode(Exception ex)
    {
        return ex switch
        {
            BadRequestException =>
                StatusCodes.Status400BadRequest,

            UnauthorizedException =>
                StatusCodes.Status401Unauthorized,

            ForbiddenException =>
                StatusCodes.Status403Forbidden,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            _ =>
                StatusCodes.Status500InternalServerError
        };
    }

    private static string GetMessage(Exception ex)
    {
        return ex switch
        {
            BadRequestException =>
                ex.Message,

            UnauthorizedException =>
                ex.Message,

            ForbiddenException =>
                ex.Message,

            NotFoundException =>
                ex.Message,

            _ =>
                "Đã xảy ra lỗi hệ thống"
        };
    }
}