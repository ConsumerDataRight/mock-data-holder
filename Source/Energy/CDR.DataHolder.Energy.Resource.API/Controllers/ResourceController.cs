using AutoMapper;
using CDR.DataHolder.Energy.Domain.Repositories;
using CDR.DataHolder.Energy.Domain.ValueObjects;
using CDR.DataHolder.Energy.Resource.API.Business.Models;
using CDR.DataHolder.Energy.Resource.API.Business.Responses;
using CDR.DataHolder.Shared.API.Infrastructure.Authorization;
using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.Business;
using CDR.DataHolder.Shared.Domain.Models;
using CDR.DataHolder.Shared.Domain.ValueObjects;
using CDR.DataHolder.Shared.Resource.API.Business.Filters;
using CDR.DataHolder.Shared.Resource.API.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using Infra = CDR.DataHolder.Shared.API.Infrastructure;

namespace CDR.DataHolder.Energy.Resource.API.Controllers
{
    [Route("cds-au")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class ResourceController : ControllerBase
    {
        private readonly IEnergyResourceRepository _resourceRepository;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly ILogger<ResourceController> _logger;
        private readonly IIdPermanenceManager _idPermanenceManager;

        public ResourceController(
            IEnergyResourceRepository resourceRepository,
            IConfiguration config,
            IMapper mapper,
            ILogger<ResourceController> logger,
            IIdPermanenceManager idPermanenceManager)
        {
            _resourceRepository = resourceRepository;
            _config = config;
            _mapper = mapper;
            _logger = logger;
            _idPermanenceManager = idPermanenceManager;
        }

        [PolicyAuthorize(AuthorisationPolicy.GetAccountsApi)]
        [HttpGet("v1/energy/accounts", Name = nameof(GetEnergyAccountsXV1))]
        [CheckScope(CDR.DataHolder.Shared.API.Infrastructure.Constants.ApiScopes.Energy.AccountsBasicRead)]
        [CheckXV(1, 1)]
        [CheckAuthDate]
        [ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetEnergyAccountsXV1(
            [FromQuery(Name = "page"), CheckPage] string? page,
            [FromQuery(Name = "page-size"), CheckPageSize] string? pageSize)
        {
            // Create the filter
            var accountIds = User.GetAccountIds();
            var accountFilter = new AccountFilter(accountIds);

            return await GetPagedEnergyAccountsForFilter<EnergyAccount>(page, pageSize, accountFilter);
        }

        private async Task<IActionResult> GetPagedEnergyAccountsForFilter<T>(string? page, string? pageSize, AccountFilter accountFilter)
            where T : BaseEnergyAccount
        {
            // Each customer id is different for each ADR based on PPID.
            // Therefore we need to look up the CustomerClient table to find the actual customer id.
            // This can be done once we have a client id (Registration) and a valid access token.
            var loginId = this.User.GetCustomerLoginId();
            if (string.IsNullOrEmpty(loginId))
            {
                return new BadRequestObjectResult(new ResponseErrorList().AddUnexpectedError());
            }

            int pageNumber = string.IsNullOrEmpty(page) ? 1 : int.Parse(page);
            int pageSizeNumber = string.IsNullOrEmpty(pageSize) ? 25 : int.Parse(pageSize);
            var accounts = await _resourceRepository.GetAllEnergyAccounts(accountFilter, pageNumber, pageSizeNumber);
            var response = _mapper.Map<EnergyAccountListResponse<T>>(accounts);

            // Check if the given page number is out of range
            var totalPages = response.Meta.TotalPages.GetValueOrDefault();
            if (pageNumber != 1 && pageNumber > totalPages)
            {
                return new UnprocessableEntityObjectResult(new ResponseErrorList().AddPageOutOfRange(totalPages));
            }

            var softwareProductId = this.User.FindFirst(Infra.Constants.TokenClaimTypes.SoftwareId)?.Value;
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId ?? string.Empty,
                CustomerId = loginId
            };

            _idPermanenceManager.EncryptIds(response.Data.Accounts, idParameters, a => a.AccountId);

            // Set pagination meta data
            response.Links = this.GetLinks(_config, pageNumber, response.Meta.TotalPages.GetValueOrDefault(), pageSizeNumber);

            return Ok(response);
        }

        [PolicyAuthorize(AuthorisationPolicy.GetAccountsApi)]
        [HttpGet("v1/energy/accounts", Name = nameof(GetEnergyAccountsXV2))]
        [CheckScope(Shared.API.Infrastructure.Constants.ApiScopes.Energy.AccountsBasicRead)]
        [CheckXV(2, 2)]
        [CheckAuthDate]
        [ApiVersion("2")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetEnergyAccountsXV2(
            [FromQuery(Name = "open-status"), CheckOpenStatus] string? openStatus,
            [FromQuery(Name = "page"), CheckPage] string? page,
            [FromQuery(Name = "page-size"), CheckPageSize] string? pageSize)
        {
            // Create the filter
            var accountIds = User.GetAccountIds();
            var accountFilter = new AccountFilter(accountIds)
            {
                OpenStatus = (openStatus != null && openStatus.Equals(OpenStatus.All.ToString(), StringComparison.OrdinalIgnoreCase)) ? null : openStatus
            };

            return await GetPagedEnergyAccountsForFilter<EnergyAccountV2>(page, pageSize, accountFilter);
        }

        [PolicyAuthorize(AuthorisationPolicy.GetConcessionsApi)]
        [HttpGet("v1/energy/accounts/{accountId}/concessions", Name = nameof(GetConcessions))]
        [CheckScope(Shared.API.Infrastructure.Constants.ApiScopes.Energy.ConcessionsRead)]
        [CheckXV(1, 1)]
        [CheckAuthDate]
        [ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetConcessions([FromRoute] string accountId)
        {
            using (LogContext.PushProperty("MethodName", nameof(GetConcessions)))
            {
                _logger.LogInformation($"Request received to {nameof(ResourceController)}.{nameof(GetConcessions)}");
            }

            var request = new RequestAccountConcessions()
            {
                AccountId = accountId
            };

            // Each customer id is different for each ADR based on PPID.
            // customer id is not required for account when account id is available
            // This can be done once we have a client id (Registration) and a valid access token.
            var loginId = User.GetCustomerLoginId();

            if (string.IsNullOrEmpty(loginId))
            {
                return new BadRequestObjectResult(new ResponseErrorList().AddUnknownError());
            }

            // Decrypt the incoming account id (ID Permanence rules).
            var softwareProductId = this.User.FindFirst(Infra.Constants.TokenClaimTypes.SoftwareId)?.Value;
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId ?? string.Empty,
                CustomerId = loginId
            };

            request.AccountId = DecryptAccountId(request.AccountId, idParameters);

            if (string.IsNullOrEmpty(request.AccountId))
            {
                using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"]?.ToString()))
                {
                    _logger.LogError("Account Id could not be retrieved from request.");
                }

                return new NotFoundObjectResult(new ResponseErrorList().AddInvalidEnergyAccount(accountId));
            }
            else
            {
                if (!(await _resourceRepository.CanAccessAccount(request.AccountId)))
                {
                    // A valid consent exists with bank:transactions:read scope but this Account Id could not be found for the supplied Customer Id.
                    // This scenario will take precedence
                    using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"]?.ToString()))
                    {
                        _logger.LogInformation("Customer does not have access to this Account Id. Account Id: {AccountId}", request.AccountId);
                    }

                    return new NotFoundObjectResult(new ResponseErrorList().AddInvalidEnergyAccount(accountId));
                }

                if (!User.GetAccountIds().Contains(request.AccountId))
                {
                    // A valid consent exists with bank:transactions:read scope and the Account Id can be found for the supplied customer
                    // but this Account Id is not in the list of consented Account Ids
                    using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"]?.ToString()))
                    {
                        _logger.LogInformation("Consent has not been granted for this Account Id: {AccountId}", request.AccountId);
                    }

                    return new NotFoundObjectResult(new ResponseErrorList().AddConsentNotFound(_config.GetValue<string>("Industry")));
                }
            }

            var filters = _mapper.Map<AccountConcessionsFilter>(request);
            var concessions = await _resourceRepository.GetEnergyAccountConcessions(filters);
            var response = _mapper.Map<EnergyConcessionsResponse>(concessions);

            // Set pagination meta data
            response.Links = this.GetLinks(_config);

            return Ok(response);
        }

        private string DecryptAccountId(string encryptedAccountId, IdPermanenceParameters idParameters)
        {
            string accountId = string.Empty;

            try
            {
                // Get the underlying Account Id from the Account Permanence Id in the request.
                accountId = _idPermanenceManager.DecryptId(encryptedAccountId, idParameters);
            }
            catch (Exception ex)
            {
                using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"]?.ToString()))
                {
                    _logger.LogError(ex, "Could not decrypt account id.");
                }
            }

            return accountId;
        }
    }
}
