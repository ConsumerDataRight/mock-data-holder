using IdentityServer4.Models;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class DataReceipientClient : Client, IClient
    {
        public DataReceipientClient()
        {
            // Configuration properties.
            Enabled = true;
            IdentityTokenLifetime = TimingsInSeconds.FiveMinutes;
            AbsoluteRefreshTokenLifetime = TimingsInSeconds.OneYear;
            RefreshTokenExpiration = TokenExpiration.Absolute;
            RefreshTokenUsage = TokenUsage.ReUse;
            AccessTokenType = AccessTokenType.Jwt;
            AuthorizationCodeLifetime = TimingsInSeconds.TenMinutes;
            AlwaysIncludeUserClaimsInIdToken = true;
            FrontChannelLogoutSessionRequired = false;
            BackChannelLogoutSessionRequired = false;
            RequireClientSecret = true;
            RequireConsent = true;
            PairWiseSubjectSalt = Pairwise.Salt;
            AllowOfflineAccess = true;

            // AllowedGrantTypes is always Hybrid as per CDS standard. Also to enable using some of the IDS4 inbuilt functionalities
            AllowedGrantTypes = IdentityServer4.Models.GrantTypes.HybridAndClientCredentials;
            AllowedGrantTypes.Add(CdsConstants.GrantTypes.RefreshToken);
        }

    }
}