using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using static CDR.DataHolder.IntegrationTests.BaseTest;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
#if DEBUG
    public class DataHolder_Token_UnitTests : IClassFixture<DataHolder_Token_UnitTests.Fixture>
    {
        class Fixture : IAsyncLifetime
        {
            public async Task InitializeAsync()
            {
                TestSetup.Register_PatchRedirectUri();
                TestSetup.DataHolder_PurgeIdentityServer();
                await TestSetup.DataHolder_RegisterSoftwareProduct();
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }
        }

        private static async Task<HttpResponseMessage> CallResourceAPI(string accessToken)
        {
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            return response;
        }

        [Fact]
        public async Task Test_E2E()
        {
            using (new AssertionScope())
            {
                var e2e = new DataHolder_Token_E2E();

                // Get token using Auth/Consent flow
                var token1 = await e2e.GetToken_UsingAuthConsent(USERID_JANEWILSON, ACCOUNTIDS_ALL_JANE_WILSON, SCOPE);
                token1.AccessToken.Should().NotBeNull();
                token1.RefreshToken.Should().NotBeNull();

                // Call resource API using access token
                (await CallResourceAPI(token1.AccessToken!)).StatusCode.Should().Be(HttpStatusCode.OK);
                // (await CallResourceAPI(token1.AccessToken!)).StatusCode.Should().Be(HttpStatusCode.OK);
                // (await CallResourceAPI(token1.AccessToken!)).StatusCode.Should().Be(HttpStatusCode.OK);

                // Get another token using refresh token
                var token2 = await e2e.GetToken_UsingRefreshToken(token1.RefreshToken!);
                token2.AccessToken.Should().NotBeNull();
                token2.RefreshToken.Should().NotBeNull();

                // Call resource API using access token
                (await CallResourceAPI(token2.AccessToken!)).StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task Test_Mocked()
        {
            using (new AssertionScope())
            {
                var mocked = new DataHolder_Token_Mocked();

                // Get token using Auth/Consent flow
                var token1 = await mocked.GetToken_UsingAuthConsent(USERID_JANEWILSON, ACCOUNTIDS_ALL_JANE_WILSON, SCOPE);
                token1.AccessToken.Should().NotBeNull();
                token1.RefreshToken.Should().NotBeNull();

                // Call resource API using access token
                (await CallResourceAPI(token1.AccessToken!)).StatusCode.Should().Be(HttpStatusCode.OK);

                // Get another token using refresh token
                var token2 = await mocked.GetToken_UsingRefreshToken(token1.RefreshToken!);
                token2.AccessToken.Should().NotBeNull();
                token2.RefreshToken.Should().NotBeNull();

                // Call resource API using access token
                (await CallResourceAPI(token2.AccessToken!)).StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
#endif

    public class DataHolder_Token_Response
    {
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
    }

    public abstract class DataHolder_Token_Base
    {
        // Get an access token for user by using auth/consent flow
        public abstract Task<DataHolder_Token_Response> GetToken_UsingAuthConsent(string userId, string selectedAccountIds, string scope);

        // Get an access token for user by using a refresh token
        public abstract Task<DataHolder_Token_Response> GetToken_UsingRefreshToken(string refreshToken);
    }

    /// <summary>
    /// Get tokens using E2E auth/consent flow
    /// </summary>
    public class DataHolder_Token_E2E : DataHolder_Token_Base
    {
        public override async Task<DataHolder_Token_Response> GetToken_UsingAuthConsent(string userId, string selectedAccountIds, string scope)
        {
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = userId,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = selectedAccountIds,
                Scope = scope
            }.Authorise();

            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null) { throw new Exception($"{nameof(GetToken_UsingAuthConsent)} - error getting token"); }

            return new DataHolder_Token_Response
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken
            };
        }

        public override async Task<DataHolder_Token_Response> GetToken_UsingRefreshToken(string refreshToken)
        {
            var tokenResponse = await DataHolder_Token_API.GetResponseUsingRefreshToken(refreshToken /*, scope: scope*/);
            if (tokenResponse == null) { throw new Exception($"{nameof(GetToken_UsingRefreshToken)} - error getting token"); }

            return new DataHolder_Token_Response
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken
            };
        }
    }

    /// <summary>
    /// Get tokens using a mocked auth/consent flow.
    /// Rows are add directly to PersistedGrants table in the DH idsvr database
    /// </summary>
    public class DataHolder_Token_Mocked : DataHolder_Token_Base
    {
        public override async Task<DataHolder_Token_Response> GetToken_UsingAuthConsent(string userId, string selectedAccountIds, string scope)
        {
            (var authCode, _) = DataHolder_Authorise_API.Authorise(userId, scope: scope, accountIds: selectedAccountIds.Split(","));

            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null) { throw new Exception($"{nameof(GetToken_UsingAuthConsent)} - error getting token"); }

            return new DataHolder_Token_Response
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken
            };
        }

        public override async Task<DataHolder_Token_Response> GetToken_UsingRefreshToken(string refreshToken)
        {
            var tokenResponse = await DataHolder_Token_API.GetResponseUsingRefreshToken(refreshToken /*, scope: scope*/);
            if (tokenResponse == null) { throw new Exception($"{nameof(GetToken_UsingRefreshToken)} - error getting token"); }

            return new DataHolder_Token_Response
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken
            };
        }
    }
}