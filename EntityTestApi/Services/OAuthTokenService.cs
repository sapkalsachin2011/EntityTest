using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace EntityTestApi.Services
{
    public class OAuthTokenService
    {
        private readonly HttpClient _httpClient;
        public OAuthTokenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GetTokenAsync(string tokenEndpoint, string clientId, string clientSecret, string audience)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("audience", audience)
                })
            };
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var token))
                return token.GetString();
            return null;
        }
    }
}
