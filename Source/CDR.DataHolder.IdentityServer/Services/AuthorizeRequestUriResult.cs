using System;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Extensions;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    // this class mainly copied from IdentityServer4 source code - AuthorizeResult
    // specific modified the returned header location if error occurs and added ValidationCheck return
    public class AuthorizeRequestUriResult : IEndpointResult
    {
        private IdentityServerOptions _options;
        private IUserSession _userSession;
        private IMessageStore<ErrorMessage> _errorMessageStore;
        private ISystemClock _clock;

        public IdentityServer4.ResponseHandling.AuthorizeResponse Response { get; }

        public ValidationCheck? ValidationCheck { get; }

        public AuthorizeRequestUriResult(IdentityServer4.ResponseHandling.AuthorizeResponse response, ValidationCheck? validationCheck)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
            ValidationCheck = validationCheck;
        }

        public AuthorizeRequestUriResult(
            IdentityServer4.ResponseHandling.AuthorizeResponse response,
            IdentityServerOptions options,
            IUserSession userSession,
            IMessageStore<ErrorMessage> errorMessageStore,
            ISystemClock clock)
            : this(response, null)
        {
            _options = options;
            _userSession = userSession;
            _errorMessageStore = errorMessageStore;
            _clock = clock;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            if (Response.IsError)
            {
                await ProcessErrorAsync(context);
            }
            else
            {
                await ProcessResponseAsync(context);
            }
        }


        private void Init(HttpContext context)
        {
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
            _userSession = _userSession ?? context.RequestServices.GetRequiredService<IUserSession>();
            _errorMessageStore = _errorMessageStore ?? context.RequestServices.GetRequiredService<IMessageStore<ErrorMessage>>();
            _clock = _clock ?? context.RequestServices.GetRequiredService<ISystemClock>();
        }

        private async Task ProcessErrorAsync(HttpContext context)
        {
            // these are the conditions where we can send a response 
            // back directly to the client, otherwise we're only showing the error UI
            var isPromptNoneError = Response.Error == OidcConstants.AuthorizeErrors.AccountSelectionRequired ||
                Response.Error == OidcConstants.AuthorizeErrors.LoginRequired ||
                Response.Error == OidcConstants.AuthorizeErrors.ConsentRequired ||
                Response.Error == OidcConstants.AuthorizeErrors.InteractionRequired;

            if (Response.Error == OidcConstants.AuthorizeErrors.AccessDenied ||
                (isPromptNoneError && Response.Request.PromptModes.Contains(OidcConstants.PromptModes.None))
            )
            {
                // this scenario we can return back to the client
                await ProcessResponseAsync(context);
            }
            else
            {
                // we now know we must show error page
                await RedirectToErrorPageAsync(context);
            }
        }

        protected async Task ProcessResponseAsync(HttpContext context)
        {
            if (!Response.IsError)
            {
                // success response -- track client authorization for sign-out
                //_logger.LogDebug("Adding client {0} to client list cookie for subject {1}", request.ClientId, request.Subject.GetSubjectId());
                await _userSession.AddClientIdAsync(Response.Request.ClientId);
            }

            await RenderAuthorizeResponseAsync(context);
        }

        private string BuildRedirectUri()
        {
            var uri = Response.RedirectUri;
            var query = Response.ToNameValueCollection().ToQueryString();

            if (Response.Request.ResponseMode == OidcConstants.ResponseModes.Query)
            {
                uri = uri.AddQueryString(query);
            }
            else
            {
                uri = uri.AddHashFragment(query);
            }

            if (Response.IsError && !uri.Contains("#"))
            {
                // https://tools.ietf.org/html/draft-bradley-oauth-open-redirector-00
                uri += "#_=_";
            }

            return uri;
        }

        private async Task RenderAuthorizeResponseAsync(HttpContext context)
        {
            if (Response.Request.ResponseMode == OidcConstants.ResponseModes.Query ||
                Response.Request.ResponseMode == OidcConstants.ResponseModes.Fragment)
            {
                context.Response.SetNoCache();
                context.Response.Redirect(BuildRedirectUri());
            }
            else
            {
                //_logger.LogError("Unsupported response mode.");
                throw new InvalidOperationException("Unsupported response mode");
            }
        }

        private async Task RedirectToErrorPageAsync(HttpContext context)
        {
            var errorModel = new ErrorMessage
            {
                RequestId = context.TraceIdentifier,
                Error = Response.Error,
                ErrorDescription = Response.ErrorDescription,
                UiLocales = Response.Request?.UiLocales,
                DisplayMode = Response.Request?.DisplayMode,
                ClientId = Response.Request?.ClientId
            };

            if (Response.RedirectUri != null && Response.Request?.ResponseMode != null)
            {
                // if we have a valid redirect uri, then include it to the error page
                errorModel.RedirectUri = BuildRedirectUri();
                errorModel.ResponseMode = Response.Request.ResponseMode;
            }

            //// IdentityServer4 default return url is commented out
            //var message = new Message<ErrorMessage>(errorModel, _clock.UtcNow.UtcDateTime);
            //var id = await _errorMessageStore.WriteAsync(message);

            //var errorUrl = _options.UserInteraction.ErrorUrl;
            //var url = errorUrl.AddQueryString(_options.UserInteraction.ErrorIdParameter, id);

            if (string.IsNullOrWhiteSpace(Response.RedirectUri))
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain";
            }
            else
            {
                var url = Response.RedirectUri;

                if (!string.IsNullOrWhiteSpace(errorModel.Error))
                {
                    url = url.AddQueryString(OidcConstants.AuthorizeResponse.Error, errorModel.Error);
                }

                if (!string.IsNullOrWhiteSpace(errorModel.ErrorDescription))
                {
                    url = url.AddQueryString(OidcConstants.AuthorizeResponse.ErrorDescription, errorModel.ErrorDescription);
                }

                if (!string.IsNullOrWhiteSpace(Response.State))
                {
                    url = url.AddQueryString(OidcConstants.AuthorizeRequest.State, Response.State);
                }

                context.Response.RedirectToAbsoluteUrl(url);
            }
        }
    }
}
