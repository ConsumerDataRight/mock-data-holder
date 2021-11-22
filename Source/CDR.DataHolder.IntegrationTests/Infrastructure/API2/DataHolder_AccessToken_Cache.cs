using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static CDR.DataHolder.IntegrationTests.BaseTest;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
// TODO MJS 2021-10-21 - Not used in pipeline, remove
/*
#if DEBUG
    public class BROKEN_DataHolder_AccessToken_Cache_UnitTests : BaseTest, IClassFixture<BROKEN_DataHolder_AccessToken_Cache_UnitTests.Fixture>
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

        // TODO 13/08/2021 - Update MDH_InfosecProfileAPI_Token story. Refresh tokens don't work. 
        // Check with Ashley if this logic is correct:-
        //   1) call token api with refresh token to get back new access token & refresh token.
        //   2) repeat #1 (but with new refresh token from previous call to #1), should be able to do this practically forever (until CdrArrangement has expired)
        // The API works on first call but fails on subsequent calls with "invalid grant". In IDSVR persistedgrants table the refreshtoken has been deleted (hence the invalid grant since the refresh token no longer exists)
        [Fact]
        public async Task GetAccessToken_CacheStrategy_ShouldDoE2EAuthConsentFlowOn1stCall_AndHitCacheOnSubsequentCalls()
        {
            // Arrange
            var cache = new BROKEN_DataHolder_AccessToken_Cache();

            // Act/Assert
            const int MAX = 5;
            for (int i = 1; i <= MAX; i++)
            {
                var token = await cache.GetAccessToken(USERID_JANEWILSON, ACCOUNTIDS_ALL_JANE_WILSON, SCOPE);
                token.Should().NotBeNullOrEmpty();
            }

            // Assert
            cache.Misses.Should().Be(1);
            cache.Hits.Should().Be(MAX - 1);
        }
    }
#endif    
*/    

    /// <summary>
    /// Get access token from DataHolder.
    /// Cache request (user/selectedaccounts/scope) and refreshtoken.
    /// If cache miss then perform full E2E auth/consent flow to get accesstoken/refresh token and cache returned refresh token.
    /// If cache hit then use cached refreshtoken to get new accesstoken.
    /// </summary>
    public class BROKEN_DataHolder_AccessToken_Cache
    {
        public int Hits { get; private set; } = 0;
        public int Misses { get; private set; } = 0;

        class CacheItem
        {
            public string? UserId { get; init; }
            public string? Scope { get; init; }
            public string? RefreshToken { get; set; }
        }

        readonly List<CacheItem> cache = new();

        public async Task<string?> GetAccessToken(
            string userId,
            string selectedAccounts,
            string scope = BaseTest.SCOPE
            // bool expired = false,
            // string accountId = ""
            )
        {
            async Task<(string accessToken, string refreshToken)> FromAuthConsentFlow()
            {
                (var authCode, _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = userId,
                    OTP = BaseTest.AUTHORISE_OTP,
                    SelectedAccountIds = selectedAccounts,
                    Scope = scope
                }.Authorise();

                // use authcode to get access and refresh tokens
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

                if (tokenResponse?.AccessToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - access token is null");

                if (tokenResponse?.RefreshToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - refresh token is null");

                return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
            }

            async Task<(string accessToken, string refreshToken)> FromRefreshToken(string? refreshToken)
            {
                if (refreshToken == null)
                    throw new Exception($"{nameof(FromRefreshToken)} - refresh token is null");

                var tokenResponse = await DataHolder_Token_API.GetResponseUsingRefreshToken(refreshToken, scope: scope);

                if (tokenResponse?.AccessToken == null)
                    throw new Exception($"{nameof(FromRefreshToken)} - access token is null");
                if (tokenResponse?.RefreshToken == null)
                    throw new Exception($"{nameof(FromRefreshToken)} - refresh token is null");

                return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
            }

            // Find refresh token in cache
            var cacheHit = cache.Find(item => item.UserId == userId && item.Scope == scope);

            // Cache hit
            if (cacheHit != null)
            {
                Hits++;

                // Use refresh token from cache to get access token
                (var accessToken, var refreshToken) = await FromRefreshToken(cacheHit.RefreshToken);

                // Update refresh token in cache
                cacheHit.RefreshToken = refreshToken;

                // Return access token
                return accessToken;
            }
            // Cache miss, so perform auth/consent flow to get accesstoken/refreshtoken
            else
            {
                Misses++;

                (var accessToken, var refreshToken) = await FromAuthConsentFlow();

                // Add refresh token to cache
                cache.Add(new CacheItem { UserId = userId, Scope = scope, RefreshToken = refreshToken });

                // Return access token
                return accessToken;
            }
        }
    }

// TODO MJS 2021-10-21 - Not used in pipeline, remove
/*
#if DEBUG
    public class DataHolder_AccessToken_Cache_UnitTests : BaseTest, IClassFixture<DataHolder_AccessToken_Cache_UnitTests.Fixture>
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
        public async Task GetAccessToken_CacheStrategy_ShouldDoE2EAuthConsentFlowOn1stCall_AndHitCacheOnSubsequentCalls()
        {
            // Arrange
            var cache = new DataHolder_AccessToken_Cache();

            // Act/Assert
            const int MAX = 5;
            for (int i = 1; i <= MAX; i++)
            {
                var token = await cache.GetAccessToken(USERID_JANEWILSON, ACCOUNTIDS_ALL_JANE_WILSON, SCOPE);
                token.Should().NotBeNullOrEmpty();
                (await CallResourceAPI(token!)).StatusCode.Should().Be(HttpStatusCode.OK);  // check access token actually works
            }

            // Assert
            cache.Misses.Should().Be(1);
            cache.Hits.Should().Be(MAX - 1);
        }
    }
#endif      
*/  

    /// <summary>
    /// Get access token from DataHolder.
    /// Cache request (user/selectedaccounts/scope) and accesstoken.
    /// If cache miss then perform full E2E auth/consent flow to get accesstoken, cache it, and return access token.
    /// If cache hit then use cached access token.
    /// </summary>
    public class DataHolder_AccessToken_Cache
    {
        public int Hits { get; private set; } = 0;
        public int Misses { get; private set; } = 0;

        class CacheItem
        {
            public string? UserId { get; init; }
            public string? SelectedAccounts { get; init; }
            public string? Scope { get; init; }

            public string? AccessToken { get; set; }
        }

        readonly List<CacheItem> cache = new();

        public async Task<string?> GetAccessToken(TokenType tokenType, string scope = BaseTest.SCOPE)
        {
            // if (expired) { throw new NotImplementedException(); }

            switch (tokenType)
            {
                case TokenType.JANE_WILSON:
                case TokenType.STEVE_KENNEDY:
                case TokenType.DEWAYNE_STEVE:
                case TokenType.BUSINESS_1:
                case TokenType.BUSINESS_2:
                case TokenType.BEVERAGE:
                case TokenType.KAMILLA_SMITH:
                    {
                        return await GetAccessToken(tokenType.UserId(), tokenType.AllAccountIds(), scope);
                    }

                case TokenType.INVALID_FOO:
                    return "foo";
                case TokenType.INVALID_EMPTY:
                    return "";
                case TokenType.INVALID_OMIT:
                    return null;

                default:
                    throw new ArgumentException($"{nameof(tokenType)} = {tokenType}");
            }
        }

        public async Task<string?> GetAccessToken(
            string userId,
            string selectedAccounts,
            string scope = BaseTest.SCOPE
            // bool expired = false,
            // string accountId = ""
            )
        {
            async Task<(string accessToken, string refreshToken)> FromAuthConsentFlow()
            {
                (var authCode, _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = userId,
                    OTP = BaseTest.AUTHORISE_OTP,
                    SelectedAccountIds = selectedAccounts,
                    Scope = scope
                }.Authorise();

                // use authcode to get access and refresh tokens
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

                if (tokenResponse?.AccessToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - access token is null");

                if (tokenResponse?.RefreshToken == null)
                    throw new Exception($"{nameof(FromAuthConsentFlow)} - refresh token is null");

                return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
            }

            // Find refresh token in cache
            var cacheHit = cache.Find(item =>
                item.UserId == userId &&
                item.SelectedAccounts == selectedAccounts &&
                item.Scope == scope);

            // Cache hit
            if (cacheHit != null)
            {
                Hits++;

                return cacheHit.AccessToken;
            }
            // Cache miss, so perform auth/consent flow to get accesstoken/refreshtoken
            else
            {
                Misses++;

                (var accessToken, _) = await FromAuthConsentFlow();

                // Add refresh token to cache
                cache.Add(new CacheItem
                {
                    UserId = userId,
                    SelectedAccounts = selectedAccounts,
                    Scope = scope,
                    AccessToken = accessToken
                });

                // Return access token
                return accessToken;
            }
        }
    }
}
