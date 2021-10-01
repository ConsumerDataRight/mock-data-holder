using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Serialization;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using AuthorizeResponse = IdentityServer4.ResponseHandling.AuthorizeResponse;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class AuthorizeRequestUriService : IAuthorizeRequestUriService
    {
        private readonly ILogger<AuthorizeRequestUriService> _logger;
        private readonly IPersistedGrantStore _persistedGrantStore;
        private readonly IAuthorizeResponseGenerator _authorizeResponseGenerator;
        private readonly ICustomGrantService _customGrantService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizeRequestUriService(
            ILogger<AuthorizeRequestUriService> logger,
            IPersistedGrantStore persistedGrantStore,
            IAuthorizeResponseGenerator authorizeResponseGenerator,
            ICustomGrantService customGrantService,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _logger = logger;
            _persistedGrantStore = persistedGrantStore;
            _authorizeResponseGenerator = authorizeResponseGenerator;
            _customGrantService = customGrantService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEndpointResult> ProcessAsync(string request_uri_key, string client_id, NameValueCollection nvcParameters)
        {
            var persistedGrant = await _persistedGrantStore.GetAsync(request_uri_key);

            // validate persistedGrant
            if (persistedGrant == null || string.IsNullOrWhiteSpace(persistedGrant.Data))
            {
                return GenerateAuthorizeRequestUriErrorResult(AuthorizeErrorCodes.InvalidRequestUri, ValidationCheck.AuthorizeRequestNotFoundRequestUri);
            }

            var exp = persistedGrant.Expiration ?? DateTime.MinValue;
            var expCompare = exp.CompareTo(DateTime.Now);
            if (expCompare <= 0)
            {
                return GenerateAuthorizeRequestUriErrorResult(AuthorizeErrorCodes.InvalidRequestUri, ValidationCheck.AuthorizeRequestExpiredRequestUri);
            }

            if (!client_id.Equals(persistedGrant.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                return GenerateAuthorizeRequestUriErrorResult(AuthorizeErrorCodes.InvalidRequest, ValidationCheck.AuthorizeRequestInvalidClientId);
            }

            // deserialize ValidatedAuthorizeRequest object from json after it's validated
            var validatedAuthorizeRequest = DeserializeValidatedAuthorizeRequest(persistedGrant);

            bool paramValidatePass = ValidateParameters(nvcParameters, validatedAuthorizeRequest.RequestObjectValues, out string failedParameter);
            if (!paramValidatePass)
            {
                return GenerateParametercomparisonFailedErrorResult(failedParameter, validatedAuthorizeRequest);
            }

            // remove the request_uri from grant store as it's only allowed to be used once
            _logger.LogTrace("Invoking RemoveAsync method of PersistedGrantStore to remove grant key : {persistedGrant.Key}", persistedGrant.Key);
            await _persistedGrantStore.RemoveAsync(persistedGrant.Key);

            // need to enable user session before creating response
            // TODO: fix this
            var claimParameters = new Dictionary<string, object>();
            //{ Parameters = validatedAuthorizeRequest.Subject.Claims.ToArray());
            await _httpContextAccessor.HttpContext.SignInAsync(new IdentityServer4.IdentityServerUser(validatedAuthorizeRequest.ClientId), new Microsoft.AspNetCore.Authentication.AuthenticationProperties(null, claimParameters));

            _logger.LogTrace("Invoking CreateResponseAsync method of AuthorizeResponseGenerator to generate response for client id: {validatedAuthorizeRequest.ClientId}", validatedAuthorizeRequest.ClientId);
            var response = await _authorizeResponseGenerator.CreateResponseAsync(validatedAuthorizeRequest);

            // revoke existing refresh tokens and access tokens when a cdr_arrangement_id is provided in the authorisation request object but ONLY after successful authorisation
            if (!response.IsError)
            {
                var claims = validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(v => v.Key == AuthorizeRequest.Claims).Value;

                AuthorizeClaims authorizeClaims;

                try
                {
                    authorizeClaims = JsonConvert.DeserializeObject<AuthorizeClaims>(claims);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Claims {Claims} in Request JWT could not deserialize.", claims);
                    throw;
                }

                if (!string.IsNullOrWhiteSpace(authorizeClaims.CdrArrangementId))
                {
                    _logger.LogTrace("Invoking RevokeRefreshToken to revoke grant for cdrArrangementId: {cdrArrangementId}", authorizeClaims.CdrArrangementId);
                    await _customGrantService.RemoveGrantsForCdrArrangementId(authorizeClaims.CdrArrangementId, client_id);

                    _logger.LogTrace("Invoking UpdateCdrArrangementGrant to update AuthCode of Data field for cdrArrangementId: {cdrArrangementId}", authorizeClaims.CdrArrangementId);
                    await _customGrantService.UpdateCdrArrangementGrant(authorizeClaims.CdrArrangementId, response.Code);
                }
            }

            return new AuthorizeRequestUriResult(response, null);
        }

        private AuthorizeRequestUriResult GenerateParametercomparisonFailedErrorResult(string failedParameter, ValidatedAuthorizeRequest validatedAuthorizeRequest)
        {
            AuthorizeRequestUriResult authorizeRequestUriResult;
            switch (failedParameter)
            {
                case AuthorizeRequest.RedirectUri:
                    authorizeRequestUriResult = GenerateAuthorizeRequestUriErrorResult(AuthorizeErrorCodes.InvalidRequest, ValidationCheck.AuthorizeRequestInvalidRedirectUri);
                    break;
                case AuthorizeRequest.ResponseType:
                    authorizeRequestUriResult = GenerateAuthorizeRequestUriErrorResult(validatedAuthorizeRequest, AuthorizeErrorCodes.InvalidRequest, ValidationCheck.AuthorizeRequestInvalidResponseType);
                    break;
                case AuthorizeRequest.Scope:
                    authorizeRequestUriResult = GenerateAuthorizeRequestUriErrorResult(validatedAuthorizeRequest, AuthorizeErrorCodes.InvalidScope, ValidationCheck.AuthorizeRequestInvalidScope);
                    break;
                case AuthorizeRequest.State:
                    authorizeRequestUriResult = GenerateAuthorizeRequestUriErrorResult(validatedAuthorizeRequest, AuthorizeErrorCodes.InvalidRequest, ValidationCheck.AuthorizeRequestInvalidState);
                    break;
                case AuthorizeRequest.Nonce:
                    authorizeRequestUriResult = GenerateAuthorizeRequestUriErrorResult(validatedAuthorizeRequest, AuthorizeErrorCodes.InvalidRequest, ValidationCheck.AuthorizeRequestInvalidNonce);
                    break;
                default:
                    authorizeRequestUriResult = GenerateAuthorizeRequestUriErrorResult(validatedAuthorizeRequest, AuthorizeErrorCodes.InvalidRequest, ValidationCheck.AuthorizeRequestInvalidParameters);
                    break;
            }

            return authorizeRequestUriResult;
        }

        /// <summary>
        /// this will return 400 as redirect_uri is not set.
        /// </summary>
        /// <param name="error">error.</param>
        /// <param name="validationCheck">validationCheck.</param>
        /// <returns>AuthorizeRequestUriResult (IEndpointResult).</returns>
        public static AuthorizeRequestUriResult GenerateAuthorizeRequestUriErrorResult(string error, ValidationCheck validationCheck)
        {
            return new AuthorizeRequestUriResult(
                new AuthorizeResponse
                {
                    Error = error,
                    ErrorDescription = validationCheck.GetDescription(),
                }, validationCheck);
        }

        /// <summary>
        /// this will return 302 as redirect_uri exists (from ValidatedAuthorizeRequest).
        /// </summary>
        /// <param name="validatedAuthorizeRequest">validatedAuthorizeRequest.</param>
        /// <param name="error">error.</param>
        /// <param name="validationCheck">validationCheck.</param>
        /// <returns>AuthorizeRequestUriResult (IEndpointResult).</returns>
        private static AuthorizeRequestUriResult GenerateAuthorizeRequestUriErrorResult(ValidatedAuthorizeRequest validatedAuthorizeRequest, string error, ValidationCheck validationCheck)
        {
            return new AuthorizeRequestUriResult(
                new AuthorizeResponse
                {
                    Request = validatedAuthorizeRequest,
                    Error = error,
                    ErrorDescription = validationCheck.GetDescription(),
                }, validationCheck);
        }

        private bool ValidateParameters(NameValueCollection nvcParameters, Dictionary<string, string> requestObjectValues, out string failedParameter)
        {
            // No additional parameters need to be compared, so return true
            if (nvcParameters == null || nvcParameters.Count == 0)
            {
                failedParameter = string.Empty;
                return true;
            }

            var keys = nvcParameters.AllKeys;
            foreach (var key in keys)
            {
                var paramValue = nvcParameters[key];
                if (string.IsNullOrWhiteSpace(paramValue))
                {
                    continue;
                }

                var valueInData = requestObjectValues.FirstOrDefault(i => i.Key == key).Value;
                if (string.IsNullOrWhiteSpace(valueInData))
                {
                    failedParameter = key;
                    return false;
                }

                // e.g. scope value can be multiple ones in different order, split by space
                if (paramValue.Contains(" ", StringComparison.OrdinalIgnoreCase))
                {
                    var paramValueArray = paramValue.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    var valueInDataArray = valueInData.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                    if (paramValueArray.Length != valueInDataArray.Length)
                    {
                        failedParameter = key;
                        return false;
                    }

                    foreach (var value in paramValueArray)
                    {
                        if (!valueInData.Contains(value, StringComparison.OrdinalIgnoreCase))
                        {
                            failedParameter = key;
                            return false;
                        }
                    }
                }
                else
                {
                    if (!paramValue.Equals(valueInData, StringComparison.OrdinalIgnoreCase))
                    {
                        failedParameter = key;
                        return false;
                    }
                }
            }

            failedParameter = string.Empty;
            return true;
        }

        private static ValidatedAuthorizeRequest DeserializeValidatedAuthorizeRequest(PersistedGrant persistedGrant)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            settings.Converters.Add(new ClaimConverter());
            settings.Converters.Add(new ClaimsPrincipalConverter());
            //settings.Converters.Add(new ScopeValidatorConverter());
            var validatedAuthorizeRequest = JsonConvert.DeserializeObject<ValidatedAuthorizeRequest>(persistedGrant.Data, settings);

            var rawCollection = new NameValueCollection();
            rawCollection.Add(AuthorizeRequest.Scope, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.Scope).Value);
            rawCollection.Add(AuthorizeRequest.ResponseType, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.ResponseType).Value);
            rawCollection.Add(AuthorizeRequest.ClientId, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.ClientId).Value);
            rawCollection.Add(AuthorizeRequest.RedirectUri, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.RedirectUri).Value);
            rawCollection.Add(AuthorizeRequest.State, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.State).Value);
            rawCollection.Add(AuthorizeRequest.Nonce, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.Nonce).Value);
            rawCollection.Add(AuthorizeRequest.Claims, validatedAuthorizeRequest.RequestObjectValues.FirstOrDefault(i => i.Key == AuthorizeRequest.Claims).Value);
            validatedAuthorizeRequest.Raw = rawCollection;

            return validatedAuthorizeRequest;
        }
    }
}
