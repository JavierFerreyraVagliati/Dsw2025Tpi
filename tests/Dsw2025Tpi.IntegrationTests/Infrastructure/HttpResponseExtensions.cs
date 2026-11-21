using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Forma de todas las respuestas de la API (ver Dsw2025Tpi.Api.Models.ApiResult).
/// </summary>
public record ApiResponse<T>(bool Success, int Status, string? Message, int? ErrorCode, T? Data);

public static class HttpResponseExtensions
{
    // Mismas reglas que la API: camelCase y enums como texto
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Lee el cuerpo de la respuesta como ApiResponse&lt;T&gt;.</summary>
    public static async Task<ApiResponse<T>?> ReadApiResponseAsync<T>(this HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
}
