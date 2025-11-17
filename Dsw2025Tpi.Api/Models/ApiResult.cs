namespace Dsw2025Tpi.Api.Models
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public int Status { get; set; }
        public string? Message { get; set; }
        public int? ErrorCode { get; set; }
        public T? Data { get; set; }

        public static ApiResult<T> SuccessResult(T data, string? message = null, int status = 200)
            => new ApiResult<T> { Success = true, Status = status, Message = message, Data = data };

        public static ApiResult<T> Error(string message, int status, int? errorCode = null)
            => new ApiResult<T> { Success = false, Status = status, Message = message, ErrorCode = errorCode };
    }

}
