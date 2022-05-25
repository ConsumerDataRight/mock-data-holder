using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_Token_API
    {
        public const string OMIT = "***OMIT**";

        public class Response
        {
            [JsonProperty("token_type")]
            public string? TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int? ExpiresIn { get; set; }

            [JsonProperty("access_token")]
            public string? AccessToken { get; set; }

            [JsonProperty("id_token")]
            public string? IdToken { get; set; }

            [JsonProperty("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonProperty("cdr_arrangement_id")]
            public string? CdrArrangementId { get; set; }

            [JsonProperty("scope")]
            public string? Scope { get; set; }
        };

        // Send token request, returning HttpResponseMessage
        static public async Task<HttpResponseMessage> SendRequest(
            string? authCode = null,
            bool? usePut = false,
            string? grantType = "authorization_code",
            string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
            string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
            bool? useClientAssertion = true,
            int? shareDuration = null,
            string? refreshToken = null,
            string? customClientAssertion = null,
            string? scope = null,
            string? redirectUri = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
            string? certificateFilename = BaseTest.CERTIFICATE_FILENAME,
            string? certificatePassword = BaseTest.CERTIFICATE_PASSWORD,
            string? jwkCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string? jwkCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            redirectUri = BaseTest.SubstituteConstant(redirectUri);

            var URL = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/token";

            var formFields = new List<KeyValuePair<string?, string?>>
            {
                new KeyValuePair<string?, string?>("redirect_uri", redirectUri),
            };

            if (authCode != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("code", authCode));
            }

            if (grantType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("grant_type", grantType));
            }

            if (clientId != null && clientId != OMIT)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_id", clientId.ToLower()));
            }

            if (clientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion_type", clientAssertionType));
            }

            if (customClientAssertion != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion", customClientAssertion));
            }
            else
            {
                if (useClientAssertion == true)
                {
                    var clientAssertion = new PrivateKeyJwt2
                    {

                        CertificateFilename = jwkCertificateFilename,
                        CertificatePassword = jwkCertificatePassword,

                        // Allow for clientId to be deliberately omitted from the JWT
                        Issuer = clientId == OMIT ?
                             "" : // Omit
                             (clientId ?? BaseTest.SOFTWAREPRODUCT_ID).ToLower(), 

                        // Don't check for issuer if we are deliberately omitting clientId
                        RequireIssuer = clientId != OMIT,  

                        Audience = URL
                    }.Generate();

                    formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion));
                }
            }

            if (shareDuration != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("share_duration", shareDuration.ToString()));
            }

            if (refreshToken != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("refresh_token", refreshToken));
            }

            if (scope != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("scope", scope));
            }

            var content = new FormUrlEncodedContent(formFields);

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            clientHandler.ClientCertificates.Add(new X509Certificate2(certificateFilename ?? throw new NullReferenceException(), certificatePassword, X509KeyStorageFlags.Exportable));

            using var client = new HttpClient(clientHandler);

            var responseMessage = usePut == true ?
                await client.PutAsync(URL, content) :
                await client.PostAsync(URL, content);

            return responseMessage;
        }

        static public async Task<Response?> DeserializeResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (String.IsNullOrEmpty(responseContent))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Response>(responseContent);
        }

        /// <summary>
        /// Use authCode to get access token
        /// </summary>
        static public async Task<string?> GetAccessToken(string authCode)
        {
            var responseMessage = await SendRequest(authCode);
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(GetAccessToken)} - Error getting access token");
            }

            var tokenResponse = await DeserializeResponse(responseMessage);
            return tokenResponse?.AccessToken;
        }

        /// <summary>
        /// Use authCode to get tokens. 
        /// </summary>
        static public async Task<Response?> GetResponse(string authCode, int? shareDuration = null,
            string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
            string? redirectUri = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
            string? certificateFilename = BaseTest.CERTIFICATE_FILENAME,
            string? certificatePassword = BaseTest.CERTIFICATE_PASSWORD,
            string? jwkCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string? jwkCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            redirectUri = BaseTest.SubstituteConstant(redirectUri);

            var responseMessage = await SendRequest(authCode, shareDuration: shareDuration,
                clientId: clientId,
                redirectUri: redirectUri,
                certificateFilename: certificateFilename,
                certificatePassword: certificatePassword,
                jwkCertificateFilename: jwkCertificateFilename,
                jwkCertificatePassword: jwkCertificatePassword
            );

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(GetResponse)} - Error getting response");
            }

            var response = await DeserializeResponse(responseMessage);

            return response;
        }

        /// <summary>
        /// Use refresh token to get tokens
        /// </summary>
        static public async Task<Response?> GetResponseUsingRefreshToken(string? refreshToken, string? scope = null)
        {
            _ = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));

            var tokenResponseMessage = await SendRequest(
                grantType: "refresh_token",
                refreshToken: refreshToken,
                scope: scope
            );

            if (tokenResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                var content = await tokenResponseMessage.Content.ReadAsStringAsync();
                throw new Exception($"{nameof(GetResponseUsingRefreshToken)} - Error getting response - {content}");
            }

            var tokenResponse = await DeserializeResponse(tokenResponseMessage);

            return tokenResponse;
        }
    }
}
