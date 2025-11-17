using Dsw2025Tpi.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Extensions
{
    public static class ApiResponseExtensions
    {
        public static IActionResult ApiOk<T>(this ControllerBase controller, T data, string? message = null)
        {
            return controller.Ok(ApiResult<T>.SuccessResult(data, message));
        }

        public static IActionResult ApiCreated<T>(this ControllerBase controller, T data, string? message = null)
        {
            return controller.StatusCode(201, ApiResult<T>.SuccessResult(data, message, 201));
        }

        public static IActionResult ApiNoContent(this ControllerBase controller)
        {
            return controller.StatusCode(204, ApiResult<object>.SuccessResult(null, "No content", 204));
        }
    }

}
