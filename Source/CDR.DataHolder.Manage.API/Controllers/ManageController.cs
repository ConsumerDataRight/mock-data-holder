using CDR.DataHolder.API.Infrastructure;
using CDR.DataHolder.API.Infrastructure.Filters;
using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CDR.DataHolder.Manage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ManageController : ControllerBase
    {
        private readonly ILogger<ManageController> _logger;
        private readonly DataHolderDatabaseContext _dbContext;
        private readonly HealthCheckStatuses _healthCheckStatuses;

        public ManageController(ILogger<ManageController> logger,
                                DataHolderDatabaseContext dbContext, HealthCheckStatuses healthCheckStatuses)
        {
            _logger = logger;
            _dbContext = dbContext;
            _healthCheckStatuses=healthCheckStatuses;
        }

        [HttpPost]
        [Route("Metadata")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> LoadData()
        {
            using var reader = new StreamReader(Request.Body);
            string json = await reader.ReadToEndAsync();

            try
            {
                await _dbContext.SeedDatabaseFromJson(json, _logger, _healthCheckStatuses, true);
            }
            catch
            {
                // SeedDatabaseFromJson doesn't throw specific error exceptions, so lets just consider any exception a BadRequest
                return new BadRequestObjectResult(new { error = "Unexpected Error", detail = "An error occurred loading the database." });
            }

            return Ok();
        }        
    }
}