using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using CDR.DataHolder.IdentityServer.Extensions;

namespace CDR.DataHolder.IdentityServer.AutoMapper
{
    public class RegistrationProfile : Profile
    {
        public RegistrationProfile()
        {
            CreateMap<IClientRegistrationRequest, ClientRegistrationResponse>()
                .ForMember(x => x.RedirectUris, x => x.Ignore())
                .ForMember(x => x.ClientId, x => x.MapFrom(s => s.ClientId.HasValue ? s.ClientId.Value : Guid.NewGuid()))
                .AfterMap((request, response, context) => context.Mapper.Map(request.SoftwareStatement, response));

            CreateMap<SoftwareStatement, ClientRegistrationResponse>()
                .ForMember(x => x.ClientIdIssuedAt, x => x.MapFrom(_ => DateTime.UtcNow.ToEpoch()));

            CreateMap<ClientRegistrationResponse, DataReceipientClient>()
                .ForMember(x => x.AllowedScopes, x => x.MapFrom(r => r.Scope.Split(" ", StringSplitOptions.None)))
                .AfterMap((src, dest) =>
                 {
                     // Ensure registered client contains openid and profile scope as it's required for authorize endpoint
                     if (!dest.AllowedScopes.Contains(StandardScopes.OpenId))
                     {
                         dest.AllowedScopes.Add(StandardScopes.OpenId);
                     }

                     if (!dest.AllowedScopes.Contains(StandardScopes.Profile))
                     {
                         dest.AllowedScopes.Add(StandardScopes.Profile);
                     }
                 })
                .ForMember(x => x.Claims, x => x.MapFrom(r => r))
                .ForMember(x => x.Description, x => x.MapFrom(r => r.ClientDescription))
                .ForMember(x => x.ClientSecrets, x => x.MapFrom(r => r));

            CreateMap<Client, DataReceipientClient>();

            CreateMap<DataReceipientClient, ClientRegistrationResponse>()
                .ForMember(d => d.ApplicationType, src => src.MapFrom(s => "web"))
                .ForMember(d => d.ClientDescription, src => src.MapFrom(s => s.Description))
                .ForMember(d => d.ClientId, src => src.MapFrom(s => s.ClientId))
                .ForMember(d => d.ClientIdIssuedAt, src => src.MapFrom(s => s.Claims.Get("client_id_issued_at")))
                .ForMember(d => d.ClientName, src => src.MapFrom(s => s.ClientName))
                .ForMember(d => d.ClientUri, src => src.MapFrom(s => s.ClientUri))
                .ForMember(d => d.IdTokenEncryptedResponseAlg, src => src.MapFrom(s => s.Claims.Get("id_token_encrypted_response_alg")))
                .ForMember(d => d.IdTokenEncryptedResponseEnc, src => src.MapFrom(s => s.Claims.Get("id_token_encrypted_response_enc")))
                .ForMember(d => d.IdTokenSignedResponseAlg, src => src.MapFrom(s => s.Claims.Get("id_token_signed_response_alg")))
                .ForMember(d => d.JwksUri, src => src.MapFrom(s => s.Claims.Get("jwks_uri")))
                .ForMember(d => d.LogoUri, src => src.MapFrom(s => s.Claims.Get("logo_uri")))
                .ForMember(d => d.LegalEntityId, src => src.MapFrom(s => s.Claims.Get("legal_entity_id")))
                .ForMember(d => d.LegalEntityName, src => src.MapFrom(s => s.Claims.Get("legal_entity_name")))
                .ForMember(d => d.OrgId, src => src.MapFrom(s => s.Claims.Get("org_id")))
                .ForMember(d => d.OrgName, src => src.MapFrom(s => s.Claims.Get("org_name")))
                .ForMember(d => d.PolicyUri, src => src.MapFrom(s => s.Claims.Get("policy_uri")))
                .ForMember(d => d.RecipientBaseUri, src => src.MapFrom(s => s.Claims.Get("recipient_base_uri")))
                .ForMember(d => d.RedirectUris, src => src.MapFrom(s => s.RedirectUris))
                .ForMember(d => d.RequestObjectSigningAlg, src => src.MapFrom(s => s.Claims.Get("request_object_signing_alg")))
                .ForMember(d => d.ResponseTypes, src => src.MapFrom(s => new List<string> { "code id_token" }))
                .ForMember(d => d.RevocationUri, src => src.MapFrom(s => s.Claims.Get("revocation_uri")))
                .ForMember(d => d.SectorIdentifierUri, src => src.MapFrom(s => s.Claims.Get("sector_identifier_uri")))
                .ForMember(d => d.Scope, src => src.MapFrom(s => string.Join(' ', s.AllowedScopes)))
                .ForMember(d => d.SoftwareId, src => src.MapFrom(s => s.Claims.Get("software_id")))
                .ForMember(d => d.SoftwareStatementJwt, src => src.MapFrom(s => s.Claims.Get("software_statement")))
                .ForMember(d => d.TokenEndpointAuthMethod, src => src.MapFrom(src => "private_key_jwt"))
                .ForMember(d => d.TokenEndpointAuthSigningAlg, src => src.MapFrom(s => s.Claims.Get("token_endpoint_auth_signing_alg")))
                .ForMember(d => d.TosUri, src => src.MapFrom(s => s.Claims.Get("tos_uri")))
                .AfterMap((src, dest) =>
                {
                    // Replace IDSVR specific grant types with standard grant types.
                    var idsvrGrants = string.Join(' ', src.AllowedGrantTypes);
                    idsvrGrants = idsvrGrants.Replace(CdsConstants.GrantTypes.Hybrid, CdsConstants.GrantTypes.AuthorizationCode);
                    dest.GrantTypes = idsvrGrants.Split(' ');
                });

            CreateMap<ClientRegistrationResponse, ICollection<Secret>>()
                .ConstructUsing((response, _) => new Secret[]
                {
                    new Secret { Type = SecretTypes.JsonWebKey, Value = response.SigningJwk, Description = SecretDescription.Signing },
                    new Secret { Type = SecretTypes.JsonWebKey, Value = response.EncryptionJwk, Description = SecretDescription.Encyption },
                });

            CreateMap<ClientRegistrationResponse, ICollection<ClientClaim>>()
                .ConstructUsing((response, _) =>
                {
                    var claims = new List<ClientClaim>();

                    if (response.ClientIdIssuedAt > 0)
                    {
                        claims.Add(new ClientClaim(ClientMetadata.ClientIdIssuedAt, response.ClientIdIssuedAt.ToString()));
                    }

                    if (response.ApplicationType.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.ApplicationType, response.ApplicationType));
                    }

                    claims.Add(new ClientClaim(ClientMetadata.SoftwareId, response.SoftwareId));
                    claims.Add(new ClientClaim(ClientMetadata.SoftwareStatement, response.SoftwareStatementJwt));
                    claims.Add(new ClientClaim(ClientMetadata.LogoUri, response.LogoUri));

                    if (response.PolicyUri.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.PolicyUri, response.PolicyUri));
                    }

                    if (response.TosUri.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.TosUri, response.TosUri));
                    }

                    claims.Add(new ClientClaim(ClientMetadata.JwksUri, response.JwksUri));
                    claims.Add(new ClientClaim(ClientMetadata.TokenEndpointAuthenticationMethod, response.TokenEndpointAuthMethod));
                    claims.Add(new ClientClaim(ClientMetadata.TokenEndpointAuthenticationSigningAlgorithm, response.TokenEndpointAuthSigningAlg));
                    claims.Add(new ClientClaim(ClientMetadata.IdentityTokenEncryptedResponseAlgorithm, response.IdTokenEncryptedResponseAlg));
                    claims.Add(new ClientClaim(ClientMetadata.IdentityTokenEncryptedResponseEncryption, response.IdTokenEncryptedResponseEnc));
                    claims.Add(new ClientClaim(ClientMetadata.IdentityTokenSignedResponseAlgorithm, response.IdTokenSignedResponseAlg));

                    if (response.RequestObjectSigningAlg.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.RequestObjectSigningAlgorithm, response.RequestObjectSigningAlg));
                    }

                    if (response.LegalEntityId.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.LegalEntityId, response.LegalEntityId));
                    }

                    if (response.LegalEntityName.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.LegalEntityName, response.LegalEntityName));
                    }

                    if (response.RecipientBaseUri.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.RecipientBaseUri, response.RecipientBaseUri));
                    }

                    claims.Add(new ClientClaim(ClientMetadata.OrgId, response.OrgId));
                    claims.Add(new ClientClaim(ClientMetadata.OrgName, response.OrgName));
                    claims.Add(new ClientClaim(ClientMetadata.RevocationUri, response.RevocationUri));

                    if (response.SectorIdentifierUri.IsPresent())
                    {
                        claims.Add(new ClientClaim(ClientMetadata.SectorIdentifierUri, response.SectorIdentifierUri));
                    }
                    else
                    {
                        // Use the hostname of the 'redirect_uris' if SectorIdentifierUri is not present.
                        claims.Add(new ClientClaim(ClientMetadata.SectorIdentifierUri, new Uri(response.RedirectUris.First()).Host));
                    }

                    return claims;
                });
        }
    }

    internal static class ClaimExtensions
    {
        public static string Get(this ICollection<Claim> claims, string type)
        {
            return claims.SingleOrDefault(x => x.Type == type)?.Value;
        }
    }

}
