using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public class ClientArrangementRevocationEndpointHttpClient : IClientArrangementRevocationEndpointHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public ClientArrangementRevocationEndpointHttpClient(
            ILogger<ClientArrangementRevocationEndpointHttpClient> logger,
            HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<HttpStatusCode?> PostToArrangementRevocationEndPoint(Dictionary<string, string> formValues, string bearerTokenJwt, Uri arrangementRevocationUri)
        {
            if (!string.IsNullOrWhiteSpace(bearerTokenJwt))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerTokenJwt);
            }

            using var form = new FormUrlEncodedContent(formValues);
            try
            {
                var httpResponseMessage = await _client.PostAsync(arrangementRevocationUri, form);

                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Posted to Clients Arrangement Revocation URI. Response: {StatusCode} {ResponseContent}",
                    httpResponseMessage.StatusCode,
                    responseContent);

                return httpResponseMessage.StatusCode;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "A HttpRequest Exception occured while calling Arrangement Revocation Endpoint {RevocationUri} {BearerTokenJwt} {FormValues}",
                    arrangementRevocationUri,
                    bearerTokenJwt,
                    string.Join(Environment.NewLine, formValues));
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An unexpected exception occured while calling Arrangement Revocation Endpoint {RevocationUri} {BearerTokenJwt} {FormValues}",
                    arrangementRevocationUri,
                    bearerTokenJwt,
                    string.Join(Environment.NewLine, formValues));
                throw;
            }
        }
    }
}
