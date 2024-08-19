using CDR.DataHolder.Manage.API.Infrastructure;
using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.Repository;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace CDR.DataHolder.Manage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ManageController : ControllerBase
    {
        private readonly ILogger<ManageController> _logger;
        private readonly DbContext _dbContext;
        private readonly HealthCheckStatuses _healthCheckStatuses;

        public ManageController(ILogger<ManageController> logger,
                                IndustryDbContextFactory dbContextFactory,                                 
                                HealthCheckStatuses healthCheckStatuses,
                                IConfiguration configuration)
        {
            _logger = logger;
            var _industry = configuration.GetValue<string>("Industry") ?? string.Empty;
            
            _dbContext = (DbContext)dbContextFactory.Create(_industry, DbConstants.ConnectionStringType.Default);
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