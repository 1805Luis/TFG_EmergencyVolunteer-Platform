using System.Net.Http.Headers;
using System.Text.Json;

namespace AssigmentService.Service
{
    public class HttpService
    {
        private readonly HttpClient _httpClient;

        public HttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T?> GetAsync<T>(string url, string? token = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
                }

                var response = await _httpClient.SendAsync(request);

                var json = await response.Content.ReadAsStringAsync(); 
                Console.WriteLine($"Contenido JSON crudo: {json}");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error de solicitud HTTP a {url}: {e.Message}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general a {url}: {ex.Message}");
                return default;
            }
        }
    }
}
