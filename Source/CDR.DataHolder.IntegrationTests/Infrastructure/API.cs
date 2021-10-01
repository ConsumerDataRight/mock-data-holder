using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#nullable enable 

namespace CDR.DataHolder.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Call API
    /// </summary>
    public class API
    {
        /// <summary>
        /// Filename of certificate to use. 
        /// If null then no certificate will be attached to the request.
        /// </summary>
        public string? CertificateFilename { get; init; }

        /// <summary>
        /// Password for certificate. 
        /// If null then no certificate password will be set.
        /// </summary>
        public string? CertificatePassword { get; init; }

        /// <summary>
        /// Access token.
        /// If null then no access token will be attached to the request.
        /// See the AccessToken class to generate an access token.
        /// </summary>
        public string? AccessToken { get; init; }

        /// <summary>
        /// The HttpMethod of the request.
        /// </summary>
        public HttpMethod? HttpMethod { get; init; }

        /// <summary>
        /// The URL of the request.
        /// </summary>
        public string? URL { get; init; }

        /// <summary>
        /// The x-v header.
        /// If null then no x-v header will be set.
        /// </summary>
        public string? XV { get; init; }

        /// <summary>
        /// The If-None-Match header (an ETag).
        /// If null then no If-None-Match header will be set.
        /// </summary>
        public string? IfNoneMatch { get; init; }

        /// <summary>
        /// The x_fapi_auth_date header.
        /// If null then no x_fapi_auth_date header will be set.
        /// </summary>
        public string? XFapiAuthDate { get; init; }

        /// <summary>
        /// The x-fapi-interaction-id header.
        /// If null then no x-fapi-interaction-id header will be set.
        /// </summary>
        public string? XFapiInteractionId { get; init; }

        /// <summary>
        /// Content
        /// If null then no content is set.
        /// </summary>
        public HttpContent? Content { get; init; }

        /// <summary>
        /// Content.Headers.ContentType
        /// If null then Content.Headers.ContentType is not set.
        /// </summary>
        public MediaTypeHeaderValue? ContentType { get; init; }

        /// <summary>
        /// Request.Headers.Accept
        /// If null then Request.Headers.Accept is not set.
        /// </summary>
        public string? Accept { get; init; }

        public IEnumerable<string>? Cookies { get; init; }

        /// <summary>
        /// Send a request to the API.
        /// </summary>
        /// <returns>The API response</returns>
        public async Task<HttpResponseMessage> SendAsync(bool AllowAutoRedirect = true)
        {
            // Build request
            HttpRequestMessage BuildRequest()
            {
                if (HttpMethod == null) { throw new Exception($"{nameof(API)}.{nameof(SendAsync)}.{nameof(BuildRequest)} - {nameof(HttpMethod)} not set"); }

                var request = new HttpRequestMessage(HttpMethod, URL);

                // Attach access token if provided
                if (AccessToken != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                }

                // Set x-v header if provided
                if (XV != null)
                {
                    request.Headers.Add("x-v", XV);
                }

                // Set If-None-Match header if provided
                if (IfNoneMatch != null)
                {
                    request.Headers.Add("If-None-Match", $"\"{IfNoneMatch}\"");
                }

                // Set x-fapi-auth-date header if provided
                if (XFapiAuthDate != null)
                {
                    request.Headers.Add("x-fapi-auth-date", XFapiAuthDate);
                }

                // Set x-fapi-interaction-id header if provided
                if (XFapiInteractionId != null)
                {
                    request.Headers.Add("x-fapi-interaction-id", XFapiInteractionId);
                }

                // Set content
                if (Content != null)
                {
                    request.Content = Content;

                    // Set content type
                    if (ContentType != null)
                    {
                        request.Content.Headers.ContentType = ContentType;
                    }
                }

                // Set request Accept header
                if (Accept != null)
                {
                    request.Headers.TryAddWithoutValidation("Accept", Accept);
                }

                return request;
            }

            // Send request and return response
            async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
            {
                HttpClient GetClient()
                {
                    var clientHandler = new HttpClientHandler
                    {
                        AllowAutoRedirect = AllowAutoRedirect,
                        // UseCookies = false
                    };

                    // Set cookies
                    if (Cookies != null)
                    {
                        // var cookieContainer = new CookieContainer();
                        // foreach (var cookie in Cookies)
                        // {
                        //     cookieContainer.Add(new Cookie());
                        // }
                        // clientHandler.CookieContainer = cookieContainer;

                        clientHandler.UseCookies = false;
                        request.Headers.Add("Cookie", Cookies);
                    }

                    // Attach client certificate
                    if (CertificateFilename != null)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                        clientHandler.ClientCertificates.Add(new X509Certificate2(
                            CertificateFilename,
                            CertificatePassword,
                            X509KeyStorageFlags.Exportable
                        ));
                    }

                    return new HttpClient(clientHandler);
                }

                using var client = GetClient();

                var response = await client.SendAsync(request);

                return response;
            }

            var request = BuildRequest();
            var response = await SendRequest(request);
            return response;
        }
    }
}
