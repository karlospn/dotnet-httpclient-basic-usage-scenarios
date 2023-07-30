using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace HttpClientUsageScenarios.NetFramework.WebApi.Controllers
{
    public class ScenarioSixController : ApiController
    {
        private readonly IHttpClientFactory _factory;

        public ScenarioSixController(IHttpClientFactory factory)
        {
            _factory = factory;
        }
        
        public async Task<IHttpActionResult> Get()
        {
            var client = _factory.CreateClient("typicode");

            var response = await client.GetAsync(
                "posts/1/comments");

            if (response.IsSuccessStatusCode)
                return Ok(await response.Content.ReadAsStringAsync());

            return InternalServerError();
        }

    }
}
