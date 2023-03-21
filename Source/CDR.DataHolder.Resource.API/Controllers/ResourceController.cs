using AutoMapper;
using CDR.DataHolder.API.Infrastructure.Authorization;
using CDR.DataHolder.API.Infrastructure.Filters;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Domain.ValueObjects;
using CDR.DataHolder.Resource.API.Business;
using CDR.DataHolder.Resource.API.Business.Filters;
using CDR.DataHolder.Resource.API.Business.Models;
using CDR.DataHolder.Resource.API.Business.Responses;
using CDR.DataHolder.Resource.API.Business.Services;
using CDR.DataHolder.Resource.API.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static CDR.DataHolder.API.Infrastructure.Constants;

namespace CDR.DataHolder.Resource.API.Controllers
{
    [Route("cds-au")]
	[ApiController]
	[Authorize]
	public class ResourceController : ControllerBase
	{
		private readonly IResourceRepository _resourceRepository;
		private readonly IConfiguration _config;
		private readonly IMapper _mapper;
		private readonly ILogger<ResourceController> _logger;
		private readonly ITransactionsService _transactionsService;
		private readonly IIdPermanenceManager _idPermanenceManager;

		public ResourceController(
			IResourceRepository resourceRepository,
			IConfiguration config,
			IMapper mapper,
			ILogger<ResourceController> logger,
			ITransactionsService transactionsService,
			IIdPermanenceManager idPermanenceManager)
		{
			_resourceRepository = resourceRepository;
			_config = config;
			_mapper = mapper;
			_logger = logger;
			_transactionsService = transactionsService;
			_idPermanenceManager = idPermanenceManager;
		}

		[PolicyAuthorize(AuthorisationPolicy.GetCustomersApi)]
		[HttpGet("v1/common/customer", Name = "GetCustomer")]
		[CheckScope("common:customer.basic:read")]
		[CheckXV(1, 1)]
		[CheckAuthDate]
		[ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
		public async Task<IActionResult> GetCustomer()
		{
            // Each customer login id is different for each ADR based on PPID.
            // Therefore we need to look up the CustomerClient table to find the actual customer login id.
            // This can be done once we have a client id (Registration) and a valid access token.
            var loginId = GetCustomerLoginId(this.User);
			if ( string.IsNullOrEmpty(loginId))
			{
				// Implement response handling when the acceptance criteria is available.
				return BadRequest();
			}

            //ResponseCommonCustomer Mapper not working because of the existing schema for ResponseCommonCustomer
            //GetCustomerByLoginId to match schema for ResponseCommonCustomer
            var response = _mapper.Map<ResponseCommonCustomer>(await _resourceRepository.GetCustomerByLoginId(loginId));
            
            if (response == null)
			{
				return BadRequest();
			}

			response.Links = this.GetLinks(nameof(GetCustomer), _config);

			return Ok(response);
		}

		[PolicyAuthorize(AuthorisationPolicy.GetAccountsApi)]
		[HttpGet("v1/banking/accounts", Name = nameof(GetAccounts))]
		[CheckScope(ApiScopes.Banking.AccountsBasicRead)]
		[CheckXV(1, 1)]
		[CheckAuthDate]
		[ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
		public async Task<IActionResult> GetAccounts(
			[FromQuery(Name = "is-owned")] bool? isOwned,
			[FromQuery(Name = "open-status"), CheckOpenStatus] string openStatus,
			[FromQuery(Name = "product-category"), CheckProductCategory] string productCategory,
			[FromQuery(Name = "page"), CheckPage] string page,
			[FromQuery(Name = "page-size"), CheckPageSize] string pageSize)
		{
			// Each customer id is different for each ADR based on PPID.
			// Therefore we need to look up the CustomerClient table to find the actual customer id.
			// This can be done once we have a client id (Registration) and a valid access token.
			var loginId = GetCustomerLoginId(this.User);
			if (string.IsNullOrEmpty(loginId))
			{
				return new BadRequestObjectResult(new ResponseErrorList(Error.UnknownError()));
			}

			// Get accounts
            var accountFilter = new AccountFilter(GetAccountIds(User))
			{                                
                IsOwned = isOwned,
				ProductCategory = productCategory,
				OpenStatus = (openStatus != null && openStatus.Equals(OpenStatus.All.ToString(), StringComparison.OrdinalIgnoreCase)) ? null : openStatus,
			};
			int pageNumber = string.IsNullOrEmpty(page) ? 1 : int.Parse(page);
			int pageSizeNumber = string.IsNullOrEmpty(pageSize) ? 25 : int.Parse(pageSize);
			var accounts = await _resourceRepository.GetAllAccounts(accountFilter, pageNumber, pageSizeNumber);
			var response = _mapper.Map<ResponseBankingAccountList>(accounts);

			// Check if the given page number is out of range
			if (pageNumber != 1 && pageNumber > response.Meta.TotalPages.GetValueOrDefault())
			{
				return new BadRequestObjectResult(new ResponseErrorList(Error.PageOutOfRange()));
			}

			var softwareProductId = this.User.FindFirst(Constants.TokenClaimTypes.SoftwareId)?.Value;
			var idParameters = new IdPermanenceParameters
			{
				SoftwareProductId = softwareProductId,                
                CustomerId = loginId
			};

			_idPermanenceManager.EncryptIds(response.Data.Accounts, idParameters, a => a.AccountId);

			// Set pagination meta data
			response.Links = this.GetLinks(nameof(GetAccounts), _config, pageNumber, response.Meta.TotalPages.GetValueOrDefault(), pageSizeNumber);

			return Ok(response);
		}

		[PolicyAuthorize(AuthorisationPolicy.GetTransactionsApi)]
		[HttpGet("v1/banking/accounts/{accountId}/transactions", Name = nameof(GetTransactions))]
		[CheckScope(ApiScopes.Banking.TransactionsRead)]
		[CheckXV(1, 1)]
		[CheckAuthDate]
		[ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
		public async Task<IActionResult> GetTransactions([FromQuery] RequestAccountTransactions request)
		{
            // Each customer id is different for each ADR based on PPID.
            // customer id is not required for account when account id is available
            // This can be done once we have a client id (Registration) and a valid access token.            
            var loginId = GetCustomerLoginId(this.User);
                        
            if (string.IsNullOrEmpty(loginId))
            {
                return new BadRequestObjectResult(new ResponseErrorList(Error.UnknownError()));
            }
			            
			var softwareProductId = this.User.FindFirst(Constants.TokenClaimTypes.SoftwareId)?.Value;

			// Decrypt the incoming account id (ID Permanence rules).
			var idParameters = new IdPermanenceParameters
			{
				SoftwareProductId = softwareProductId,
				CustomerId = loginId
			};
			
			request.AccountId = DecryptAccountId(request.AccountId, idParameters);

			if (string.IsNullOrEmpty(request.AccountId))
			{
				using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
				{
					_logger.LogError("Account Id could not be retrived from request.");
				}
				return new NotFoundObjectResult(new ResponseErrorList(Error.NotFound("Account ID could not be found for the customer")));
			}
			else
			{
				if (!(await _resourceRepository.CanAccessAccount(request.AccountId)))
				{
					// A valid consent exists with bank:transactions:read scope but this Account Id could not be found for the supplied Customer Id.
					// This scenario will take precedence
					using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
					{
						_logger.LogInformation("Customer does not have access to this Account Id. Account Id: {accountId}", request.AccountId);
					}
					return new NotFoundObjectResult(new ResponseErrorList(Error.NotFound("Account ID could not be found for the customer")));
				}

				if (!GetAccountIds(User).Contains(request.AccountId))
				{
					// A valid consent exists with bank:transactions:read scope and the Account Id can be found for the supplied customer
					// but this Account Id is not in the list of consented Account Ids
					using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
					{
						_logger.LogInformation("Consent has not been granted for this Account Id: {accountId}", request.AccountId);
					}
					return new NotFoundObjectResult(new ResponseErrorList(Error.ConsentNotFound()));
				}
			}

			var page = string.IsNullOrEmpty(request.Page) ? 1 : int.Parse(request.Page);
			var pageSize = string.IsNullOrEmpty(request.PageSize) ? 25 : int.Parse(request.PageSize);
			var response = await _transactionsService.GetAccountTransactions(request, page, pageSize);

			_idPermanenceManager.EncryptIds(response.Data.Transactions, idParameters, t => t.AccountId, t => t.TransactionId);

			// Set pagination meta data
			response.Links = this.GetLinks(nameof(GetTransactions), _config, page, response.Meta.TotalPages.GetValueOrDefault(), pageSize);

			return new OkObjectResult(await Task.FromResult(response));
		}

		private string DecryptAccountId(string encryptedAccountId, IdPermanenceParameters idParameters)
		{
			string accountId = null;

			try
			{
				// Get the underlying Account Id from the the Account Permanence Id in the request.
				accountId = _idPermanenceManager.DecryptId(encryptedAccountId, idParameters);
			}
			catch (Exception ex)
			{
				using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
				{
					_logger.LogError(ex, "Could not decrypt account id.");
				}
			}

			return accountId;
		}

        private static string GetCustomerLoginId(ClaimsPrincipal principal)
		{
			var loginId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;			
			if (string.IsNullOrEmpty(loginId))
			{
				return string.Empty;
			}
			return loginId;
		}
		
        private static string[] GetAccountIds(ClaimsPrincipal principal)
		{
			// Check if consumer has granted consent to this account Id
			return principal.FindAll(Constants.TokenClaimTypes.AccountId)
				.Select(c => c.Value)
				.ToArray();
		}
	}
}
