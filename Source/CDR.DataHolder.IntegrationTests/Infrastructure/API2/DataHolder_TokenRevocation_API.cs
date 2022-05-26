using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_TokenRevocation_API
    {       
        // Send token request, returning HttpResponseMessage
        static public async Task<HttpResponseMessage> SendRequest(
            string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
            string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
            string? clientAssertion = null,
            string? token = null,
            string? tokenTypeHint = null,
            string? certificateFilename = BaseTest.CERTIFICATE_FILENAME,
            string? certificatePassword = BaseTest.CERTIFICATE_PASSWORD,
            string? jwt_certificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string? jwt_certificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD
        )
        {
            var URL = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/revocation";

            var formFields = new List<KeyValuePair<string?, string?>>();

            if (clientId != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_id", clientId.ToLower()));
            }

            if (clientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion_type", clientAssertionType));
            }

            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ?? 
                new PrivateKeyJwt2
                {
                    CertificateFilename = jwt_certificateFilename,
                    CertificatePassword = jwt_certificatePassword,
                    Issuer = (clientId ?? BaseTest.SOFTWAREPRODUCT_ID).ToLower(),
                    Audience = URL
                }.Generate())
            );

            if (token != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("token", token));
            }

            if (tokenTypeHint != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("token_type_hint", tokenTypeHint));
            }

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
    }
}
