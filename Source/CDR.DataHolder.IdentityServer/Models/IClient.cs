using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Models
{
    public interface IClient
    {
        // Standard properties.
        string ClientId { get; }

        string ClientName { get; }

        string Description { get; }

        string ClientUri { get; }

        string LogoUri { get; }

        bool Enabled { get; }

        int IdentityTokenLifetime { get; }

        int AbsoluteRefreshTokenLifetime { get; }

        TokenUsage RefreshTokenUsage { get; }

        int AuthorizationCodeLifetime { get; }

        bool AlwaysIncludeUserClaimsInIdToken { get; }

        bool RequireConsent { get; }

        ICollection<string> AllowedGrantTypes { get; }

        ICollection<string> AllowedScopes { get; }

        ICollection<string> RedirectUris { get; }

        ICollection<Secret> ClientSecrets { get; }

        ICollection<ClientClaim> Claims { get; }

        bool RequireClientSecret { get; }
    }
}