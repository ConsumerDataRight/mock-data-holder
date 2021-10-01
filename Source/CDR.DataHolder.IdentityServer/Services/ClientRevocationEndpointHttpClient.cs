using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public class ClientRevocationEndpointHttpClient : IClientRevocationEndpointHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public ClientRevocationEndpointHttpClient(
            ILogger<ClientRevocationEndpointHttpClient> logger,
            HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<HttpResponseMessage> PostToRevocationEndPoint(Dictionary<string, string> formValues, string bearerTokenJwt, Uri revocationUri)
        {
            if (!string.IsNullOrWhiteSpace(bearerTokenJwt))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerTokenJwt);
            }

            using var form = new FormUrlEncodedContent(formValues);
            try
            {
                var httpResponseMessage = await _client.PostAsync(revocationUri, form);

                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Posted to Clients Revocation URI. Response: {StatusCode} {ResponseContent}",
                    httpResponseMessage.StatusCode,
                    responseContent);

                return httpResponseMessage;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "A HttpRequest Exception occured while calling Revocation Endpoint {RevocationUri} {BearerTokenJwt} {FormValues}",
                    revocationUri,
                    bearerTokenJwt,
                    string.Join(Environment.NewLine, formValues));
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An unexpected exception occured while calling Revocation Endpoint {RevocationUri} {BearerTokenJwt} {FormValues}",
                    revocationUri,
                    bearerTokenJwt,
                    string.Join(Environment.NewLine, formValues));
                throw;
            }
        }
    }
}
