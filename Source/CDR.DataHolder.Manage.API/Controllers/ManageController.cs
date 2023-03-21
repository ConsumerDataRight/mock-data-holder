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

        public ManageController(ILogger<ManageController> logger,
                                DataHolderDatabaseContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
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
                await _dbContext.SeedDatabaseFromJson(json, _logger, true);
            }
            catch
            {
                // SeedDatabaseFromJson doesn't throw specific error exceptions, so lets just consider any exception a BadRequest
                return new BadRequestObjectResult(new { error = "Unexpected Error", detail = "An error occurred loading the database." });
            }

            return Ok();
        }
                
        private async Task<string> GetData(string endpoint, int version)
        {
            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("Retrieving data from {endpoint} (x-v: {version})...", endpoint, version);
            }

            var httpClient = GetHttpClient();
            httpClient.DefaultRequestHeaders.Add("x-v", version.ToString());
            var response = await httpClient.GetAsync(endpoint);

            _logger.LogInformation("Status code: {statusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }
        
        private static HttpClient GetHttpClient()
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            return new HttpClient(clientHandler);
        }
    }
}