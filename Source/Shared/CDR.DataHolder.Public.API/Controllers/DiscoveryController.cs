using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.Resource.API.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CDR.DataHolder.Public.API.Controllers
{
    [ApiController]
	[Route("cds-au")]
    public class DiscoveryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DiscoveryController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("v1/discovery/status")]
        [CheckXV(1, 1)]
        [ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetStatus()
        {
            var json = (await System.IO.File.ReadAllTextAsync("Data/status.json")).Replace("#{domain}", _configuration.GetValue<string>("Domain"));
            return Content(json, "application/json");
        }

        [HttpGet("v1/discovery/outages")]
        [CheckXV(1, 1)]
        [ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetOutages()
        {
            var json = (await System.IO.File.ReadAllTextAsync("Data/outages.json")).Replace("#{domain}", _configuration.GetValue<string>("Domain"));
            return Content(json, "application/json");
        }
    }
}
