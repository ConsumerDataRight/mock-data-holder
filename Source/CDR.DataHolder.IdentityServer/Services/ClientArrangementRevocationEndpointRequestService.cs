using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{

    public class ClientArrangementRevocationEndpointRequestService : IClientArrangementRevocationEndpointRequestService
    {
        private readonly IConfiguration _config;
        private readonly ISecurityService _securityService;
        private readonly IClientArrangementRevocationEndpointHttpClient _clientArrangementRevocationEndpointHttpClient;
        private readonly ILogger _logger;
        private readonly int _defaultExpiryMinutes;
        private readonly IClientService _clientService;
        private readonly ICustomGrantService _customGrantService;

        public ClientArrangementRevocationEndpointRequestService(
            ILogger<ClientArrangementRevocationEndpointRequestService> logger,
            ISecurityService securityService,
            IConfiguration config,
            IClientArrangementRevocationEndpointHttpClient clientArrangementRevocationEndpointHttpClient,
            IClientService clientService,
            ICustomGrantService customGrantService)
        {
            _securityService = securityService;
            _config = config;
            _logger = logger;
            _defaultExpiryMinutes = 5;
            _clientArrangementRevocationEndpointHttpClient = clientArrangementRevocationEndpointHttpClient;
            _clientService = clientService;
            _customGrantService = customGrantService;
        }

        /// <summary>
        /// Send a arrangement revocation request to the relevant DR.
        /// </summary>
        /// <param name="cdrArrangementId">CDR Arrangement ID</param>
        /// <param name="useJwt">Send CDR Arrangement ID in JWT parameter</param>
        /// <returns></returns>
        public async Task<DataRecipientRevocationResult> SendRevocationRequest(string cdrArrangementId, bool useJwt = false)
        {
            using (LogContext.PushProperty("MethodName", "SendRevocationRequest"))
            {
                _logger.LogInformation("invoked with cdrArrangementId: {cdrArrangementId}", cdrArrangementId);
            }

            var result = new DataRecipientRevocationResult() { CdrArrangementId = cdrArrangementId };

            // Find the CDR Arrangement Grant.
            var grant = await _customGrantService.GetGrant(cdrArrangementId);

            // "cdr_arrangement_grant" grant not found for given id. 
            if (grant == null
            || !grant.Type.Equals(CdsConstants.GrantTypes.CdrArrangementGrant))
            {
                _logger.LogError("Invalid consent arrangement: {grant}", (grant == null ? "not found" : $"grant type: {grant.Type}"));
                result.Errors.Add(new Error() { Code = "urn:au-cds:error:cds-all:Authorisation/InvalidArrangement", Title = "Invalid Consent Arrangement", Detail = cdrArrangementId });
                result.IsSuccessful = false;
                return result;
            }

            // Find the associated client id.
            var client = await _clientService.FindClientById(grant.ClientId);
            if (client == null)
            {
                _logger.LogError("Invalid consent arrangement: client ({clientId}) not found", grant.ClientId);
                result.Errors.Add(new Error() { Code = "urn:au-cds:error:cds-all:GeneralError/Unexpected", Title = "Unexpected Error Encountered", Detail = $"Client {grant.ClientId} not found for arrangement: {cdrArrangementId}" });
                result.IsSuccessful = false;
                return result;
            }
            result.ClientId = client.ClientId;

            // Get the revocation uri for the client.
            var recipientBaseUriClaim = client.Claims.FirstOrDefault(c => c.Type == CdsConstants.ClientMetadata.RecipientBaseUri);
            if (recipientBaseUriClaim == null)
            {
                _logger.LogError("Invalid consent arrangement: client {recipientBaseUri} not found", CdsConstants.ClientMetadata.RecipientBaseUri);
                result.Errors.Add(new Error() { Code = "urn:au-cds:error:cds-all:GeneralError/Unexpected", Title = "Unexpected Error Encountered", Detail = $"{CdsConstants.ClientMetadata.RecipientBaseUri} not found for client {grant.ClientId}" });
                result.IsSuccessful = false;
                return result;
            }

            // Build the parameters for the call to the DR's arrangement revocation endpoint.
            var revocationUri = new Uri($"{recipientBaseUriClaim.Value}/arrangements/revoke");
            var brandId = _config.GetValue<string>("BrandId");
            var jwtSecurityToken = new JwtSecurityToken(
               claims: new Claim[]
               {
                 new Claim(JwtRegisteredClaimNames.Sub, brandId),
                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               },
               issuer: brandId,
               audience: revocationUri.ToString(),
               expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));
            var signedBearerTokenJwt = await GetSignedJwt(jwtSecurityToken);

            _logger.LogInformation("Calling DR arrangement revocation endpoint ({revocationUri})...", revocationUri);

            // Call the DR's arrangement revocation endpoint.
            var httpResponse = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                (await GetFormValues(cdrArrangementId, brandId, revocationUri.ToString(), useJwt)),
                signedBearerTokenJwt,
                revocationUri);

            _logger.LogInformation("Response from DR arrangement revocation endpoint: {httpResponse}", httpResponse);

            result.Status = httpResponse.Status;
            result.IsSuccessful = (result.Status.HasValue && result.Status.Value == HttpStatusCode.NoContent);

            if (!result.IsSuccessful && !string.IsNullOrEmpty(httpResponse.Detail))
            {
                var respError = JsonConvert.DeserializeObject<API.Infrastructure.Models.ResponseErrorList>(httpResponse.Detail);
                foreach (var err in respError.Errors)
                {
                    result.Errors.Add(new Error() { Code = err.Code, Title = err.Title, Detail = err.Detail });
                }
            }
            return result;
        }

        public async Task<string> GetSignedJwt(JwtSecurityToken jwtSecurityToken)
        {
            var keys = await _securityService.GetActiveSecurityKeys(Algorithms.Signing.PS256);

            jwtSecurityToken.Header["alg"] = Algorithms.Signing.PS256;
            jwtSecurityToken.Header["kid"] = keys.First().Key.KeyId;
            jwtSecurityToken.Header["typ"] = JwtToken.JwtType;

            var plaintext = $"{jwtSecurityToken.EncodedHeader}.{jwtSecurityToken.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);
            var signature = Base64UrlTextEncoder.Encode(await _securityService.Sign(jwtSecurityToken.SignatureAlgorithm, digest));

            return $"{plaintext}.{signature}";
        }

        private async Task<Dictionary<string, string>> GetFormValues(
            string cdrArrangementId,
            string brandId,
            string audience,
            bool useJwt = false)
        {
            var formValues = new Dictionary<string, string>();

            if (useJwt)
            {
                var jwt = new JwtSecurityToken(
                    issuer: brandId,
                    audience: audience,
                    expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes),
                    claims: new Claim[] {
                        new Claim(CdrArrangementRevocationRequest.CdrArrangementId, cdrArrangementId),
                        new Claim(JwtRegisteredClaimNames.Sub, brandId),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    });
                formValues.Add(CdrArrangementRevocationRequest.CdrArrangementJwt, (await GetSignedJwt(jwt)));
            }
            else
            {
                formValues.Add(CdrArrangementRevocationRequest.CdrArrangementId, cdrArrangementId);
            }

            _logger.LogInformation("Arrangement revocation request using {form}:{form_value} ", formValues.First().Key, formValues.First().Value);

            return formValues;
        }
    }
}
