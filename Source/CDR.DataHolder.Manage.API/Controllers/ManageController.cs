using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CDR.DataHolder.Domain.Entities;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CDR.DataHolder.Manage.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ManageController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ManageController> _logger;
        private readonly IStatusRepository _statusRepository;
        private readonly DataHolderDatabaseContext _dbContext;

        public ManageController(
            IConfiguration config,
            ILogger<ManageController> logger,
            DataHolderDatabaseContext dbContext,
            IStatusRepository statusRepository)
        {
            _config = config;
            _logger = logger;
            _statusRepository = statusRepository;
            _dbContext = dbContext;
        }

        [HttpPost]
        [Route("Metadata")]
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

        [HttpGet]
        [Route("Metadata")]
        public async Task GetData()
        {
            var metadata = await _dbContext.GetJsonFromDatabase(_logger);

            // Return the raw JSON response.
            Response.ContentType = "application/json";
            await Response.BodyWriter.WriteAsync(System.Text.UTF8Encoding.UTF8.GetBytes(metadata));
        }

        [HttpGet]
        [HttpPost]
        [Route("refresh-dr-metadata")]
        public async Task<IActionResult> RefreshDataRecipients()
        {
            _logger.LogInformation($"Request received to {nameof(ManageController)}.{nameof(RefreshDataRecipients)}");

            // Call the Register to get the data recipients list.
            var endpoint = _config["Register:GetDataRecipientsEndpoint"];
            var data = await GetData(endpoint, 2);

            // If data was retrieved, then update it in our repository.
            if (!string.IsNullOrEmpty(data))
            {
                await _statusRepository.RefreshDataRecipients(data);

                return Ok($"Data recipient records refreshed from {endpoint}.");
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Data recipient data could not be refreshed.");
        }

        [HttpGet]
        [HttpPost]
        [Route("refresh-dr-status")]
        public async Task<IActionResult> RefreshDataRecipientStatus()
        {
            _logger.LogInformation($"Request received to {nameof(ManageController)}.{nameof(RefreshDataRecipientStatus)}");

            // Call the Register to get the data recipient status list.
            var endpoint = _config["Register:GetDataRecipientStatusEndpoint"];
            var data = await GetData<DataRecipientStatus>(endpoint, 1, "dataRecipients");

            // If data was retrieved, then update it in our repository.
            if (data != null && data.Any())
            {
                foreach (var status in data)
                {
                    await _statusRepository.UpdateDataRecipientStatus(status);
                }

                return Ok($"{data.Count()} data recipient status records refreshed from {endpoint}.");
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Data recipient status data could not be refreshed.");
        }

        [HttpGet]
        [HttpPost]
        [Route("refresh-sp-status")]
        public async Task<IActionResult> RefreshSoftwareProductStatus()
        {
            _logger.LogInformation($"Request received to {nameof(ManageController)}.{nameof(RefreshSoftwareProductStatus)}");

            // Call the Register to get the software product status list.
            var endpoint = _config["Register:GetSoftwareProductsStatusEndpoint"];
            var data = await GetData<SoftwareProductStatus>(endpoint, 1, "softwareProducts");

            // If data was retrieved, then update it in our repository.
            if (data != null && data.Any())
            {
                foreach (var status in data)
                {
                    await _statusRepository.UpdateSoftwareProductStatus(status);
                }

                return Ok($"{data.Count()} software product status records refreshed from {endpoint}.");
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Software product status data could not be refreshed.");
        }

        private async Task<string> GetData(string endpoint, int version)
        {
            _logger.LogInformation($"Retrieving data from {endpoint} (x-v: {version})...");

            var httpClient = GetHttpClient();
            httpClient.DefaultRequestHeaders.Add("x-v", version.ToString());
            var response = await httpClient.GetAsync(endpoint);

            _logger.LogInformation($"Status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        private async Task<IEnumerable<T>> GetData<T>(string endpoint, int version, string rootNode)
        {
            var json = await GetData(endpoint, version);

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonConvert.DeserializeObject<JObject>(json);
                return data[rootNode].ToObject<List<T>>();
            }

            return null;
        }

        private HttpClient GetHttpClient()
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            return new HttpClient(clientHandler);
        }
    }
}
