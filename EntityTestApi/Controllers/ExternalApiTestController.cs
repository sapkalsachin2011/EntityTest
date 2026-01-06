using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EntityTestApi.Services;

namespace EntityTestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/externalapi")]
    public class ExternalApiTestController : ControllerBase
    {
        private readonly ExternalApiService _externalApiService;

        public ExternalApiTestController(ExternalApiService externalApiService)
        {
            _externalApiService = externalApiService;
        }

        [HttpGet("compare")] // GET api/externalapi/compare?url=...
        public async Task<IActionResult> CompareHttpClientMethods([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Please provide a valid URL as a query parameter.");

            var factoryResult = await _externalApiService.GetDataFromExternalApiAsync(url);
            var directResult = await ExternalApiService.GetDataWithDirectHttpClientAsync(url);

            return Ok(new
            {
                IHttpClientFactory = factoryResult,
                DirectHttpClient = directResult
            });
        }
    }
}
