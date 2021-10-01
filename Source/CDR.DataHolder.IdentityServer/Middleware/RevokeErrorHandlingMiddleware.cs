using CDR.DataHolder.IdentityServer.Services;
using CDR.DataHolder.IdentityServer.Validation;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Middleware
{
    /// <summary>
    /// Applicable for the Revoke Request End Point and if an Error is being returned
    /// IS4 returns an ErrorId which we need to use to get the real error to return to the End User Redirection URI
    /// As per https://docs.identityserver.io/en/release/endpoints/revocation.html
    /// As per https://openid.net/specs/openid-connect-core-1_0.html#TokenLifetime
    /// and as per https://datatracker.ietf.org/doc/html/rfc6749#section-5.2
    /// </summary>
    public class RevokeErrorHandlingMiddleware : IMiddleware
    {
        private readonly IConfiguration _configurationSettings;
        private readonly ILogger<RevokeErrorHandlingMiddleware> _logger;
        private readonly IPersistedGrantStore _persistedGrantStore;
        private readonly IClientService _clientService;
        private readonly IIntrospectionRequestValidator _validator;
        private readonly IRefreshTokenStore _refreshTokenStore;

        public RevokeErrorHandlingMiddleware(IConfiguration configSettings,
                                            ILogger<RevokeErrorHandlingMiddleware> logger,
                                            IPersistedGrantStore persistedGrantStore,
                                            IClientService clientService,
                                            IIntrospectionRequestValidator validator,
                                            IRefreshTokenStore refreshTokenStore)
        {
            _configurationSettings = configSettings;
            _logger = logger;
            _persistedGrantStore = persistedGrantStore;
            _clientService = clientService;
            _validator = validator;
            _refreshTokenStore = refreshTokenStore;
        }

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate _next)
        {
            if (httpContext.Request.Path == "/connect/revocation")
            {
                bool movedNext = false;
                try
                {
                    if (httpContext.Request.Method == HttpMethods.Post)
                    {
                        string tokenTypeHint = httpContext.Request.Form.FirstOrDefault(x => x.Key == AuthorizeRequest.TokenTypeHint).Value;
                        if (string.Equals(tokenTypeHint, "access_token"))
                        {
                            if (httpContext.Request.HasFormContentType)
                            {
                                var form = await httpContext.Request.ReadFormAsync();
                                if (form != null)
                                {
                                    string errMsg = string.Empty;
                                    string authToken = form[TokenRequest.Token].FirstOrDefault();
                                    var tokenIsValid = ValidateAccessTokenToken(authToken);
                                    if (!tokenIsValid)
                                    {
                                        // InValid Access Token
                                        movedNext = await CustomResponseAsync(httpContext, _next, null, StatusCodes.Status200OK);
                                    }
                                    else
                                    {
                                        string clientId = form[RevocationRequest.ClientId].FirstOrDefault();
                                        var client = await _clientService.FindClientById(clientId);
                                        if (client == null)
                                        {
                                            // InValid ClientId
                                            errMsg = @"{""error"":""invalid_client""}";
                                            movedNext = await CustomResponseAsync(httpContext, _next, errMsg, StatusCodes.Status401Unauthorized);
                                        }
                                        else
                                        {
                                            string grantType = form[StandardClaims.GrantType].FirstOrDefault();
                                            string clientAssertion = form[RevocationRequest.ClientAssertion].FirstOrDefault();
                                            string clientAssertionType = form[RevocationRequest.ClientAssertionType].FirstOrDefault();
                                            bool isValidClient = false;
                                            isValidClient = Guid.TryParse(client.ClientId, out _);
                                            if (!isValidClient)
                                            {
                                                // InValid Client
                                                errMsg = @"{""error"":""invalid_client""}";
                                                movedNext = await CustomResponseAsync(httpContext, _next, errMsg, StatusCodes.Status401Unauthorized);
                                            }
                                            else if (!string.IsNullOrEmpty(grantType) && !grantType.Equals(IntrospectionRequestElements.AllowedGrantType))
                                            {
                                                // InValid Grant Type
                                                errMsg = @"{""error"":""unsupported_grant_type""}";
                                                movedNext = await CustomResponseAsync(httpContext, _next, errMsg, StatusCodes.Status401Unauthorized);
                                            }
                                            else if (!string.IsNullOrEmpty(clientAssertionType) && !clientAssertionType.Equals(IntrospectionRequestElements.AllowedClientAssertionType))
                                            {
                                                // InValid Client Assertion Type
                                                errMsg = @"{""error"":""invalid_client""}";
                                                movedNext = await CustomResponseAsync(httpContext, _next, errMsg, StatusCodes.Status401Unauthorized);
                                            }
                                            else if (!string.IsNullOrEmpty(clientAssertion))
                                            {
                                                var validationResults = await _validator.ValidateClientAssertionAsync(httpContext, client);
                                                if (validationResults.Any())
                                                {
                                                    // InValid Client Assertion
                                                    errMsg = @"{""error"":""invalid_client""}";
                                                    movedNext = await CustomResponseAsync(httpContext, _next, errMsg, StatusCodes.Status401Unauthorized);
                                                }
                                                else
                                                {
                                                    var keys = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { ClientId = clientId });
                                                    var refTkn = keys.FirstOrDefault(g => g.Type == TokenTypes.RefreshToken && g.ClientId == clientId);
                                                    if (refTkn != null && !string.IsNullOrEmpty(refTkn.Key))
                                                    {
                                                        // DELETE Refresh Token from PersistedGrants db table AFTER Access Token has been Revoked
                                                        await _persistedGrantStore.RemoveAsync(refTkn.Key);
                                                        movedNext = await CustomResponseAsync(httpContext, _next, null, StatusCodes.Status200OK);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (string.Equals(tokenTypeHint, "refresh_token"))
                        {
                            if (httpContext.Request.HasFormContentType)
                            {
                                var form = await httpContext.Request.ReadFormAsync();
                                if (form != null)
                                {
                                    string errMsg = string.Empty;
                                    string clientAssertion = form[RevocationRequest.ClientAssertion].FirstOrDefault();
                                    if (!string.IsNullOrEmpty(clientAssertion))
                                    {
                                        string clientId = form[RevocationRequest.ClientId].FirstOrDefault();
                                        var client = await _clientService.FindClientById(clientId);
                                        if (client != null)
                                        {
                                            var validationResults = await _validator.ValidateClientAssertionAsync(httpContext, client);
                                            if (validationResults.Any())
                                            {
                                                // InValid Client Assertion
                                                movedNext = await CustomResponseAsync(httpContext, _next, null, StatusCodes.Status401Unauthorized);
                                            }
                                            else
                                            {
                                                string refToken = form[TokenRequest.Token].FirstOrDefault();
                                                var rt = await _refreshTokenStore.GetRefreshTokenAsync(refToken);
                                                if (rt != null)
                                                {
                                                    // DELETE Refresh Token Store (IF EXISTS) AFTER Refresh Token has been Revoked
                                                    await _refreshTokenStore.RemoveRefreshTokenAsync(refToken);
                                                }
                                                var keys = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { ClientId = clientId });
                                                var refTkn = keys.FirstOrDefault(g => g.Type == TokenTypes.RefreshToken && g.ClientId == clientId);
                                                if (refTkn != null && !string.IsNullOrEmpty(refTkn.Key))
                                                {
                                                    // DELETE Refresh Token from PersistedGrants db table (IF EXISTS) AFTER Refresh Token has been Revoked
                                                    await _persistedGrantStore.RemoveAsync(refTkn.Key);
                                                }
                                                movedNext = await CustomResponseAsync(httpContext, _next, null, StatusCodes.Status200OK);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // InValid Token Type Hint
                            if (httpContext.Request.HasFormContentType)
                            {
                                var form = await httpContext.Request.ReadFormAsync();
                                if (form != null)
                                {
                                    string clientId = form[RevocationRequest.ClientId].FirstOrDefault();
                                    var client = await _clientService.FindClientById(clientId);
                                    if (client != null)
                                    {
                                        var keys = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { ClientId = clientId });
                                        var refTkn = keys.FirstOrDefault(g => g.Type == TokenTypes.RefreshToken && g.ClientId == clientId);
                                        if (refTkn != null && !string.IsNullOrEmpty(refTkn.Key))
                                        {
                                            // DELETE Refresh Token from PersistedGrants db table to InValidate the Access Token
                                            await _persistedGrantStore.RemoveAsync(refTkn.Key);
                                        }
                                    }
                                }
                            }
                            movedNext = await CustomResponseAsync(httpContext, _next, null, StatusCodes.Status200OK);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "revoke error: ");
                }
                finally
                {
                    if (!movedNext)
                        await _next(httpContext);
                }
            }
            else
            {
                await _next.Invoke(httpContext);
            }
        }

        protected bool ValidateAccessTokenToken(string authToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();
                SecurityToken validatedToken;
                IPrincipal principal = tokenHandler.ValidateToken(authToken, validationParameters, out validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }

        protected TokenValidationParameters GetValidationParameters()
        {
            var issuerUri = _configurationSettings.GetValue<string>("IssuerUri");
            var filePath = _configurationSettings["SigningCertificate:Path"];
            var pwd = _configurationSettings["SigningCertificate:Password"];
            var cert = new X509Certificate2(filePath, pwd);
            var certSecurityKey = new X509SecurityKey(cert);

            return new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidIssuer = issuerUri,
                ValidateAudience = false,
                ValidateIssuerSigningKey = false,
                ValidateLifetime = true,
                IssuerSigningKey = certSecurityKey
            };
        }

        protected async Task<bool> CustomResponseAsync(HttpContext httpContext, RequestDelegate _next, string errMsg, int statusCode)
        {
            var originBody = httpContext.Response.Body;
            try
            {
                var memStream = new MemoryStream();
                httpContext.Response.Body = memStream;

                await _next.Invoke(httpContext);

                // To be able to update the Response Status Code it requires the body to be buffered, the status code
                // to then be updated, then the same resonse body returned for the next item in the pipeline to read.
                // If this is not done this way the response status code AFTER being updated here always returns
                // 400 BadRequest.
                var memoryStreamModified = new MemoryStream();

                if (errMsg != null)
                {
                    var responseBody = errMsg;
                    var sw = new StreamWriter(memoryStreamModified);
                    sw.Write(responseBody);
                    sw.Flush();
                    memoryStreamModified.Position = 0;
                }
                if (!httpContext.Response.HasStarted)
                    httpContext.Response.StatusCode = statusCode;

                await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
            }
            finally
            {
                httpContext.Response.Body = originBody;
            }
            return true;
        }
    }
}