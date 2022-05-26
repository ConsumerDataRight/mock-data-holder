using System;
using System.Threading.Tasks;
using static CDR.DataHolder.IntegrationTests.BaseTest;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
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