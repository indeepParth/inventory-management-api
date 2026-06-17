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
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.ContentType = "application/json";

                int statusCode = ex switch
                {
                    ValidationException => StatusCodes.Status400BadRequest,
                    NotFoundException => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };
                context.Response.StatusCode = statusCode;

                var responce = new
                {
                    message = ex.Message
                };

                var json = JsonSerializer.Serialize(responce);

                await context.Response.WriteAsync(json);
            }
        }
    }
}