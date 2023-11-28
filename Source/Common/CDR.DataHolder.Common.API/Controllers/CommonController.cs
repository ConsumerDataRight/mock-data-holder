using AutoMapper;
using CDR.DataHolder.Common.API.Infrastructure;
using CDR.DataHolder.Common.Resource.API.Business.Responses;
using CDR.DataHolder.Shared.API.Infrastructure.Authorization;
using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.Business;
using CDR.DataHolder.Shared.Resource.API.Business.Filters;
using CDR.DataHolder.Shared.Resource.API.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CDR.DataHolder.Shared.Domain.Constants;

namespace CDR.DataHolder.Common.API.Controllers
{
    [Route("cds-au")]
    [ApiController]
    [Authorize]
    public class CommonController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _industry;

        public CommonController(
            IMapper mapper,
            IConfiguration config,
            IServiceProvider serviceProvider
            )
        {
            _mapper = mapper;
            _config = config;
            _serviceProvider=serviceProvider;
            _industry = config.GetValue<string>("Industry")?? Industry.Banking;
        }

        [PolicyAuthorize(AuthorisationPolicy.GetCustomersApi)]
        [HttpGet("v1/common/customer", Name = "GetCustomer")]
        [CheckScope(CDR.DataHolder.Shared.API.Infrastructure.Constants.ApiScopes.Common.CustomerBasicRead)]
        [CheckXV(1, 1)]
        [CheckAuthDate]
        [ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetCustomer()
        {

            // Each customer id is different for each ADR based on PPID.
            // Therefore we need to look up the CustomerClient table to find the actual customer id.
            // This can be done once we have a client id (Registration) and a valid access token.
            var loginId = User.GetCustomerLoginId();
            if (string.IsNullOrEmpty(loginId))
            {
                // Implement response handling when the acceptance criteria is available.
                return BadRequest();
            }

            var commonRepository = _serviceProvider.GetCommonRepository(_industry);

            ResponseCommonCustomer response;

            var customer = await commonRepository.GetCustomerByLoginId(loginId);
            response = _mapper.Map<ResponseCommonCustomer>(customer);

            if (response == null)
            {
                return BadRequest();
            }

            response.Links = this.GetLinks(_config);            

            return Ok(response);
        }
    }
}