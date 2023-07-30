using Microsoft.AspNetCore.Mvc;

namespace HttpClientUsageScenarios.Net7.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScenarioFiveController : ControllerBase
    {
        private readonly IHttpClientFactory _factory;

        public ScenarioFiveController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        [HttpGet()]
        public async Task<ActionResult> Get()
        {
            var client = _factory.CreateClient("typicode");

            var response = await client.GetAsync(
                "posts/1/comments");

            if (response.IsSuccessStatusCode)
                return Ok(await response.Content.ReadAsStringAsync());

            return StatusCode(500);
        }
    }
}