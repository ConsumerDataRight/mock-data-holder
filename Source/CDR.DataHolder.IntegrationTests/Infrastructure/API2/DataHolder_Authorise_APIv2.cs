using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using Microsoft.Playwright;
using FluentAssertions;
using CDR.DataHolder.IntegrationTests.Models;
using System.Linq;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class EDataHolder_Authorise_IncorrectCustomerId : Exception { }
    public class EDataHolder_Authorise_IncorrectOneTimePassword : Exception { }

    public class DataHolder_Authorise_APIv2
    {
        public string? UserId { get; init; }
        public string? OTP { get; init; }
        public string? SelectedAccountIds { get; init; }
        protected string[]? SelectedAccountIdsArray => SelectedAccountIds?.Split(",");

        private string[]? _selectedAccountDisplayNames = null;
        protected string[]? SelectedAccountDisplayNames
        {
            get
            {
                if (_selectedAccountDisplayNames == null)
                {
                    List<string> list = new();

                    if (SelectedAccountIdsArray != null)
                    {
                        using var connection = new SqlConnection(BaseTest.DATAHOLDER_CONNECTIONSTRING);
                        foreach (var accountId in SelectedAccountIdsArray)
                        {
                            var displayName = connection.QuerySingle<string>("select displayName from account where accountId = @AccountId", new { AccountId = accountId });
                            list.Add(displayName);
                        }
                    }
                    _selectedAccountDisplayNames = list.ToArray();
                }

                return _selectedAccountDisplayNames;
            }
        }

        public string Scope { get; init; } = BaseTest.SCOPE;
        public int TokenLifetime { get; init; } = 3600;
        public int SharingDuration { get; init; } = BaseTest.SHARING_DURATION;
        public string? RequestUri { get; init; }
        public string CertificateFilename { get; init; } = BaseTest.CERTIFICATE_FILENAME;
        public string CertificatePassword { get; init; } = BaseTest.CERTIFICATE_PASSWORD;
        public string ClientId { get; init; } = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID).ToLower();
        public string RedirectURI { get; init; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
        public string JwtCertificateFilename { get; init; } = BaseTest.JWT_CERTIFICATE_FILENAME;
        public string JwtCertificatePassword { get; init; } = BaseTest.JWT_CERTIFICATE_PASSWORD;

        /// <summary>
        /// Perform authorisation and consent flow. Returns authCode and idToken
        /// </summary>
        public async Task<(string authCode, string idToken)> Authorise(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS)
        {
            redirectUrl = BaseTest.SubstituteConstant(redirectUrl);

            const string RESPONSETYPE = "code id_token";
            const string RESPONSEMODE = "form_post";
            Uri authRedirectUri = await Authorise_GetRedirectUri(RESPONSETYPE, RESPONSEMODE, redirectUrl);

            return await Authorize_Consent(authRedirectUri, RESPONSEMODE);
        }

        // Call authorise endpoint, should respond with a redirect to auth UI, return the redirect URI
        private async Task<Uri> Authorise_GetRedirectUri(string responseType, string responseMode, string redirectURI)
        {
            var clientId = IntegrationTests.BaseTest.GetClientId(IntegrationTests.BaseTest.SOFTWAREPRODUCT_ID);

            var queryString = new Dictionary<string, string?>
            {
                { "request_uri", RequestUri },
                { "response_type", responseType },
                { "response_mode", responseMode },
                { "client_id", clientId },
                { "redirect_uri", redirectURI },
                { "scope", IntegrationTests.BaseTest.SCOPE },
            };

            var api = new IntegrationTests.Infrastructure.API
            {
                CertificateFilename = IntegrationTests.BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = IntegrationTests.BaseTest.CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = QueryHelpers.AddQueryString($"{IntegrationTests.BaseTest.DH_TLS_AUTHSERVER_BASE_URL}/connect/authorize", queryString),
            };

            var response = await api.SendAsync(AllowAutoRedirect: false);

            var redirectlocation = response.Headers.Location;

            if (response.StatusCode != HttpStatusCode.Redirect)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(content);
                var error = doc.DocumentNode.SelectSingleNode("//input[@name='error']").Attributes["value"].Value;
                var errorDescription = doc.DocumentNode.SelectSingleNode("//input[@name='error_description']").Attributes["value"].Value;

                throw new AuthoriseException("Expected {HttpStatusCode.Redirect} but {response.StatusCode}", response.StatusCode, error, errorDescription);
            }

            return response.Headers.Location ?? throw new NullReferenceException(nameof(response.Headers.Location.AbsoluteUri));
        }


        private async Task<(string authCode, string idToken)> Authorize_Consent(Uri authRedirectUri, string responseMode)
        {
            var authRedirectLeftPart = authRedirectUri.GetLeftPart(UriPartial.Authority) + "/ui";

            string? code = null;
            string? idtoken = null;

            await PlaywrightHelper2.Execute(async (page) =>
            {
                // Perform consent flow            
                await page.GotoAsync(authRedirectUri.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                await page.Locator("h6:has-text(\"Mock Data Holder Banking\")").TextContentAsync();
                await page.Locator("h5:has-text(\"Login\")").TextContentAsync();

                // Username
                await page.Locator("input[type=\"text\"]").FillAsync(UserId ?? throw new NullReferenceException(nameof(UserId)));
                await page.Locator("[aria-label=\"continue\"]").ClickAsync();

                // OTP
                await Task.Delay(3000);

                await page.Locator("input[type=\"text\"]").FillAsync(OTP ?? throw new NullReferenceException(nameof(OTP)));
                await page.Locator("text=Continue").ClickAsync();

                // Select accounts
                await page.WaitForURLAsync($"{authRedirectLeftPart}/select-accounts");

                if (SelectedAccountDisplayNames != null)
                {
                    foreach (string displayName in SelectedAccountDisplayNames)
                    {
                        await page.Locator($"li >> id=account-{displayName}").ClickAsync();
                    }
                }
                await page.Locator("text=Continue").ClickAsync();

                // Confirmation - Click authorise and check callback response
                await page.WaitForURLAsync($"{authRedirectLeftPart}/confirmation");

                (code, idtoken) = await HybridFlow_HandleCallback(redirectUri: RedirectURI, responseMode: responseMode, page: page, setup: async (page) =>
                {
                    await page.Locator("text=Authorise").ClickAsync();
                });
            });

            return (
                authCode: code ?? throw new NullReferenceException(nameof(code)),
                idToken: idtoken ?? throw new NullReferenceException(nameof(idtoken))
            );
        }

        private delegate Task HybridFlow_HandleCallback_Setup(IPage page);
        static private async Task<(string code, string idtoken)> HybridFlow_HandleCallback(string redirectUri, string responseMode, IPage page, HybridFlow_HandleCallback_Setup setup)
        {
            var callback = new IntegrationTests.Infrastructure.API2.DataRecipientConsentCallback(redirectUri);
            callback.Start();
            try
            {
                await setup(page);

                var callbackRequest = await callback.WaitForCallback();
                switch (responseMode)
                {
                    case "form_post":
                        {
                            callbackRequest.Should().NotBeNull();
                            callbackRequest?.received.Should().BeTrue();
                            callbackRequest?.method.Should().Be(HttpMethod.Post);
                            callbackRequest?.body.Should().NotBeNullOrEmpty();

                            var body = QueryHelpers.ParseQuery(callbackRequest?.body);
                            var code = body["code"];
                            var id_token = body["id_token"];
                            return (code, id_token);
                        }
                    case "fragment":
                    case "query":
                    default:
                        throw new NotSupportedException(nameof(responseMode));
                }
            }
            finally
            {
                await callback.Stop();
            }
        }
    }
}
