using CDR.DataHolder.API.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.Admin.API.Controllers
{
    [ApiController]
    [Route("cds-au")]
    public class AdminController : ControllerBase
    {
        private const string emptyResult = "{}";

        public AdminController()
        {
        }

        [HttpGet("v1/admin/metrics")]
        [ApiVersion("1")]
        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public string GetMetrics()
        {
            return emptyResult;
        }

        [HttpGet("v1/admin/metrics")]
        [ApiVersion("2")]
        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public string GetMetricsV2()
        {
            return emptyResult;
        }
    }
}
