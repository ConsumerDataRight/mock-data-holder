using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.IdentityServer.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Middleware
{
    /// <summary>
    /// Applicable for the Authorize Request End Point and if an Error is being returned
    /// IS4 returns an ErrorId which we need to use to get the real error to return to the End User Redirection URI
    /// As per OIDC 3.1.2.6 - we will return an error to the Clients Redirection URI unless the Redirect Uri is invalid
    /// As per OAuth 4.1.2.1 - in the event the Redirection URI is invalid, we will return a 400 Bad Request
    /// https://tools.ietf.org/html/rfc6749#section-4.1.2.1, https://openid.net/specs/openid-connect-core-1_0.html#AuthError.
    /// </summary>
    public class AuthoriseErrorHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<AuthoriseErrorHandlingMiddleware> _logger;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IPersistedGrantStore _persistedGrantStore;
        private readonly IConfiguration _configuration;

        public AuthoriseErrorHandlingMiddleware(
            ILogger<AuthoriseErrorHandlingMiddleware> logger,
            IConfiguration configuration,
            IIdentityServerInteractionService interaction,
            IPersistedGrantStore persistedGrantStore)
        {
            _logger = logger;
            _configuration = configuration;
            _interaction = interaction;
            _persistedGrantStore = persistedGrantStore;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await next.Invoke(context);

            if (context.Request.Path != "/connect/authorize" || !context.Response.Headers.ContainsKey("location"))
            {
                return;
            }

            try
            {
                var location = context.Response.Headers["location"];
                var locationUri = new Uri(location);
                var locationQueryValues = HttpUtility.ParseQueryString(locationUri.Query);
                string errorId = locationQueryValues["errorid"];

                if (string.IsNullOrWhiteSpace(errorId))
                {
                    return;
                }

                context.Response.Headers.Remove("location");
                var errorDetail = await _interaction.GetErrorContextAsync(locationQueryValues.Get("errorid"));
                var requestJwt = await GetRequestJwt(context, errorDetail);

                if (string.IsNullOrWhiteSpace(requestJwt))
                {
                    // Invalid request as there was no request object/request uri.
                    if (errorDetail.Error.Equals(AuthorizeErrorCodes.InvalidRequest, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("Authorization Error - error: {error}, error_description: {error_description}", errorDetail.Error, errorDetail.ErrorDescription);
                        SetErrorResponse(context);
                        return;
                    }

                    _logger.LogError("Error processing the NULL Request JWT");
                    await SetNullJWTRedirectResponse(context, AuthorizeErrorDescriptions.InvalidRequestJwt, errorDetail.ClientId);
                    return;
                }

                string redirectUri = string.Empty;
                string state = string.Empty;

                try
                {
                    var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(requestJwt);
                    redirectUri = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == AuthorizeRequest.RedirectUri)?.Value;
                    state = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == AuthorizeRequest.State)?.Value;
                }
                catch (Exception ex)
                {
                    // Cant log actual Exception as Azure AppService Logging crashes in some instances
                    _logger.LogError("Error processing the Request JWT {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                    SetInvalidJWTErrorResponse(context, locationUri, errorDetail.Error, AuthorizeErrorDescriptions.InvalidRequestJwt);
                    return;
                }

                if (errorDetail.ErrorDescription == AuthorizeErrorDescriptions.ClientIdNotFound)
                {
                    await SetBadRequestErrorJsonResponse(context, AuthorizeErrorDescriptions.ClientIdNotFound);
                    return;
                }

                if (string.IsNullOrWhiteSpace(redirectUri) && errorDetail.ErrorDescription == AuthorizeErrorDescriptions.InvalidRedirectUri)
                {
                    // If the redirect URI is invalid, do not redirect to the URI. Just show the error page.
                    context.Response.Headers.Add("location", location);
                    return;
                }

                if (!string.IsNullOrEmpty(redirectUri) && errorDetail.ErrorDescription == AuthorizeErrorDescriptions.InvalidRedirectUri)
                {
                    //Return a BadRequest not Redirect with provided RedirectUri
                    await SetBadRequestErrorJsonResponse(context, AuthorizeErrorDescriptions.InvalidRedirectUri);
                    return;
                }

                // Let the customer error factory handle some of the errors and generate custom error codes
                var responseError = ConvertToError(errorDetail);

                // We are supposed to return the state value to the Client exactly how they gave it to us.
                // By using EscapeDataString we may be modifying it in a way they do not expect/plan for.
                // This may need to be refactored in future if it causes an issue.
                var stateQueryValue = BuildQueryValue(AuthorizeResponse.State, state);
                var errorDescriptonQueryValue = BuildQueryValue(AuthorizeResponse.ErrorDescription, string.IsNullOrEmpty(responseError.Detail) ? responseError.Title : responseError.Detail);

                // Authorisation errors are returned using # query fragments
                // Additional info: Check fapi1-advanced-final-ensure-response-mode-query scenario; OIDC 3.1.2.6 defines parameters to return, including acceptable error codes
                var errorLocation = $"{redirectUri}#{AuthorizeResponse.Error}={Uri.EscapeDataString(responseError.Code)}{errorDescriptonQueryValue}{stateQueryValue}";

                context.Response.Headers.Add("location", errorLocation);
                _logger.LogDebug("Created and returned 302 error response for an Authorize Request: {ErrorLocation}", errorLocation);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "revoke error: ");
            }
        }

        private static string BuildQueryValue(string parameterName, string parameterValue)
        {
            return string.IsNullOrWhiteSpace(parameterValue)
                    ? string.Empty
                    : $"&{parameterName}={Uri.EscapeDataString(parameterValue)}";
        }

        private async Task<string> GetRequestJwt(HttpContext context, ErrorMessage error)
        {
            var requestUri = string.Empty;
            var request = string.Empty;

            if (context.Request.Method == HttpMethods.Get)
            {
                var requestQueryValues = HttpUtility.ParseQueryString(context.Request.QueryString.Value);

                // If request JWT is missing or invalid, we cant get the redirecturi so return 400
                requestUri = requestQueryValues.Get(AuthorizeRequest.RequestUri);
                request = requestQueryValues.Get(AuthorizeRequest.Request);
            }
            else
            {
                requestUri = context.Request.Form.FirstOrDefault(x => x.Key == AuthorizeRequest.RequestUri).Value;
                request = context.Request.Form.FirstOrDefault(x => x.Key == AuthorizeRequest.Request).Value;
            }

            if (string.IsNullOrEmpty(requestUri))
            {
                return request;
            }

            // Retrieve the request object using the request_uri parameter.
            var grant = await _persistedGrantStore.GetAsync(requestUri.Replace(AuthorizeRequest.RequestUriPrefix, ""));
            if (grant == null || !grant.ClientId.Equals(error.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                return request;
            }

            return grant.Data;
        }

        private static Error ConvertToError(ErrorMessage errorDetail)
        {
            // First try to match error codes
            switch (errorDetail.Error)
            {
                case AuthorizeErrorCodes.UnsupportedResponseType:
                    return Error.InvalidRequest("Unsupported response_type value");

                case AuthorizeErrorCodes.InvalidRequestObject:
                    /// Authorisation invalid request object should be returned without error code changes
                    /// (e.g. fapi1-advanced-final-ensure-request-object-without-nonce-fails (OIDCC-3.1.2.6  OIDCC-3.3.2.6);
                    /// fapi1-advanced-final-ensure-request-object-without-exp-fails (OIDCC-3.1.2.6  RFC6749-4.2.2.1)
                    return Error.InvalidRequestObject(errorDetail.ErrorDescription);

                default:
                    break;
            }

            // Then, try to match using the error message
            switch (errorDetail.ErrorDescription)
            {
                case AuthorizeErrorCodeDescription.UnknownClient:
                    return Error.UnknownClient("Invalid client ID.");
                case AuthorizeErrorCodeDescription.InvalidClientId:
                    return Error.InvalidField("The client ID is invalid");
                case AuthorizeErrorCodeDescription.InvalidScope:
                    return Error.InvalidField("The scope is not supported");
                case AuthorizeErrorCodeDescription.InvalidRedirectUri:
                    return Error.InvalidField("The redirect uri is invalid");
                case AuthorizeErrorCodeDescription.MissingOpenIdScope:
                    return Error.MissingOpenIdScope("OpenID Connect requests MUST contain the openid scope value.");
                case AuthorizeErrorCodeDescription.InvalidJWTRequest:
                    return Error.InvalidRequest("Signature is not valid.");
                case AuthorizeErrorCodeDescription.InvalidGrantType:
                    // e.g. fapi1-advanced-final-ensure-response-type-code-fails
                    return Error.InvalidRequest("The token type is not supported");

                default:
                    break;
            }

            // Finally, return the same error message
            return new Error(errorDetail.Error, errorDetail.Error, errorDetail.ErrorDescription);
        }

        private void SetErrorResponse(HttpContext context)
        {
            string redirLocation = BuildUrl(context, "/home/error");

            redirLocation += @"#error=invalid_request";
            context.Response.StatusCode = 302;
            context.Response.Headers.Add("location", redirLocation);            
        }

        private string BuildUrl(HttpContext context, string path)
        {
            var url = new StringBuilder();
            url.Append("https://");
            url.Append(context.Request.Host);

            var basePath = _configuration.GetBasePath();
            if (!string.IsNullOrEmpty(basePath))
            {
                url.Append(basePath);
            }

            url.Append(path);

            return url.ToString();
        }

        private async Task<bool> SetNullJWTRedirectResponse(HttpContext context, string error, string clientId)
        {
            string redirLocation;
            var keys = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { ClientId = clientId });
            var par = keys.FirstOrDefault(g => g.Type == CdsConstants.GrantTypes.PushAuthoriseRequest && g.ClientId == clientId);
            if (par == null || string.IsNullOrEmpty(par.Key))
            {
                redirLocation = context.Request.IsHttps ? "https://" : "http://";

                // When running in the sandbox, the Azure Application Gateway injects this header for all requests
                if (context.Request.Headers.TryGetValue("X-Forwarded-Host", out StringValues forwardedHosts))
                    redirLocation += forwardedHosts.First() + context.Request.Path.ToString();

                // When not in the sandbox revert to where the request originates from
                // (as the X-Forwarded-Host header is added only in the mTLS dependency injection)
                else
                    redirLocation += context.Request.Host.ToString() + context.Request.Path.ToString();
            }
            else
            {
                var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(par.Data);
                redirLocation = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == AuthorizeRequest.RedirectUri)?.Value;
            }
            redirLocation += @"#error=invalid_request_uri&error_description=The request uri has expired";
            context.Response.StatusCode = 302;
            context.Response.Headers.Add("location", redirLocation);
            _logger.LogDebug("Created and returned 400 error response for an Authorize Request: {error}", error);
            return true;
        }

        private void SetInvalidJWTErrorResponse(HttpContext context, Uri locUri, string idSvrErr, string error)
        {
            context.Response.StatusCode = 400;
            string redirLocation = locUri.GetLeftPart(UriPartial.Path);

            if (string.Equals(idSvrErr, AuthorizeErrorCodes.InvalidRequestObject))
                redirLocation += @"#error=invalid_request";
            else
                redirLocation += @"#error=invalid_request_object&error_description=Request JWT is not valid&state=";

            context.Response.Headers.Add("location", redirLocation);
            _logger.LogDebug("Created and returned 400 error response for an Authorize Request: {error}", error);
        }

        private async Task SetBadRequestErrorJsonResponse(HttpContext context, string error)
        {
            context.Response.StatusCode = 400;
            var errorMessage = @"{""error"": ""invalid_request""}";
            var errorData = UTF8Encoding.UTF8.GetBytes(errorMessage);
            context.Response.ContentType = "application/json";
            await context.Response.BodyWriter.WriteAsync(errorData);
            _logger.LogDebug("Created and returned 400 error response for an Authorize Request: {error}", error);
        }
    }
}