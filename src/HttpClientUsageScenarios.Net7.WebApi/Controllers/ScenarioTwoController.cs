using Microsoft.AspNetCore.Mvc;

namespace HttpClientUsageScenarios.Net7.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScenarioTwoController : ControllerBase
    {
        
        [HttpGet()]
        public async Task<ActionResult> Get()
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri("https://jsonplaceholder.typicode.com/"),
                DefaultRequestHeaders = { { "accept", "application/json" } },
                Timeout = TimeSpan.FromSeconds(15)
            };

            var response = await client.GetAsync(
                "posts/1/comments");

            if (response.IsSuccessStatusCode)
                return Ok(await response.Content.ReadAsStringAsync());

            return StatusCode(500);
        }
    }
}