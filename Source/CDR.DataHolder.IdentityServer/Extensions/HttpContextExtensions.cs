using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class HttpContextExtensions
    {
        public static bool ValidateCnf(this HttpContext context, ILogger logger)
        {
            string requestHeaderClientCertThumprint = null;

            // MTLS validation for User Info middleware
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

                if (accessTokenClientCertThumbprint != null && !accessTokenClientCertThumbprint.Equals(requestHeaderClientCertThumprint, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError("Unauthorized request. X-TlsClientCertThumbprint request header value '{requestHeaderClientCertThumprint}' does not match access token cnf:x5t#S256 claim value '{accessTokenClientCertThumbprint}'", requestHeaderClientCertThumprint, accessTokenClientCertThumbprint);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing the Request JWT {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return false;
            }

            return true;
        }
    }
}
