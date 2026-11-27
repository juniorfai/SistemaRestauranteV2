using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaRestaurante.Tests.Infrastructure
{
    /// <summary>HttpClient con el formato JSON de la API y comprobaciones con mensajes claros.</summary>
    public class ApiClient
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        public HttpClient Http { get; }

        public ApiClient(HttpClient http)
        {
            Http = http;
        }

        public LoginResponse? Session { get; private set; }

        public async Task LoginAsync(string username, string password)
        {
            Session = await PostOkAsync<LoginResponse>("/api/Auth/login", new { username, password });
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        }

        public Task<HttpResponseMessage> GetAsync(string url) => Http.GetAsync(url);

        public Task<HttpResponseMessage> PostAsync(string url, object? body = null) =>
            Http.PostAsJsonAsync(url, body ?? new { }, Json);

        public Task<HttpResponseMessage> PutAsync(string url, object body) => Http.PutAsJsonAsync(url, body, Json);

        public Task<HttpResponseMessage> PatchAsync(string url, object? body = null) =>
            Http.PatchAsJsonAsync(url, body ?? new { }, Json);

        public Task<HttpResponseMessage> DeleteAsync(string url) => Http.DeleteAsync(url);

        public async Task<T> GetOkAsync<T>(string url) => await ReadAsync<T>(await GetAsync(url));

        public async Task<T> PostOkAsync<T>(string url, object body) => await ReadAsync<T>(await PostAsync(url, body));

        public async Task<HttpStatusCode> StatusOfAsync(Task<HttpResponseMessage> request)
        {
            using var response = await request;
            return response.StatusCode;
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} devolvió {(int)response.StatusCode}: {body}");
                }
                return (await response.Content.ReadFromJsonAsync<T>(Json))!;
            }
        }
    }

    public static class HttpAssert
    {
        /// <summary>Comprueba el código de respuesta y enseña el cuerpo si no coincide.</summary>
        public static async Task StatusAsync(HttpStatusCode expected, Task<HttpResponseMessage> request)
        {
            using var response = await request;
            if (response.StatusCode != expected)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Se esperaba {(int)expected} y llegó {(int)response.StatusCode} en " +
                            $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {body}");
            }
        }
    }

    // ---------- Respuestas de la API (solo los campos que usan las pruebas) ----------

    public record UserDto(int Id, string Username, string FullName, string Role, bool IsActive, int? RestaurantId, string? RestaurantName);
    public record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);
    public record RestaurantDto(int Id, string Name, string Email, bool IsActive);
    public record TableDto(int Id, int TableNumber, int Capacity, string Status, int RestaurantId);
    public record CustomerDto(int Id, string FirstName, string LastName, string Email);
    public record MenuItemDto(int Id, string Name, decimal Price, bool IsAvailable, int RestaurantId);
    public record ReservationDto(int Id, DateTime ReservationDateTime, int NumberOfPeople, string Status, int CustomerId, int TableId, decimal? TotalAmount);
}
