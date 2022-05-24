using System.Collections.Generic;
using CDR.DataHolder.IdentityServer.Extensions;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.Extensions.Configuration;
using Is4Models = IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Helpers
{
    public static class ConvertIdentityServerModelToEntityHelper
    {
        public static Client ConvertModelClientToEntityClient(Is4Models.Client modelClient, IConfiguration configuration)
        {
            var clientCorsOrigin = new List<ClientCorsOrigin>();
            foreach (var item in modelClient.AllowedCorsOrigins)
            {
                clientCorsOrigin.Add(new ClientCorsOrigin() { Origin = item });
            }

            var allowdGrantTypes = new List<ClientGrantType>();
            foreach (var item in modelClient.AllowedGrantTypes)
            {
                allowdGrantTypes.Add(new ClientGrantType() { GrantType = item });
            }

            var allowedScopes = new List<ClientScope>();
            foreach (var item in modelClient.AllowedScopes)
            {
                allowedScopes.Add(new ClientScope() { Scope = item });
            }

            var clientClaims = new List<ClientClaim>();
            foreach (var item in modelClient.Claims)
            {
                clientClaims.Add(new ClientClaim() { Type = item.Type, Value = item.Value });
            }

            var clientSecrets = new List<ClientSecret>();
            foreach (var item in modelClient.ClientSecrets)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    clientSecrets.Add(new ClientSecret() { Type = item.Type, Value = item.Value, Description = item.Description, Expiration = item.Expiration });
                }
            }

            var clientIdPRestrictions = new List<ClientIdPRestriction>();
            foreach (var item in modelClient.IdentityProviderRestrictions)
            {
                clientIdPRestrictions.Add(new ClientIdPRestriction() { Provider = item });
            }

            var clientPostLogoutRedirectUri = new List<ClientPostLogoutRedirectUri>();
            foreach (var item in modelClient.IdentityProviderRestrictions)
            {
                clientPostLogoutRedirectUri.Add(new ClientPostLogoutRedirectUri() { PostLogoutRedirectUri = item });
            }

            var clientProperties = new List<ClientProperty>();
            foreach (var item in modelClient.Properties)
            {
                clientProperties.Add(new ClientProperty() { Key = item.Key, Value = item.Value });
            }

            var clientRedirectUris = new List<ClientRedirectUri>();
            foreach (var item in modelClient.RedirectUris)
            {
                clientRedirectUris.Add(new ClientRedirectUri() { RedirectUri = item });
            }

            var efClient = new Client()
            {
                AbsoluteRefreshTokenLifetime = modelClient.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = modelClient.AccessTokenLifetime,
                AccessTokenType = (int)modelClient.AccessTokenType,
                AllowAccessTokensViaBrowser = modelClient.AllowAccessTokensViaBrowser,
                AllowedCorsOrigins = clientCorsOrigin,
                AllowedGrantTypes = allowdGrantTypes,
                AllowedScopes = allowedScopes,
                AllowOfflineAccess = modelClient.AllowOfflineAccess,
                AllowPlainTextPkce = modelClient.AllowPlainTextPkce,
                AllowRememberConsent = modelClient.AllowRememberConsent,
                AlwaysIncludeUserClaimsInIdToken = modelClient.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = modelClient.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = modelClient.AuthorizationCodeLifetime,
                BackChannelLogoutSessionRequired = modelClient.BackChannelLogoutSessionRequired,
                BackChannelLogoutUri = modelClient.BackChannelLogoutUri,
                Claims = clientClaims,
                ClientClaimsPrefix = modelClient.ClientClaimsPrefix,
                ClientId = modelClient.ClientId,
                ClientName = modelClient.ClientName,
                ClientSecrets = clientSecrets,
                ClientUri = modelClient.ClientUri,
                ConsentLifetime = modelClient.ConsentLifetime,
                Description = modelClient.Description,
                DeviceCodeLifetime = modelClient.DeviceCodeLifetime,
                Enabled = modelClient.Enabled,
                EnableLocalLogin = modelClient.EnableLocalLogin,
                FrontChannelLogoutSessionRequired = modelClient.FrontChannelLogoutSessionRequired,
                FrontChannelLogoutUri = modelClient.FrontChannelLogoutUri,
                IdentityProviderRestrictions = clientIdPRestrictions,
                IdentityTokenLifetime = modelClient.IdentityTokenLifetime,
                IncludeJwtId = modelClient.IncludeJwtId,
                LogoUri = modelClient.LogoUri,
                PairWiseSubjectSalt = modelClient.PairWiseSubjectSalt,
                PostLogoutRedirectUris = clientPostLogoutRedirectUri,
                Properties = clientProperties,
                ProtocolType = modelClient.ProtocolType,
                RedirectUris = clientRedirectUris,
                RefreshTokenExpiration = (int)modelClient.RefreshTokenExpiration,
                RefreshTokenUsage = (int)modelClient.RefreshTokenUsage,
                RequireClientSecret = modelClient.RequireClientSecret,
                RequireConsent = modelClient.RequireConsent,
                RequirePkce = configuration.FapiComplianceLevel() >= CdsConstants.FapiComplianceLevel.Fapi1Phase2,
                SlidingRefreshTokenLifetime = modelClient.SlidingRefreshTokenLifetime,
                UpdateAccessTokenClaimsOnRefresh = modelClient.UpdateAccessTokenClaimsOnRefresh,
                UserCodeType = modelClient.UserCodeType,
                UserSsoLifetime = modelClient.UserSsoLifetime,
            };

            return efClient;
        }
    }
}
