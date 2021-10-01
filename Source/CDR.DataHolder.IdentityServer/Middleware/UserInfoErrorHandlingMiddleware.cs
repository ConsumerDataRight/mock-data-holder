using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Middleware
{
    /// <summary>
    /// Applicable for the UserInfo Request End Point and if an Error is being returned
    /// IS4 returns an ErrorId which we need to use to get the real error as per ACs    
    /// </summary>
    public class UserInfoErrorHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<UserInfoErrorHandlingMiddleware> _logger;
        private readonly IPersistedGrantStore _persistedGrantStore;
        
        public UserInfoErrorHandlingMiddleware(ILogger<UserInfoErrorHandlingMiddleware> logger, 
                                                IPersistedGrantStore persistedGrantStore)
        {
            _logger = logger;
            _persistedGrantStore = persistedGrantStore;            
        }
        
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/connect/userinfo" && context.Request.Method == HttpMethods.Get)
            {
                if (!context.Response.HasStarted)
                {
                    string requestHeaderClientCertThumprint = null;
                    //MTLS validation for User Info middleware
                    if (context.Request.Headers.TryGetValue("X-TlsClientCertThumbprint", out StringValues headerThumbprints))
                    {
                        requestHeaderClientCertThumprint = headerThumbprints.First();
                    }

                    string accessTokenClientCertThumbprint = null;
                    
                    try
                    {                        
                        var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]);
                        var jwtAccessToken = authHeader.Parameter;

                        var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(jwtAccessToken);

                        //Client Confirmation claim from the access token
                        //When a client obtains an access token and has authenticated with mutual TLS,
                        //IdentityServer issues a confirmation claim(or cnf) in the access token.
                        //This value is a hash of the thumbprint of the client certificate used to authenticate with IdentityServer.

                        var cnfJson = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "cnf")?.Value;
                        if (!string.IsNullOrWhiteSpace(cnfJson))
                        {
                            var cnf = JObject.Parse(cnfJson);
                            accessTokenClientCertThumbprint = cnf.Value<string>("x5t#S256");
                        }

                        if (!accessTokenClientCertThumbprint.Equals(requestHeaderClientCertThumprint, System.StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogError($"Unauthorized UserInfo request. X-TlsClientCertThumbprint request header value '{requestHeaderClientCertThumprint}' does not match access token cnf:x5t#S256 claim value '{accessTokenClientCertThumbprint}'");
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {                        
                        _logger.LogError("Error processing the UserInfo Request JWT {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;                        
                    }                    
                }
            }

            await next.Invoke(context);


            if (context.Request.Path == "/connect/userinfo")
            {                                                
                try
                {
                    if (context.Request.Method == HttpMethods.Get)
                    {

                        var originBody = context.Response.Body;
                        
                        if (context.Response.Headers.ContainsKey("WWW-Authenticate"))
                        {                                
                            var customHeader = context.Response.Headers["WWW-Authenticate"];
                                
                            var hasCustomLegalStatusErrors = customHeader.ToString().Contains("Invalid legal_status");
                            var hasCustomSoftwareProductErrors = customHeader.ToString().Contains("Invalid software_status_");

                            if (hasCustomLegalStatusErrors)
                            {                                
                                //Capturing custom errors                                    
                                //Check for legal status entity errors. 
                                var legalEntityStatusError = customHeader.ToString();
                                string customErrorDescription = string.Empty;

                                var memStream = new MemoryStream();
                                context.Response.Body = memStream;

                                memStream.Position = 0;
                                var responseBody = new StreamReader(memStream).ReadToEnd();

                                if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidLegalStatusInactive))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusInactive);
                                }
                                else if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidLegalStatusRemoved))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusRemoved);
                                }
                                else if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidLegalStatusRevoked))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusRevoked);
                                }
                                else if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidLegalStatusSurrendered))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusSurrendered);
                                }
                                else if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidLegalStatusSuspended))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusSuspended);
                                }

                                responseBody = customErrorDescription;

                                var memoryStreamModified = new MemoryStream();
                                var sw = new StreamWriter(memoryStreamModified);
                                sw.Write(responseBody);
                                sw.Flush();
                                memoryStreamModified.Position = 0;

                                if (!context.Response.HasStarted)
                                    context.Response.StatusCode = StatusCodes.Status403Forbidden;

                                await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
                            }
                            else if (hasCustomSoftwareProductErrors)
                            {

                                var legalEntityStatusError = customHeader.ToString();
                                string customErrorDescription = string.Empty;

                                var memStream = new MemoryStream();
                                context.Response.Body = memStream;

                                memStream.Position = 0;
                                var responseBody = new StreamReader(memStream).ReadToEnd();

                                if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidSoftwareProductStatusInactive))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseSoftwareProductStatus(UserInfoStatusErrorDescriptions.StatusInactive);
                                }
                                else if (legalEntityStatusError.Contains(UserInfoErrorCodes.InvalidSoftwareProductStatusRemoved))
                                {
                                    customErrorDescription = await SetForbiddenErrorResponseSoftwareProductStatus(UserInfoStatusErrorDescriptions.StatusRemoved);
                                }

                                responseBody = customErrorDescription;

                                var memoryStreamModified = new MemoryStream();
                                var sw = new StreamWriter(memoryStreamModified);
                                sw.Write(responseBody);
                                sw.Flush();
                                memoryStreamModified.Position = 0;

                                if (!context.Response.HasStarted)
                                    context.Response.StatusCode = StatusCodes.Status403Forbidden;

                                await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
                            }                            
                        }                            
                    }
                }
                catch (Exception ex)
                {                        
                    _logger.LogError("Error processing the UserInfo {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                    return;
                }                
            }         
        }

        private async Task<string> SetForbiddenErrorResponseLegalStatus(string status)
        {            
            var errorMessage = $@"{{
                            ""errors"": [
                                {{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""ADR status is {status}"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";

            return errorMessage;
        }

        private async Task<string> SetForbiddenErrorResponseSoftwareProductStatus(string status)
        {
            var errorMessage = $@"{{
                            ""errors"": [
                                {{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""Software product status is {status}"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";

            return errorMessage;
        }
    }
}