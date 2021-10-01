using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CDR.DataHolder.Public.API.Controllers
{
    [ApiController]
	[Route("cds-au")]
    public class DiscoveryController : ControllerBase
    {
        private readonly ILogger<DiscoveryController> _logger;

        public DiscoveryController(ILogger<DiscoveryController> logger)
        {
            _logger = logger;
        }

        [HttpGet("v1/discovery/status")]
        [ApiVersion("1")]
        [HttpGet]
        public async Task GetStatus()
        {
            _logger.LogInformation($"Request received to {nameof(DiscoveryController)}.{nameof(GetStatus)}");
            // var json = await System.IO.File.ReadAllTextAsync("data/status.json");
            var json = await System.IO.File.ReadAllTextAsync("Data/status.json");

            // Return the raw JSON response.
            Response.ContentType = "application/json";
            await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(json));
        }

        [HttpGet("v1/discovery/outages")]
        [ApiVersion("1")]
        [HttpGet]
        public async Task GetOutages()
        {
            _logger.LogInformation($"Request received to {nameof(DiscoveryController)}.{nameof(GetOutages)}");
            // var json = await System.IO.File.ReadAllTextAsync("data/outages.json");
            var json = await System.IO.File.ReadAllTextAsync("Data/outages.json");

            // Return the raw JSON response.
            Response.ContentType = "application/json";
            await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(json));
        }
    }
}
