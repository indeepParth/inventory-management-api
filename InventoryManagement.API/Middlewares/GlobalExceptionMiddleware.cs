using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Exceptions;

namespace InventoryManagement.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
                context.Response.ContentType = "application/json";

                int statusCode = ex switch
                {
                    ValidationException => StatusCodes.Status400BadRequest,
                    BadRequestException => StatusCodes.Status400BadRequest,
                    NotFoundException => StatusCodes.Status404NotFound,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    _ => StatusCodes.Status500InternalServerError
                };
                context.Response.StatusCode = statusCode;

                if (statusCode >= StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(
                        ex,
                        "Unhandled exception occurred while processing {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Request failed with {StatusCode} while processing {Method} {Path}",
                        statusCode,
                        context.Request.Method,
                        context.Request.Path);
                }

                object response = ex is ValidationException validationException
                    ? new
                    {
                        statusCode,
                        message = validationException.Message,
                        errors = validationException.Errors,
                        traceId = context.TraceIdentifier
                    }
                    : new
                    {
                        statusCode,
                        message = ex.Message,
                        traceId = context.TraceIdentifier
                    };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
            }
        }
    }
}
