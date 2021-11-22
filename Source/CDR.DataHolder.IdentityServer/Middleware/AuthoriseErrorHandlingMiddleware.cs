using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CDR.DataHolder.API.Infrastructure.Models;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

        public AuthoriseErrorHandlingMiddleware(
            ILogger<AuthoriseErrorHandlingMiddleware> logger, 
            IIdentityServerInteractionService interaction, 
            IPersistedGrantStore persistedGrantStore)
        {
            _logger = logger;
            _interaction = interaction;
            _persistedGrantStore = persistedGrantStore;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await next.Invoke(context);

            if (context.Request.Path == "/connect/authorize" && context.Response.Headers.ContainsKey("location"))
            {
                try
                {
                    var location = context.Response.Headers["location"];

                    var locationUri = new Uri(location);
                    var locationQueryValues = HttpUtility.ParseQueryString(locationUri.Query);

                    string errorId = locationQueryValues["errorid"];

                    if (!string.IsNullOrWhiteSpace(errorId))
                    {
                        context.Response.Headers.Remove("location");
                        var requestJwt = string.Empty;
                        var errorDetail = await _interaction.GetErrorContextAsync(locationQueryValues.Get("errorid"));

                        if (context.Request.Method == HttpMethods.Get)
                        {
                            var requestQueryValues = HttpUtility.ParseQueryString(context.Request.QueryString.Value);

                            // If request JWT is missing or invalid, we cant get the redirecturi so return 400
                            requestJwt = requestQueryValues.Get(AuthorizeRequest.Request);
                        }
                        else
                        {
                            requestJwt = context.Request.Form.FirstOrDefault(x => x.Key == AuthorizeRequest.Request).Value;
                        }

                        if (string.IsNullOrWhiteSpace(requestJwt))
                        {
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
                        }
                        else if (string.IsNullOrWhiteSpace(redirectUri) && errorDetail.ErrorDescription == AuthorizeErrorDescriptions.InvalidRedirectUri)
                        {
                            // If the redirect URI is invalid, do not redirect to the URI. Just show the error page.
                            context.Response.Headers.Add("location", location);
                            return;
                        }
                        else if (!string.IsNullOrEmpty(redirectUri) && errorDetail.ErrorDescription == AuthorizeErrorDescriptions.InvalidRedirectUri)
                        {
                            //Return a BadRequest not Redirect with provided RedirectUri
                            await SetBadRequestErrorJsonResponse(context, AuthorizeErrorDescriptions.InvalidRedirectUri);
                            return;
                        }
                        else
                        {
                            // Let the customer error factory handle some of the errors and generate custom error codes
                            var responseError = ConvertToError(errorDetail);

                            // We are supposed to return the state value to the Client exactly how they gave it to us.
                            // By using EscapeDataString we may be modifying it in a way they do not expect/plan for.
                            // This may need to be refactored in future if it causes an issue.
                            var stateQueryValue = string.IsNullOrWhiteSpace(state)
                                ? string.Empty
                                : $"&{AuthorizeRequest.State}={Uri.EscapeDataString(state)}";
                            var errorDetailsQueryValue = string.IsNullOrWhiteSpace(responseError.Detail)
                                ? string.Empty
                                : $"&{AuthorizeResponse.ErrorDetail}={Uri.EscapeDataString(responseError.Detail)}";

                            var errorLocation = string.Empty;
                            var errorDescriptonQueryValue = string.Empty;

                            if (!string.IsNullOrEmpty(errorDetail.Error) &&
                                 (string.Equals(errorDetail.Error, AuthorizeErrorCodes.UnsupportedResponseType, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(errorDetail.Error, AuthorizeErrorCodes.InvalidScope, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(errorDetail.ErrorDescription, AuthorizeErrorCodeDescription.MissingOpenIdScope, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(errorDetail.ErrorDescription, AuthorizeErrorCodeDescription.UnknownClient, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(errorDetail.ErrorDescription, AuthorizeErrorCodeDescription.InvalidJWTRequest, StringComparison.OrdinalIgnoreCase)
                                   )
                                )
                            {
                                //Updates for AC
                                errorDetailsQueryValue = string.IsNullOrWhiteSpace(responseError.Detail)
                                    ? string.Empty
                                    : $"&{AuthorizeError.ErrorDescription}={Uri.EscapeDataString(responseError.Detail)}";
                                // Authorisation errors are returned using # query fragments
                                // Additional info: Check fapi1-advanced-final-ensure-response-mode-query scenario
                                errorLocation = $"{redirectUri}#{AuthorizeError.Error}={Uri.EscapeDataString(responseError.Code)}{errorDescriptonQueryValue}{errorDetailsQueryValue}{stateQueryValue}";
                            }
                            else
                            {
                                errorDescriptonQueryValue = string.IsNullOrWhiteSpace(responseError.Title)
                                ? string.Empty
                                : $"&{AuthorizeResponse.ErrorDescription}={Uri.EscapeDataString(responseError.Title)}";

                                // Authorisation errors are returned using # query fragments
                                // Additional info: Check fapi1-advanced-final-ensure-response-mode-query scenario; OIDC 3.1.2.6 defines parameters to return, including acceptable error codes
                                errorLocation = $"{redirectUri}#{AuthorizeResponse.Error}={Uri.EscapeDataString(responseError.Code)}{errorDescriptonQueryValue}{errorDetailsQueryValue}{stateQueryValue}";
                            }

                            context.Response.Headers.Add("location", errorLocation);
                            _logger.LogDebug("Created and returned 302 error response for an Authorize Request: {ErrorLocation}", errorLocation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "revoke error: ");
                }
            }
        }

		private Error ConvertToError(ErrorMessage errorDetail)
		{
            // First try to match error codes
			switch (errorDetail.Error)
			{
                case AuthorizeErrorCodes.InvalidScope:
                    return Error.InvalidScope("The request scope is valid, unknown, or malformed.");
                case AuthorizeErrorCodes.UnsupportedResponseType:                    
                    return Error.InvalidRequest("Unsupported response_type value");
                case AuthorizeErrorCodes.InvalidRequestObject:                    
                    // Authorisation invalid request object should be returned without error code changes
                    //(e.g. fapi1-advanced-final-ensure-request-object-without-nonce-fails (OIDCC-3.1.2.6  OIDCC-3.3.2.6);
                    // fapi1-advanced-final-ensure-request-object-without-exp-fails (OIDCC-3.1.2.6  RFC6749-4.2.2.1)
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

		private async Task<bool> SetNullJWTRedirectResponse(HttpContext context, string error, string clientId)
        {
            string redirLocation;
            var keys = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { ClientId = clientId });
            var par = keys.FirstOrDefault(g => g.Type == CdsConstants.GrantTypes.PushAuthoriseRequest && g.ClientId == clientId);
            if (par == null || string.IsNullOrEmpty(par.Key))
            {
                redirLocation = context.Request.IsHttps ? "https://" : "http://";
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

        private bool SetInvalidJWTErrorResponse(HttpContext context, Uri locUri, string idSvrErr, string error)
        {
            context.Response.StatusCode = 400;
            string redirLocation = locUri.GetLeftPart(UriPartial.Path);

            if (string.Equals(idSvrErr, AuthorizeErrorCodes.InvalidRequestObject))
                redirLocation += @"#error=invalid_request";
            else
                redirLocation += @"#error=invalid_request_object&error_description=Request JWT is not valid&state=";

            context.Response.Headers.Add("location", redirLocation);
            _logger.LogDebug("Created and returned 400 error response for an Authorize Request: {error}", error);
            return true;
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