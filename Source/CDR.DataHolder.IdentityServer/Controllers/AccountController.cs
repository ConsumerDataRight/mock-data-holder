// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using CDR.DataHolder.Domain.Entities;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.IdentityServer.Models.UI;
using CDR.DataHolder.IdentityServer.Stores;
using CDR.DataHolder.IdentityServer.Validation;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Controllers
{
	/// <summary>
	/// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
	/// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
	/// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
	/// </summary>
	[SecurityHeaders]
	[AllowAnonymous]
	public class AccountController : Controller
	{
		private readonly IIdentityServerInteractionService _interaction;
		private readonly IClientStore _clientStore;
		private readonly IAuthenticationSchemeProvider _schemeProvider;
		private readonly IEventService _events;
		private readonly IResourceRepository _resourceRepository;

		public AccountController(
			IIdentityServerInteractionService interaction,
			IClientStore clientStore,
			IAuthenticationSchemeProvider schemeProvider,
			IEventService events,
			IResourceRepository resourceRepository)
		{
			_interaction = interaction;
			_clientStore = clientStore;
			_schemeProvider = schemeProvider;
			_events = events;
			_resourceRepository = resourceRepository;
		}

		/// <summary>
		/// Entry point into the login workflow
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Login(string returnUrl)
		{
			// build a model so we know what to show on the login page
			var vm = await BuildLoginViewModelAsync(returnUrl);
			if (!vm.EnableLogin)
			{
				//TODO:C do something here or leave for the post.
			}

			return View(vm);
		}

		/// <summary>
		/// Handle postback from username/password login
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(UserAuthViewModel model, string button)
		{
			// check if we are in the context of an authorization request
			var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

			switch (button)
			{
				case UserAuthViewModel.ButtonActions.Cancel:
					if (context != null)
					{
						// if the user cancels, send a result back into IdentityServer as if they 
						// denied the consent (even if this client does not require consent).
						// this will send back an access denied OIDC error response to the client.
						await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

						return Redirect(model.ReturnUrl);
					}
					else
					{
						// since we don't have a valid context, then we just go back to the home page
						return Redirect("~/");
					}

				case UserAuthViewModel.ButtonActions.Page1:
					model.CustomerId = string.Empty;
					break;
				case UserAuthViewModel.ButtonActions.Page2:
					// Validate the customer id
					ModelState.Clear();
					if (await ValidateCustomer(model.CustomerId, context?.Client.ClientId) == null)
					{
						model.ClearInputs();
						break;
					}

					// Generate the OTP here. This is to mock the OTP behavior.
					// In production, the OTP will be generated and sent to the user auth device.
					model.ValidOtp = "000789";
					model.Otp = string.Empty;
					model.ShowOtp = true;
					break;
				case UserAuthViewModel.ButtonActions.Authenticate:
					if (ModelState.IsValid)
					{
						// Validate the customer id and OTP
						var customer = await ValidateCustomer(model.CustomerId, context?.Client.ClientId);
						if (customer == null)
						{
							model.ClearInputs();
							break;
						}
						if (model.Otp != model.ValidOtp) 
						{
							await _events.RaiseAsync(new UserLoginFailureEvent(model.CustomerId, "Incorrect one time password", clientId: context?.Client.ClientId));
							ModelState.AddModelError(string.Empty, "Incorrect one time password");
							model.ClearInputs();
							break;
						}

						// Login the user.
						await _events.RaiseAsync(new UserLoginSuccessEvent(customer.LoginId, customer.CustomerId.ToString(), customer.LoginId, clientId: context?.Client.ClientId));
						var isuser = new IdentityServerUser(customer.CustomerId.ToString())
						{
							DisplayName = customer.LoginId
						};
						await HttpContext.SignInAsync(isuser, null);

						if (context != null)
						{
							// we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
							return Redirect(model.ReturnUrl);
						}

						// request for a local page
						if (Url.IsLocalUrl(model.ReturnUrl))
						{
							return Redirect(model.ReturnUrl);
						}
						else if (string.IsNullOrEmpty(model.ReturnUrl))
						{
							return Redirect("~/");
						}
						else
						{
							// user might have clicked on a malicious link - should be logged
							throw new Exception("invalid return URL");
						}
					}

					break;
				default:
					break;
			}

			// something went wrong, show form with error
			var vm = await BuildLoginViewModelAsync(model);
			return View(vm);
		}

		private async Task<Customer> ValidateCustomer(string customerId, string clientId)
		{
			var customer = await _resourceRepository.GetCustomerByLoginId(customerId);
			if (customer == null)
			{
				await _events.RaiseAsync(new UserLoginFailureEvent(customerId, "Incorrect customer ID", clientId: clientId));
				ModelState.AddModelError(string.Empty, "Incorrect customer ID");
			}

			return customer;
		}

		/// <summary>
		/// Show logout page
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Logout(string logoutId)
		{
			// build a model so the logout page knows what to display
			LogoutViewModel vm = await BuildLogoutViewModelAsync(logoutId);

			// if the request for logout was properly authenticated from IdentityServer, then
			// we don't need to show the prompt and can just log the user out directly.
			return await Logout(vm);
		}

		/// <summary>
		/// Handle logout page postback
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout(LogoutViewModel model)
		{
			// build a model so the logged out page knows what to display
			var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

			if (User?.Identity.IsAuthenticated == true)
			{
				// delete local authentication cookie
				await HttpContext.SignOutAsync();

				// raise the logout event
				await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
			}

			return View("LoggedOut", vm);
		}

		[HttpGet]
		public IActionResult AccessDenied()
		{
			return View();
		}

		/*****************************************/
		/* helper APIs for the AccountController */
		/*****************************************/
		private async Task<UserAuthViewModel> BuildLoginViewModelAsync(string returnUrl)
		{
			var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

			var allowLocal = true;
			var clientName = "Unknown";
			if (context?.Client.ClientId != null)
			{
				var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
				if (client != null)
				{
					allowLocal = client.EnableLocalLogin;
					clientName = client.ClientName;
				}
			}

			return new UserAuthViewModel
			{
				EnableLogin = allowLocal,
				ReturnUrl = returnUrl,
				ClientName = clientName
			};
		}

		private async Task<UserAuthViewModel> BuildLoginViewModelAsync(UserAuthViewModel model)
		{
			var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
			vm.CustomerId = model.CustomerId;
			vm.Otp = model.Otp;
			vm.ValidOtp = model.ValidOtp;
			vm.ShowOtp = model.ShowOtp;

			return vm;
		}

		private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
		{
			var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = false };

			if (User?.Identity.IsAuthenticated != true)
			{
				// if the user is not authenticated, then just show logged out page
				vm.ShowLogoutPrompt = false;
				return vm;
			}

			var context = await _interaction.GetLogoutContextAsync(logoutId);
			if (context?.ShowSignoutPrompt == false)
			{
				// it's safe to automatically sign-out
				vm.ShowLogoutPrompt = false;
				return vm;
			}

			// show the logout prompt. this prevents attacks where the user
			// is automatically signed out by another malicious web page.
			return vm;
		}

		private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
		{
			// get context information (client name, post logout redirect URI and iframe for federated signout)
			var logout = await _interaction.GetLogoutContextAsync(logoutId);

			var vm = new LoggedOutViewModel
			{
				AutomaticRedirectAfterSignOut = false,
				PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
				ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
				SignOutIframeUrl = logout?.SignOutIFrameUrl,
				LogoutId = logoutId
			};

			if (User?.Identity.IsAuthenticated == true)
			{
				var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
				if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
				{
					var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
					if (providerSupportsSignout)
					{
						if (vm.LogoutId == null)
						{
							// if there's no current logout context, we need to create one
							// this captures necessary info from the current logged in user
							// before we signout and redirect away to the external IdP for signout
							vm.LogoutId = await _interaction.CreateLogoutContextAsync();
						}

						vm.ExternalAuthenticationScheme = idp;
					}
				}
			}

			return vm;
		}
	}
}
