using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class JwksService : IJwksService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<JwksService> _logger;

        public JwksService(ILogger<JwksService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<JsonWebKeySet> GetJwks(Uri jwksUri)
        {
            var httpResponse = await _httpClient.GetAsync(jwksUri);

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("{JwksUri} returned 404.", jwksUri);
                throw new JwksEndpointNotFoundException(JwksUriNotFoundMessage(jwksUri));
            }
            else if (!httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError(
                    "{JwksUri} returned {Code} Content:\r\n{Content}",
                    jwksUri,
                    httpResponse.StatusCode,
                    responseContent);
                throw new JwksEndpointDidNotReturnSuccessException(JwksUriInvalidResponseMessage(jwksUri, httpResponse.StatusCode, responseContent));
            }

            return await GetJwksFromResponse(jwksUri, httpResponse);
        }

        private static string JwksUriNotFoundMessage(Uri jwksUri) => $"{jwksUri} returned 404.";

        private static string JwksUriInvalidResponseMessage(Uri jwksUri, HttpStatusCode code, string content) =>
            $"{jwksUri} returned {code} Content:\r\n{content}";

        private async Task<JsonWebKeySet> GetJwksFromResponse(Uri jwksUri, HttpResponseMessage httpResponse)
        {
            try
            {
                return await httpResponse.Content.ReadAsJson<JsonWebKeySet>();
            }
            catch
            {
                _logger.LogError("No valid JWKS found from {JwksUri}", jwksUri);
                throw new JwksEndpointDidNotReturnValidJwkException($"No valid JWKS found from {jwksUri}");
            }
        }
    }

    public class JwksEndpointNotFoundException : HttpRequestException
    {
        public JwksEndpointNotFoundException(string message)
            : base(message)
        {
        }
    }

    public class JwksEndpointDidNotReturnSuccessException : HttpRequestException
    {
        public JwksEndpointDidNotReturnSuccessException(string message)
            : base(message)
        {
        }
    }

    public class JwksEndpointDidNotReturnValidJwkException : HttpRequestException
    {
        public JwksEndpointDidNotReturnValidJwkException(string message)
            : base(message)
        {
        }
    }
}