using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CDR.DataHolder.Admin.API.Controllers
{
    [ApiController]
    [Route("cds-au")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        [HttpGet("v1/admin/metrics")]
        [ApiVersion("1")]
        [HttpGet]
        public string GetMetrics()
        {
            return "{}";
        }

        [HttpGet("v1/admin/metrics")]
        [ApiVersion("2")]
        [HttpGet]
        public string GetMetricsV2()
        {
            return "{}";
        }
    }
}
