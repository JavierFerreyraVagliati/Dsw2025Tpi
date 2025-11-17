using Dsw2025Tpi.Api.Models;
using Dsw2025Tpi.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

    namespace Dsw2025Tpi.Api.Middleware
    {
        public class GlobalExceptionHandlingMiddleware
        {
            private readonly RequestDelegate _next;

            public GlobalExceptionHandlingMiddleware(RequestDelegate next)
            {
                _next = next;
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int status = StatusCodes.Status500InternalServerError;
            int? errorCode = null;
            string message = exception.Message;

            switch (exception)
            {
                case AppException appEx:
                    status = MapAppStatus(appEx.AppStatus);
                    errorCode = appEx.ErrorCode;
                    message = appEx.Message;
                    break;
            }

            var response = ApiResult<object>.Error(message, status, errorCode);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;

            return context.Response.WriteAsJsonAsync(response);
        }

        /// <summary>
        /// Traduce AppStatus internos a HTTP Status Codes.
        /// </summary>
        private static int MapAppStatus(int appStatus) =>
                appStatus switch
                {
                    404 => StatusCodes.Status404NotFound,   // Not Found
                    400 => StatusCodes.Status400BadRequest, // Bad Request
                    409 => StatusCodes.Status409Conflict,   // Conflict
                    403 => StatusCodes.Status403Forbidden,  // Forbidden
                    204 => StatusCodes.Status204NoContent,
                    _ => StatusCodes.Status500InternalServerError
                };
        }
    }

