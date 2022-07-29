// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using CDR.DataHolder.IdentityServer.Validation;
using CDR.DataHolder.IdentityServer.Models.UI;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using Serilog.Context;
using CDR.DataHolder.IdentityServer.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using CDR.DataHolder.IdentityServer.Extensions;
using static CDR.DataHolder.API.Infrastructure.Constants;

namespace CDR.DataHolder.IdentityServer.Controllers
{
	/// <summary>
	/// This controller processes the consent UI
	/// </summary>
	[SecurityHeaders]
	[Authorize]
	public class ConsentController : Controller
	{
		private readonly IIdentityServerInteractionService _interaction;
		private readonly IConfiguration _configuration;
		private readonly IEventService _events;
		private readonly ILogger<ConsentController> _logger;
		private readonly IResourceRepository _resourceRepository;

		public ConsentController(
			IIdentityServerInteractionService interaction,
			IConfiguration configuration,
			IEventService events,
			IResourceRepository resourceRepository,
			ILogger<ConsentController> logger)
		{
			_interaction = interaction;
			_configuration = configuration;
			_events = events;
			_resourceRepository = resourceRepository;
			_logger = logger;
		}

		/// <summary>
		/// Shows the consent screen
		/// </summary>
		/// <param name="returnUrl"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<IActionResult> Index(string returnUrl)
		{
			var vm = await BuildViewModelAsync(returnUrl);
			if (vm != null)
			{
				return View("Index", vm);
			}

			return View("Error");
		}

		/// <summary>
		/// Handles the consent screen postback
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Index(ConsentInputModel model)
		{
			var result = await ProcessConsent(model);
			if (result.IsRedirect)
			{
				return Redirect(_configuration.EnsurePath(result.RedirectUri));
			}

			if (result.HasValidationError)
			{
				ModelState.AddModelError(string.Empty, result.ValidationError);
			}

			if (result.ShowView)
			{
				return View("Index", result.ViewModel);
			}

			return View("Error");

		}

		/*****************************************/
		/* helper APIs for the ConsentController */
		/*****************************************/
		private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel model)
		{
			var result = new ProcessConsentResult();

			// validate return url is still valid
			var request = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
			if (request == null)
			{
				return result;
			}

			ConsentResponse grantedConsent = null;

			switch (model?.Button)
			{
				case ConsentViewModel.ActionTypes.Cancel:
					grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };
					await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
					break;

				case ConsentViewModel.ActionTypes.Page1:
					model.SelectedAccountIds = Array.Empty<string>();
					break;

				case ConsentViewModel.ActionTypes.Page2:
					// Check if any accounts are selected. If not show error.
					break;

				case ConsentViewModel.ActionTypes.Consent:

					// Auto-consent to all the requested scopes because we don't give the user to consent to each one, but we show the relavent information.
					List<string> consentedScopes = new List<string>();
					consentedScopes.AddRange(request.ValidatedResources.ParsedScopes.Select(s => s.ParsedName));
					if (request.ValidatedResources.Resources.OfflineAccess)
					{
						consentedScopes.Add(StandardScopes.OfflineAccess);
					}
					
					// Always remember consent, set expiry based on the user or client settings.
					grantedConsent = new ConsentResponse
					{
						RememberConsent = model.RememberConsent,
						ScopesValuesConsented = consentedScopes,
						Description = model.Description
					};
					// emit event
					await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent));
					break;

				default:
					result.ValidationError = "ConsentOptions.InvalidSelectionErrorMessage";
					break;
			}

			if (grantedConsent != null)
			{
				// communicate outcome of consent back to identityserver
				await _interaction.GrantConsentAsync(request, grantedConsent);

				// indicate that's it ok to redirect back to authorization endpoint
				result.RedirectUri = model.ReturnUrl;
				result.Client = request.Client;

				// Create the user session again with the new claims for the selected account ids.
				var accountIdClaims = model.SelectedAccountIds
					.Select(id => new Claim(StandardClaims.AccountId, id));
				var isuser = new IdentityServer4.IdentityServerUser(User.GetSubjectId().ToString())
				{
					DisplayName = User.GetDisplayName(),
					AdditionalClaims = accountIdClaims.ToArray()
				};
				await HttpContext.SignInAsync(isuser, null);
			}
			else
			{
				// we need to redisplay the consent UI
				result.ViewModel = await BuildViewModelAsync(model.ReturnUrl, model);
			}

			return result;
		}

		private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null)
		{
			var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
			if (request != null)
			{
				return await CreateConsentViewModel(model, returnUrl, request);
			}
			else
			{
				using (LogContext.PushProperty("MethodName", "BuildViewModelAsync"))
				{
					_logger.LogError("No consent request matching request: {returnUrl}", returnUrl);
				}
			}

			return null;
		}

		private static AccountModel ConvertToAccountModel(Account account)
		{
			return new AccountModel()
			{
				Id = account.AccountId,
				Name = account.DisplayName,
				MaskedName = account.MaskedName,
				IsValid = account.OpenStatus == "OPEN"
			};
		}

		private async Task<ConsentViewModel> CreateConsentViewModel(
			ConsentInputModel model, string returnUrl,
			AuthorizationRequest request)
		{
			// Fetch accounts and invalid accounts here.
			// Improvement: These accounts are loaded every time the page loads. Maybe we can implement caching.
			var allAccounts = (await _resourceRepository.GetAllAccountsByCustomerIdForConsent(Guid.Parse(User.GetSubjectId())))
				.Select(acc => ConvertToAccountModel(acc));
			if (allAccounts == null || !allAccounts.Any())
			{
				// throw some error message to the UI
			}

			var validAccounts = allAccounts.Where(acc => acc.IsValid).ToArray();
			var invalidAccounts = allAccounts.Where(acc => !acc.IsValid).ToArray();

			// Set the selected account ids
			if (model != null && model.SelectedAccountIds.Any())
			{
				foreach (var account in validAccounts)
				{
					account.IsSelected = model.SelectedAccountIds.Contains(account.Id);
				}
			}

			//Get sharing duration
			TimeSpan sharingDuration = TimeSpan.FromDays(365);
			if (request.RequestObjectValues.ContainsKey(AuthorizeRequest.Claims))
			{
				var authorizeClaims = JsonConvert.DeserializeObject<AuthorizeClaims>(request.RequestObjectValues[AuthorizeRequest.Claims]);
				if (authorizeClaims.SharingDuration.HasValue)
				{
					sharingDuration = TimeSpan.FromSeconds(authorizeClaims.SharingDuration.Value);
				}
			}

			var vm = new ConsentViewModel
			{
				ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
				Description = model?.Description,
				SelectedAccountIds = model?.SelectedAccountIds,

				ReturnUrl = returnUrl,

				ClientName = request.Client.ClientName ?? request.Client.ClientId,
				ClientUrl = request.Client.ClientUri,
				ClientLogoUrl = request.Client.LogoUri,
				AllowRememberConsent = request.Client.AllowRememberConsent,
				ConsentLifeTimeSpan = sharingDuration,

				Accounts = validAccounts,
				InvalidAccounts = invalidAccounts,
			};

			return vm;
		}
	}
}