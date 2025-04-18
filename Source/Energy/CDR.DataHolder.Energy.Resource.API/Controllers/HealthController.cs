﻿using CDR.DataHolder.Shared.API.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.Energy.Resource.API.Controllers
{
    [Route("health")]
    public class HealthController : Controller
    {
        [HttpGet("status")]
        public IActionResult Index()
        {
            return Json(new Health() { Status = "OK" });
        }
    }
}
