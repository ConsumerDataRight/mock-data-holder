using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class ClientRegistrationRequestValidator : IClientRegistrationRequestValidator
    {
        private readonly ILogger<ClientRegistrationRequestValidator> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSettings _configurationSettings;
        private IClientRegistrationRequest _registrationRequestObject;
        private readonly List<ValidationResult> _validationResults = new();

        public ClientRegistrationRequestValidator(
            IConfiguration configuration, 
            ILogger<ClientRegistrationRequestValidator> logger,
            IConfigurationSettings configurationSettings)
        {
            // [ABA] Request processing:
            // 1. Decode the request JWT received from Data Recipient, without validating the signature.
            // 2. Extract the software statement from the decoded request JWT.
            // 3. Validate the software statement JWT with the CDR Registry JWKS endpoint.
            // 4. Extract the ADR software JWKS endpoint from the software statement.
            // 5. Validate the request JWT using the ADR software JWKS endpoint extracted.
            _configuration = configuration;
            _logger = logger;
            _configurationSettings = configurationSettings;
        }

        public async Task<IEnumerable<ValidationResult>> ValidateAsync(IClientRegistrationRequest request)
        {
            _registrationRequestObject = request;

            var audience = _configurationSettings.Registration.AudienceUri;

            // 1. SSA validation first.  If it fails, then exit as no point in validating anything else.
            if (!(await ValidateSSA()))
            {
                return _validationResults;
            }

            // 2. Signature validation to determine if we can rely on the contents of the registration request jwt.
            if (!(await ValidateRequestSignature()))
            {
                return _validationResults;
            }

            // 3. Validate the sector identifier uri
            if (!(await ValidateSectorIdentifierUri(_registrationRequestObject.SoftwareStatement.SectorIdentifierUri)))
            {
                return _validationResults;
            }

            //
            // Signature validation has been completed successfully.
            //

            // 4. Basic validation.
            CheckMandatory(request.Iss, nameof(request.Iss));
            CheckMandatory(request.Iat, nameof(request.Iat));
            CheckMandatory(request.Exp, nameof(request.Exp));
            CheckMandatory(request.Jti, nameof(request.Jti));
            MustEqual(request.Aud, nameof(request.Aud), audience);
            MustEqual(request.Iss, nameof(request.Iss), request.SoftwareStatement.SoftwareId);
            MustBeOne(request.TokenEndpointAuthSigningAlg, nameof(request.TokenEndpointAuthSigningAlg), new string[] { "PS256", "ES256" });
            MustEqual(request.TokenEndpointAuthMethod, nameof(request.TokenEndpointAuthMethod), "private_key_jwt");
            MustBeOne(request.IdTokenSignedResponseAlg, nameof(request.IdTokenSignedResponseAlg), new string[] { "PS256", "ES256" });
            MustBeOne(request.IdTokenEncryptedResponseAlg, nameof(request.IdTokenEncryptedResponseAlg), new string[] { "RSA-OAEP", "RSA-OAEP-256" });
            MustBeOne(request.IdTokenEncryptedResponseEnc, nameof(request.IdTokenEncryptedResponseEnc), new string[] { "A256GCM", "A128CBC-HS256" });
            MustContain(request.GrantTypes, nameof(request.GrantTypes), "authorization_code");

            if (!string.IsNullOrEmpty(request.RequestObjectSigningAlg))
            {
                MustBeOne(request.RequestObjectSigningAlg, nameof(request.RequestObjectSigningAlg), new string[] { "PS256", "ES256" });
            }

            if (!string.IsNullOrEmpty(request.ApplicationType))
            {
                MustEqual(request.ApplicationType, nameof(request.ApplicationType), "web");
            }

            foreach (var redirectUri in request.RedirectUris)
            {
                bool isValid = MustBeOne(redirectUri, nameof(request.RedirectUris), request.SoftwareStatement.RedirectUris, CdsConstants.ValidationErrorMessages.InvalidRedirectUri);
                if (!isValid)
                {
                    break;
                }
            }

            foreach (var responseType in request.ResponseTypes)
            {
                MustEqual(responseType, nameof(request.ResponseTypes), "code id_token");
            }

            // Return the validation results.
            return _validationResults;
        }

        private bool CheckMandatory(object propValue, string propName)
        {
            if (propValue == null || (propValue as string) == "")
            {
                _validationResults.Add(new ValidationResult(string.Format(CdsConstants.ValidationErrorMessages.MissingClaim, GetDisplayName(propName)), new string[] { propName }));
                return false;
            }

            return true;
        }

        private bool MustBeOne(object propValue, string propName, IEnumerable<string> expectedValues, string customErrorMessage = null)
        {
            if (!CheckMandatory(propValue, propName))
            {
                return false;
            }

            if (!expectedValues.Contains(propValue.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(customErrorMessage))
                {
                    _validationResults.Add(new ValidationResult(string.Format(CdsConstants.ValidationErrorMessages.MustBeOne, GetDisplayName(propName), String.Join(",", expectedValues)), new string[] { propName }));
                }
                else
                {
                    _validationResults.Add(new ValidationResult(customErrorMessage, new string[] { propName }));
                }

                return false;
            }

            return true;
        }

        private void MustEqual(object propValue, string propName, string expectedValue)
        {
            if (!CheckMandatory(propValue, propName))
            {
                return;
            }

            if (!propValue.ToString().Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                _validationResults.Add(new ValidationResult(string.Format(CdsConstants.ValidationErrorMessages.MustEqual, GetDisplayName(propName), expectedValue), new string[] { propName }));
            }
        }

        private void MustContain(IEnumerable<string> propValue, string propName, string expectedValue)
        {
            if (!propValue.Contains(expectedValue, StringComparer.OrdinalIgnoreCase))
            {
                _validationResults.Add(new ValidationResult(string.Format(CdsConstants.ValidationErrorMessages.MustContain, GetDisplayName(propName), expectedValue), new string[] { propName }));
            }
        }

        private async Task<bool> ValidateRequestSignature()
        {
            _logger.LogInformation($"{nameof(ClientRegistrationRequestValidator)}.{nameof(ValidateRequestSignature)}");

            // Get the Data Recipient's JWKS from the Software Statement.
            var jwks = await GetJwks(_registrationRequestObject.SoftwareStatement.JwksUri);

            if (jwks == null || jwks.Keys.Count == 0)
            {
                _validationResults.Add(new ValidationResult($"Could not load JWKS from Data Recipient endpoint: {_registrationRequestObject.SoftwareStatement.JwksUri}"));
                return false;
            }

            // Assert - Validate Registration Request Signature
            var validationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),

                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jwks.Keys,

                ValidateAudience = true,
                ValidAudiences = _configuration.GetValidAudiences(),

                ValidateIssuer = true,
                ValidIssuer = _registrationRequestObject.SoftwareStatement.SoftwareId,
            };

            // Validate token.
            try
            {
                new JwtSecurityTokenHandler().ValidateToken(_registrationRequestObject.ClientRegistrationRequestJwt, validationParameters, out var validatedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client Registration Request validation failed.");
                _validationResults.Add(new ValidationResult($"Client Registration Request validation failed. {ex.Message}"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate the sectory identifier url.
        /// Currently it is only required to call this endpoint and we do not validate the output.
        /// </summary>
        private async Task<bool> ValidateSectorIdentifierUri(string sectorIdentifierUri)
        {
            _logger.LogInformation($"{nameof(ClientRegistrationRequestValidator)}.{nameof(ValidateSectorIdentifierUri)}");

            if (string.IsNullOrEmpty(sectorIdentifierUri))
            {
                _logger.LogInformation("Sector URI not found");
                return true;
            }

            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            _logger.LogInformation("Sending a request to: {sectorIdentifierUri}", sectorIdentifierUri);

            var sectorIdClient = new HttpClient(clientHandler);
            var sectorIdResponse = await sectorIdClient.GetAsync(sectorIdentifierUri);
            return sectorIdResponse.IsSuccessStatusCode;
        }

        private async Task<bool> ValidateSSA()
        {
            _logger.LogInformation($"{nameof(ClientRegistrationRequestValidator)}.{nameof(ValidateSSA)}");

            if (_registrationRequestObject.SoftwareStatement == null)
            {
                _validationResults.Add(new ValidationResult("The software_statement is empty or invalid.", new string[] { nameof(_registrationRequestObject.SoftwareStatement) }));
                return false;
            }

            // Get the SSA JWKS from the Register.
            var ssaJwks = await GetJwks(_configurationSettings.RegisterSsaJwksUri);

            if (ssaJwks == null || ssaJwks.Keys.Count == 0)
            {
                _validationResults.Add(new ValidationResult($"Could not load SSA JWKS from Register endpoint: {_configurationSettings.RegisterSsaJwksUri}", new string[] { nameof(_registrationRequestObject.SoftwareStatement) }));
                return false;
            }

            // Assert - Validate SSA Signature
            var validationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),

                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = ssaJwks.Keys.First(),

                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = "cdr-register",
            };

            // Validate token.
            try
            {
                new JwtSecurityTokenHandler().ValidateToken(_registrationRequestObject.SoftwareStatementJwt, validationParameters, out var validatedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSA validation failed.");
                _validationResults.Add(new ValidationResult($"SSA validation failed. {ex.Message}", new string[] { "SoftwareStatement.Signature" }));
                return false;
            }

            return true;
        }

        private async Task<JsonWebKeySet> GetJwks(string jwksEndpoint)
        {
            _logger.LogInformation($"{nameof(ClientRegistrationRequestValidator)}.{nameof(GetJwks)}");

            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            _logger.LogInformation("Retrieving JWKS from: {jwksEndpoint}", jwksEndpoint);

            var jwksClient = new HttpClient(clientHandler);
            var jwksResponse = await jwksClient.GetAsync(jwksEndpoint);
            return new JsonWebKeySet(await jwksResponse.Content.ReadAsStringAsync());
        }

        private string GetDisplayName(string propName)
        {
            var propInfo = _registrationRequestObject.GetType().GetProperty(propName);

            if (propInfo == null)
            {
                return propName;
            }

            var displayAttr = propInfo.GetCustomAttributes(false).OfType<DisplayAttribute>().FirstOrDefault(); 

            if (displayAttr != null)
            {
                return displayAttr.Name;
            }

            return propName;
        }
    }
}
