using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_Par_API
    {
        public class Response
        {
            [JsonProperty("request_uri")]
            public string? RequestURI { get; set; }

            [JsonProperty("expires_in")]
            public int? ExpiresIn { get; set; }
        };

        static public async Task<HttpResponseMessage> SendRequest(
             string? grantType = "client_credentials",
             string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
             string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
             string? cdrArrangementId = null,
             string? clientAssertion = null,
             string? certificateFilename = BaseTest.CERTIFICATE_FILENAME,
             string? certificatePassword = BaseTest.CERTIFICATE_PASSWORD,
             string? jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
             string? jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            var URL = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/par";

            var formFields = new List<KeyValuePair<string?, string?>>();
            if (grantType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("grant_type", grantType));
            }
            if (clientId != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_id", clientId.ToLower()));
            }
            if (clientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion_type", clientAssertionType));
            }
            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ??
                // new ClientAssertion { Aud = URL }.Get()
                new PrivateKeyJwt2()
                {
                    CertificateFilename = jwtCertificateFilename,
                    CertificatePassword = jwtCertificatePassword,
                    Issuer = BaseTest.SOFTWAREPRODUCT_ID.ToLower(),
                    Audience = URL
                }.Generate()
            ));
            formFields.Add(new KeyValuePair<string?, string?>("request", new RequestObject { CdrArrangementId = cdrArrangementId }.Get()));
            var content = new FormUrlEncodedContent(formFields);

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            clientHandler.ClientCertificates.Add(new X509Certificate2(
                certificateFilename ?? throw new ArgumentNullException(nameof(certificateFilename)), 
                certificatePassword,
                X509KeyStorageFlags.Exportable));

            using var client = new HttpClient(clientHandler);

            var responseMessage = await client.PostAsync(URL, content);

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

        // static public async Task<string?> GetRequestURI(
        //     string? grantType = "client_credentials",
        //     string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
        //     string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
        //     string? cdrArrangementId = null
        // )
        // {
        //     var responseMessage = await SendRequest(
        //         grantType: grantType,
        //         clientId: clientId,
        //         clientAssertionType: clientAssertionType,
        //         cdrArrangementId: cdrArrangementId
        //     );

        //     if (responseMessage.StatusCode != HttpStatusCode.Created)
        //     {
        //         throw new Exception($"{nameof(GetRequestURI)} - Error getting RequestURI");
        //     }

        //     var parResponse = await DeserializeResponse(responseMessage);
        //     return parResponse?.RequestURI;
        // }
    }
}
