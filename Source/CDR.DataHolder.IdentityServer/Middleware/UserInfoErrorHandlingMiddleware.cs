using CDR.DataHolder.IdentityServer.Extensions;
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

        public UserInfoErrorHandlingMiddleware(ILogger<UserInfoErrorHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/connect/userinfo"
                && context.Request.Method == HttpMethods.Get
                && !context.Response.HasStarted
                && !context.ValidateCnf(_logger))
            {
                return;
            }

            await next.Invoke(context);

            if (context.Request.Path == "/connect/userinfo")
            {
                if (context.Request.Method != HttpMethods.Get)
                {
                    return;
                }

                try
                {
                    var originBody = context.Response.Body;

                    if (!context.Response.Headers.ContainsKey("WWW-Authenticate"))
                    {
                        return;
                    }

                    var customHeader = context.Response.Headers["WWW-Authenticate"];
                    var hasCustomLegalStatusErrors = customHeader.ToString().Contains("Invalid legal_status");
                    var hasCustomSoftwareProductErrors = customHeader.ToString().Contains("Invalid software_status_");

                    if (hasCustomLegalStatusErrors)
                    {
                        string customErrorDescription = SetLegalStatusError(customHeader.ToString());
                        await WriteResponse(context, originBody, customErrorDescription).ConfigureAwait(false);
                    }
                    else if (hasCustomSoftwareProductErrors)
                    {
                        string customErrorDescription = SetSoftwareProductStatusError(customHeader.ToString());
                        await WriteResponse(context, originBody, customErrorDescription).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing the UserInfo {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                }
            }
        }

        private static async Task WriteResponse(HttpContext context, Stream originBody, string customErrorDescription)
        {
            var memStream = new MemoryStream();
            context.Response.Body = memStream;
            memStream.Position = 0;

            var responseBody = customErrorDescription;
            var memoryStreamModified = new MemoryStream();
            var sw = new StreamWriter(memoryStreamModified);
            sw.Write(responseBody);
            sw.Flush();
            memoryStreamModified.Position = 0;

            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

            await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
        }

        private static string SetLegalStatusError(string statusError)
        {
            // Check for legal status entity errors. 
            if (statusError.Contains(UserInfoErrorCodes.InvalidLegalStatusInactive))
            {
                return SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusInactive);
            }

            if (statusError.Contains(UserInfoErrorCodes.InvalidLegalStatusRemoved))
            {
                return SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusRemoved);
            }

            if (statusError.Contains(UserInfoErrorCodes.InvalidLegalStatusRevoked))
            {
                return SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusRevoked);
            }

            if (statusError.Contains(UserInfoErrorCodes.InvalidLegalStatusSurrendered))
            {
                return SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusSurrendered);
            }

            if (statusError.Contains(UserInfoErrorCodes.InvalidLegalStatusSuspended))
            {
                return SetForbiddenErrorResponseLegalStatus(UserInfoStatusErrorDescriptions.StatusSuspended);
            }

            return String.Empty;
        }

        private static string SetSoftwareProductStatusError(string statusError)
        {
            if (statusError.Contains(UserInfoErrorCodes.InvalidSoftwareProductStatusInactive))
            {
                return SetForbiddenErrorResponseSoftwareProductStatus(UserInfoStatusErrorDescriptions.StatusInactive);
            }
            
            if (statusError.Contains(UserInfoErrorCodes.InvalidSoftwareProductStatusRemoved))
            {
                return SetForbiddenErrorResponseSoftwareProductStatus(UserInfoStatusErrorDescriptions.StatusRemoved);
            }

            return String.Empty;
        }

        

        private static string SetForbiddenErrorResponseLegalStatus(string status)
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

        private static string SetForbiddenErrorResponseSoftwareProductStatus(string status)
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