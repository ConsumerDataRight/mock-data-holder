using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class JwtRequestUriService : IJwtRequestUriService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<JwtRequestUriService> _logger;

        public JwtRequestUriService(ILogger<JwtRequestUriService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<JwtSecurityToken> GetJwtAsync(string jwtRequestUri, Client client)
        {
            var httpResponse = await _httpClient.GetAsync(jwtRequestUri);

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("{jwtRequestUri} returned 404.", jwtRequestUri);
                throw new JwtRequestUriEndpointNotFoundException(JwtRequestUriEndpointNotFoundException(jwtRequestUri));
            }
            else if (!httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError(
                    "{JwksUri} returned {Code} Content:\r\n{Content}",
                    jwtRequestUri,
                    httpResponse.StatusCode,
                    responseContent);
                throw new JwtRequestUriEndpointDidNotReturnSuccessException(JwtRequestUriEndpointDidNotReturnSuccessException(jwtRequestUri, httpResponse.StatusCode, responseContent));
            }

            return await GetJwtFromResponse(jwtRequestUri, httpResponse);
        }

        private static string JwtRequestUriEndpointNotFoundException(string jwtRequestUri) => $"{jwtRequestUri} returned 404.";

        private static string JwtRequestUriEndpointDidNotReturnSuccessException(string jwtRequestUri, HttpStatusCode code, string content) =>
            $"{jwtRequestUri} returned {code} Content:\r\n{content}";

        private async Task<JwtSecurityToken> GetJwtFromResponse(string jwtRequestUri, HttpResponseMessage httpResponse)
        {
            try
            {
                var jwt = await httpResponse.Content.ReadAsStringAsync();
                return new JwtSecurityToken(jwt);
            }
            catch
            {
                _logger.LogError("No valid JWT request found from {jwtRequestUri}", jwtRequestUri);
                throw new JwksEndpointDidNotReturnValidJwkException($"No valid JWT request found from {jwtRequestUri}");
            }
        }
    }

    public class JwtRequestUriEndpointNotFoundException : HttpRequestException
    {
        public JwtRequestUriEndpointNotFoundException(string message)
            : base(message)
        {
        }
    }

    public class JwtRequestUriEndpointDidNotReturnSuccessException : HttpRequestException
    {
        public JwtRequestUriEndpointDidNotReturnSuccessException(string message)
            : base(message)
        {
        }
    }

    public class JwtRequestUriEndpointDidNotReturnValidJwkException : HttpRequestException
    {
        public JwtRequestUriEndpointDidNotReturnValidJwkException(string message)
            : base(message)
        {
        }
    }
}