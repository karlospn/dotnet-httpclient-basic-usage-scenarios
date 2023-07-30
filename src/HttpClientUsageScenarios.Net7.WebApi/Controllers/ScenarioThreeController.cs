using Microsoft.AspNetCore.Mvc;

namespace HttpClientUsageScenarios.Net7.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScenarioThreeController : ControllerBase
    {
        private static readonly HttpClient Client = new()
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
            DefaultRequestHeaders = { { "accept", "application/json" } },
            Timeout = TimeSpan.FromSeconds(15),
        };

        [HttpGet()]
        public async Task<ActionResult> Get()
        {

            var response = await Client.GetAsync(
                "posts/1/comments");

            if (response.IsSuccessStatusCode)
                return Ok(await response.Content.ReadAsStringAsync());

            return StatusCode(500);
        }
    }
}