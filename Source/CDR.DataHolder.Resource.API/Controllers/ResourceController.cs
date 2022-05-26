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
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CDR.DataHolder.Resource.API.Controllers
{
    [Route("cds-au")]
	[ApiController]
	[Authorize]
	public class ResourceController : ControllerBase
	{
		private readonly IResourceRepository _resourceRepository;
		private readonly IStatusRepository _statusRepository;
		private readonly IMapper _mapper;
		private readonly ILogger<ResourceController> _logger;
		private readonly ITransactionsService _transactionsService;
		private readonly IIdPermanenceManager _idPermanenceManager;

		public ResourceController(IResourceRepository resourceRepository,
									IStatusRepository statusRepository,
									IMapper mapper,
									ILogger<ResourceController> logger,
									ITransactionsService transactionsService,
									IIdPermanenceManager idPermanenceManager)
		{
			_resourceRepository = resourceRepository;
			_statusRepository = statusRepository;
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
			// Each customer id is different for each ADR based on PPID.
			// Therefore we need to look up the CustomerClient table to find the actual customer id.
			// This can be done once we have a client id (Registration) and a valid access token.
			var customerId = GetCustomerId(this.User);
			if (customerId == Guid.Empty)
			{
				// Implement response handling when the acceptance criteria is available.
				return BadRequest();
			}

			// Check the status of the data recipient.
			var statusErrors = await ValidateDataRecipientStatus();
			if (statusErrors.HasErrors())
			{
				return new DataHolderForbidResult(statusErrors);
			}

			var response = _mapper.Map<ResponseCommonCustomer>(await _resourceRepository.GetCustomer(customerId));
			if (response == null)
			{
				return BadRequest();
			}

			response.Links = this.GetLinks("GetCustomer");

			return Ok(response);
		}

		[PolicyAuthorize(AuthorisationPolicy.GetAccountsApi)]
		[HttpGet("v1/banking/accounts", Name = nameof(GetAccounts))]
		[CheckScope("bank:accounts.basic:read")]
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
			var customerId = GetCustomerId(this.User);
			if (customerId == Guid.Empty)
			{
				return new BadRequestObjectResult(new ResponseErrorList(Error.UnknownError()));
			}

			// Check the status of the data recipient.
			var statusErrors = await ValidateDataRecipientStatus();
			if (statusErrors.HasErrors())
			{
				return new DataHolderForbidResult(statusErrors);
			}

			// Get accounts
			var accountFilter = new AccountFilter(GetAccountIds(User))
			{
				CustomerId = customerId,
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
				CustomerId = customerId.ToString()
			};

			_idPermanenceManager.EncryptIds(response.Data.Accounts, idParameters, a => a.AccountId);

			// Set pagination meta data
			response.Links = this.GetLinks(nameof(GetAccounts), pageNumber, response.Meta.TotalPages.GetValueOrDefault(), pageSizeNumber);

			return Ok(response);
		}

		[PolicyAuthorize(AuthorisationPolicy.GetTransactionsApi)]
		[HttpGet("v1/banking/accounts/{accountId}/transactions", Name = nameof(GetTransactions))]
		[CheckScope("bank:transactions:read")]
		[CheckXV(1, 1)]
		[CheckAuthDate]
		[ApiVersion("1")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
		public async Task<IActionResult> GetTransactions([FromQuery] RequestAccountTransactions request)
		{
			// Each customer id is different for each ADR based on PPID.
			// Therefore we need to look up the CustomerClient table to find the actual customer id.
			// This can be done once we have a client id (Registration) and a valid access token.
			request.CustomerId = GetCustomerId(this.User);
			if (request.CustomerId == Guid.Empty)
			{
				return new BadRequestObjectResult(new ResponseErrorList(Error.UnknownError()));
			}

			// Check the status of the data recipient.
			var statusErrors = await ValidateDataRecipientStatus();
			if (statusErrors.HasErrors())
			{
				return new DataHolderForbidResult(statusErrors);
			}

			var softwareProductId = this.User.FindFirst(Constants.TokenClaimTypes.SoftwareId)?.Value;

			// Decrypt the incoming account id (ID Permanence rules).
			var idParameters = new IdPermanenceParameters
			{
				SoftwareProductId = softwareProductId,
				CustomerId = request.CustomerId.ToString(),
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
				if (!(await _resourceRepository.CanAccessAccount(request.AccountId, request.CustomerId)))
				{
					// A valid consent exists with bank:transactions:read scope but this Account Id could not be found for the supplied Customer Id.
					// This scenario will take precedence
					using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
					{
						_logger.LogInformation("Customer does not have access to this Account Id. Customer Id: {customerId}, Account Id: {accountId}", request.CustomerId, request.AccountId);
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
			response.Links = this.GetLinks(nameof(GetTransactions), page, response.Meta.TotalPages.GetValueOrDefault(), pageSize);

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

		private async Task<ResponseErrorList> ValidateDataRecipientStatus()
		{
			ResponseErrorList errorList = new ResponseErrorList();

			// Get the Product Id from the token
			var softwareProductId = this.GetSoftwareProductId();
			if (softwareProductId == null)
			{
				errorList.Errors.Add(Error.UnknownError());
				return errorList;
			}

			// Get the latest data recipient details from the repository
			var softwareProduct = await _statusRepository.GetSoftwareProduct(softwareProductId.Value);
			if (softwareProduct == null)
			{
				errorList.Errors.Add(Error.DataRecipientSoftwareProductNotActive());
				return errorList;
			}

			if (String.Equals(softwareProduct.Brand.LegalEntity.Status, "Removed", StringComparison.OrdinalIgnoreCase))
			{
				errorList.Errors.Add(Error.DataRecipientParticipationRemoved());
			}
			else if (String.Equals(softwareProduct.Brand.LegalEntity.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
			{
				errorList.Errors.Add(Error.DataRecipientParticipationSuspended());
			}
			else if (String.Equals(softwareProduct.Brand.LegalEntity.Status, "Revoked", StringComparison.OrdinalIgnoreCase))
			{
				errorList.Errors.Add(Error.DataRecipientParticipationRevoked());
			}
			else if (String.Equals(softwareProduct.Brand.LegalEntity.Status, "Surrendered", StringComparison.OrdinalIgnoreCase))
			{
				errorList.Errors.Add(Error.DataRecipientParticipationSurrendered());
			}
			else if (!softwareProduct.Brand.IsActive || 
				String.Equals(softwareProduct.Brand.LegalEntity.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
			{
				errorList.Errors.Add(Error.DataRecipientParticipationNotActive());
			}			

			if (String.Equals(softwareProduct.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                errorList.Errors.Add(Error.DataRecipientSoftwareProductNotActive());
            }
            else if (String.Equals(softwareProduct.Status, "Removed", StringComparison.OrdinalIgnoreCase))
            {
                errorList.Errors.Add(Error.DataRecipientSoftwareProductStatusRemoved());
            }

            return errorList;
		}

		private static Guid GetCustomerId(ClaimsPrincipal principal)
		{
			var customerId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(customerId, out var customerIdGuid))
			{
				return Guid.Empty;
			}
			return customerIdGuid;
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
