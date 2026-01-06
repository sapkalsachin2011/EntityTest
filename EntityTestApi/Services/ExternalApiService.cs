using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EntityTestApi.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiService> _logger;

        public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetDataFromExternalApiAsync(string url)
        {
            _logger.LogInformation($"Requesting data from {url}");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // For comparison: direct HttpClient usage (not recommended for production)
        public static async Task<string> GetDataWithDirectHttpClientAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
