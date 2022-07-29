using IdentityModel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;

namespace CDR.DataHolder.IdentityServer
{
    public static class CdsConstants
    {
        public static class TimingsInSeconds
        {
            public const int OneYear = 31536000;
            public const int TenMinutes = 600;
            public const int FiveMinutes = 300;
        }

        public static class PushedAuthorizationServiceErrorCodes
        {
            public const string UnauthorizedClient = "unauthorized_client";
            public const string InvalidCdrArrangementId = "invalid_cdr_arrangement_id";
            public const string RequestJwtFailedValidation = "request_jwt_failed_validation";
        }

        public static readonly ImmutableList<string> PushedAuthorizationResponseErrorCodes = new List<string>
        {
            AuthorizeErrorCodes.UnauthorizedClient,
            AuthorizeErrorCodes.InvalidRequest,
            AuthorizeErrorCodes.InvalidRequestObject,
            AuthorizeErrorCodes.UnsupportedResponseType,
        }.ToImmutableList();

        public static class AuthServerEndPoints
        {
            public const string Arrangements = "/connect/arrangements";

            public const string PushAuthoriseRequest = "/connect/par";

            public const string Authorize = "/connect/authorize";

            public const string AuthorizeRequestUri = "/connect/authorizerequesturi";

            public static class Path
            {
                public const string RequestUri = "request_uri";
            }
        }

        public static class AdrArrangementEndPoints
        {
            public const string Revocation = "/arrangements/revoke";
        }

        public static class AuthorizeRequest
        {
            public const string RequestUriPrefix = "urn:mdh:";

            public const string Scope = "scope";
            public const string CodeChallengeMethod = "code_challenge_method";
            public const string CodeChallenge = "code_challenge";
            public const string AcrValues = "acr_values";
            public const string LoginHint = "login_hint";
            public const string IdTokenHint = "id_token_hint";
            public const string Display = "display";
            public const string Nonce = "nonce";
            public const string ResponseMode = "response_mode";
            public const string State = "state";
            public const string RedirectUri = "redirect_uri";
            public const string ClientId = "client_id";
            public const string ResponseType = "response_type";
            public const string Request = "request";
            public const string Claims = "claims";
            public const string RequestUri = "request_uri";
            public const string TokenTypeHint = "token_type_hint";
        }

        public static class Discovery
        {
            public const string Issuer = "issuer";
            public const string ClaimsParameterSupported = "claims_parameter_supported";
            public const string ClaimTypesSupported = "claim_types_supported";
            public const string DisplayValuesSupported = "display_values_supported";
            public const string AcrValuesSupported = "acr_values_supported";
            public const string IdTokenEncryptionAlgorithmsSupported = "id_token_encryption_alg_values_supported";
            public const string IdTokenEncryptionEncValuesSupported = "id_token_encryption_enc_values_supported";
            public const string IdTokenSigningAlgorithmsSupported = "id_token_signing_alg_values_supported";
            public const string OpPolicyUri = "op_policy_uri";
            public const string OpTosUri = "op_tos_uri";
            public const string ClaimsLocalesSupported = "claims_locales_supported";
            public const string RequestObjectEncryptionAlgorithmsSupported = "request_object_encryption_alg_values_supported";
            public const string RequestObjectSigningAlgorithmsSupported = "request_object_signing_alg_values_supported";
            public const string RequestParameterSupported = "request_parameter_supported";
            public const string RequestUriParameterSupported = "request_uri_parameter_supported";
            public const string RequireRequestUriRegistration = "require_request_uri_registration";
            public const string ServiceDocumentation = "service_documentation";
            public const string TokenEndpointAuthSigningAlgorithmsSupported = "token_endpoint_auth_signing_alg_values_supported";
            public const string UILocalesSupported = "ui_locales_supported";
            public const string UserInfoEncryptionAlgorithmsSupported = "userinfo_encryption_alg_values_supported";
            public const string UserInfoEncryptionEncValuesSupported = "userinfo_encryption_enc_values_supported";
            public const string RequestObjectEncryptionEncValuesSupported = "request_object_encryption_enc_values_supported";
            public const string TokenEndpointAuthenticationMethodsSupported = "token_endpoint_auth_methods_supported";
            public const string ClaimsSupported = "claims_supported";
            public const string ResponseTypesSupported = "response_types_supported";
            public const string AuthorizationEndpoint = "authorization_endpoint";
            public const string DeviceAuthorizationEndpoint = "device_authorization_endpoint";
            public const string TokenEndpoint = "token_endpoint";
            public const string UserInfoEndpoint = "userinfo_endpoint";
            public const string AuthorizationEndpointOverride = "authorization_endpoint_override";
            public const string UserInfoEndpointOverride = "userinfo_endpoint_override";
            public const string IntrospectionEndpoint = "introspection_endpoint";
            public const string RevocationEndpoint = "revocation_endpoint";
            public const string DiscoveryEndpoint = ".well-known/openid-configuration";
            public const string JwksUri = "jwks_uri";
            public const string JwksUriOverride = "jwks_uri_override";
            public const string EndSessionEndpoint = "end_session_endpoint";
            public const string CheckSessionIframe = "check_session_iframe";
            public const string RegistrationEndpoint = "registration_endpoint";
            public const string CDRArrangementRevocationEndPoint = "cdr_arrangement_revocation_endpoint";
            public const string PushedAuthorizedRequestEndPoint = "pushed_authorization_request_endpoint";
            public const string RequirePushedAuthorizedRequests = "require_pushed_authorization_requests";
            public const string MtlsEndpointAliases = "mtls_endpoint_aliases";
            public const string FrontChannelLogoutSupported = "frontchannel_logout_supported";
            public const string FrontChannelLogoutSessionSupported = "frontchannel_logout_session_supported";
            public const string BackChannelLogoutSupported = "backchannel_logout_supported";
            public const string BackChannelLogoutSessionSupported = "backchannel_logout_session_supported";
            public const string GrantTypesSupported = "grant_types_supported";
            public const string CodeChallengeMethodsSupported = "code_challenge_methods_supported";
            public const string ScopesSupported = "scopes_supported";
            public const string SubjectTypesSupported = "subject_types_supported";
            public const string ResponseModesSupported = "response_modes_supported";
            public const string UserInfoSigningAlgorithmsSupported = "userinfo_signing_alg_values_supported";
            public const string TlsClientCertificateBoundAccessTokens = "tls_client_certificate_bound_access_tokens";
        }

        public static class Filters
        {
            // filter for claims from an incoming access token (e.g. used at the user profile endpoint)
            public static readonly string[] ProtocolClaimsFilter = {
                JwtClaimTypes.AccessTokenHash,
                JwtClaimTypes.Audience,
                JwtClaimTypes.AuthorizedParty,
                JwtClaimTypes.AuthorizationCodeHash,
                JwtClaimTypes.ClientId,
                JwtClaimTypes.Expiration,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Issuer,
                JwtClaimTypes.JwtId,
                JwtClaimTypes.Nonce,
                JwtClaimTypes.NotBefore,
                JwtClaimTypes.ReferenceTokenId,
                JwtClaimTypes.SessionId,
                JwtClaimTypes.Scope
            };

            // filter list for claims returned from profile service prior to creating tokens
            public static readonly string[] ClaimsServiceFilterClaimTypes = {
                JwtClaimTypes.AccessTokenHash,
                JwtClaimTypes.Audience,
                JwtClaimTypes.AuthenticationMethod,
                JwtClaimTypes.AuthenticationTime,
                JwtClaimTypes.AuthorizedParty,
                JwtClaimTypes.AuthorizationCodeHash,
                JwtClaimTypes.ClientId,
                JwtClaimTypes.Expiration,
                JwtClaimTypes.IdentityProvider,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Issuer,
                JwtClaimTypes.JwtId,
                JwtClaimTypes.Nonce,
                JwtClaimTypes.NotBefore,
                JwtClaimTypes.ReferenceTokenId,
                JwtClaimTypes.SessionId,
                JwtClaimTypes.Subject,
                JwtClaimTypes.Scope,
                JwtClaimTypes.Confirmation
            };
            public static readonly string[] JwtRequestClaimTypesFilter = {
                JwtClaimTypes.Audience,
                JwtClaimTypes.Expiration,
                JwtClaimTypes.IssuedAt,
                JwtClaimTypes.Issuer,
                JwtClaimTypes.NotBefore,
                JwtClaimTypes.JwtId,
            };
        }

        public static readonly string[] SupportedDisplayModes = 
        {
            OidcConstants.DisplayModes.Page,
            OidcConstants.DisplayModes.Popup,
            OidcConstants.DisplayModes.Touch,
            OidcConstants.DisplayModes.Wap
        };

        public static class Algorithms
        {
            public const string None = "none";

            public static class Signing
            {
                public const string ES256 = "ES256";
                public const string PS256 = "PS256";
            }

            public static class Jwe
            {
                public static class Alg
                {
                    public const string RSAOAEP = "RSA-OAEP";
                    public const string RSAOAEP256 = "RSA-OAEP-256";
                    public const string RSA15 = "RSA1_5";
                }

                public static class Enc
                {
                    public const string A128GCM = "A128GCM";
                    public const string A192GCM = "A192GCM";
                    public const string A256GCM = "A256GCM";
                    public const string A128CBCHS256 = "A128CBC-HS256";
                    public const string A192CBCHS384 = "A192CBC-HS384";
                    public const string A256CBCHS512 = "A256CBC-HS512";
                }
            }
        }

        public static class AuthenticationMethods
        {
            public const string OneTimePassword = "otp";
        }

        public static class EndpointAuthenticationMethods
        {
            public const string PrivateKeyJwt = "private_key_jwt";
        }

        public static class ProtectedResourceErrors
        {
            public const string InvalidToken = "invalid_token";
            public const string ExpiredToken = "expired_token";
            public const string InvalidRequest = "invalid_request";
            public const string InsufficientScope = "insufficient_scope";
        }

        public static class CodeChallengeMethods
        {
            public const string Plain = "plain";
            public const string Sha256 = "S256";
        }

        public static class PromptModes
        {
            public const string None = "none";
            public const string Login = "login";
            public const string Consent = "consent";
            public const string SelectAccount = "selectaccount";
        }

        public static class SubjectTypes
        {
            public const string Pairwise = "pairwise";
        }

        public static class ResponseModes
        {
            public const string FormPost = "form_post";
            public const string Query = "query";
            public const string Fragment = "fragment";
        }

        public static class ResponseTypes
        {
            public const string CodeIdToken = "code id_token";
        }

        public static class ClientAssertionTypes
        {
            public const string JwtBearer = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
        }

        public static class GrantTypes
        {
            public const string AuthorizationCode = "authorization_code";
            public const string ClientCredentials = "client_credentials";
            public const string RefreshToken = "refresh_token";
            public const string JwtBearer = "urn:ietf:params:oauth:grant-type:jwt-bearer";
            public const string JwtBearerAlternate = "urn:ietf:params:oauth:grant-type:jwtbearer";
            public const string Hybrid = "hybrid";
            public const string PushAuthoriseRequest = "push_authorise_request";
            public const string CdrArrangementGrant = "cdr_arrangement_grant";
            public const string GrantType = "grant_type";
        }

        public static class AuthenticationSchemes
        {
            public const string AuthorizationHeaderBearer = "Bearer";
        }

        public static class TokenTypeIdentifiers
        {
            public const string AccessToken = "urn:ietf:params:oauth:token-type:access_token";
            public const string IdentityToken = "urn:ietf:params:oauth:token-type:id_token";
            public const string RefreshToken = "urn:ietf:params:oauth:token-type:refresh_token";
        }

        public static readonly ImmutableList<string> SupportedTokenTypeHints = new List<string>
        {
            TokenTypes.RefreshToken,
        }.ToImmutableList();

        // Custom: This is different from the source IDV4. So should be a custom validator.
        public static readonly ImmutableList<string> SupportedResponseTypes = new List<string>
        {
            ResponseTypes.CodeIdToken,
        }.ToImmutableList();

        public static readonly ImmutableDictionary<string, string> ResponseTypeToGrantTypeMapping = new Dictionary<string, string>
        {
            { ResponseTypes.CodeIdToken, GrantTypes.Hybrid },
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<string, IEnumerable<string>> AllowedResponseModesForGrantType = new Dictionary<string, IEnumerable<string>>
        {
            { GrantTypes.AuthorizationCode, new[] { ResponseModes.Fragment, ResponseModes.FormPost } },
            { GrantTypes.Hybrid, new[] { ResponseModes.Fragment, ResponseModes.FormPost } },
        }.ToImmutableDictionary();

        public static readonly string[] AllowedGrantTypesForAuthorizeEndpoint = 
        {
            GrantTypes.AuthorizationCode,
            GrantTypes.Hybrid,
        };

        public static readonly string[] SupportedResponseModes = 
        {
            ResponseModes.FormPost,
            ResponseModes.Fragment,
        };

        public static readonly string[] SupportedCodeChallengeMethods = 
        {
            CodeChallengeMethods.Sha256,
        };

        public enum ScopeRequirement
        {
            None,
            ResourceOnly,
            IdentityOnly,
            Identity,
        }

        public enum FapiComplianceLevel
        {
            Fapi1Phase1 = 11,
            Fapi1Phase2 = 12,
            Fapi1Phase3 = 13,
            Fapi2 = 20,
        }

        public static readonly ImmutableDictionary<string, ScopeRequirement> ResponseTypeToScopeRequirement = new Dictionary<string, ScopeRequirement>
        {
            { ResponseTypes.CodeIdToken, ScopeRequirement.Identity },
        }.ToImmutableDictionary();

        public static readonly string[] SupportedPromptModes =
        {
            PromptModes.None,
            PromptModes.Login,
            PromptModes.Consent,
            PromptModes.SelectAccount,
        };

        public static class RevocationErrors
        {
            public const string UnsupportedTokenType = "unsupported_token_type";
        }

        public static class TokenTypes
        {
            public const string AccessToken = "access_token";
            public const string IdentityToken = "id_token";
            public const string RefreshToken = "refresh_token";
        }

        public static class Pairwise
        {
            public const string Salt = "00000000000000000000000000000000";
        }

        public static class ClientMetadata
        {
            public const string ClientIdIssuedAt = "client_id_issued_at";
            public const string RedirectUris = "redirect_uris";
            public const string RequestObjectSigningAlgorithm = "request_object_signing_alg";
            public const string UserinfoEncryptedResponseEncryption = "userinfo_encrypted_response_enc";
            public const string UserInfoEncryptedResponseAlgorithm = "userinfo_encrypted_response_alg";
            public const string UserinfoSignedResponseAlgorithm = "userinfo_signed_response_alg";
            public const string IdentityTokenEncryptedResponseEncryption = "id_token_encrypted_response_enc";
            public const string IdentityTokenEncryptedResponseAlgorithm = "id_token_encrypted_response_alg";
            public const string IdentityTokenSignedResponseAlgorithm = "id_token_signed_response_alg";
            public const string RequestUris = "request_uris";
            public const string InitiateLoginUris = "initiate_login_uri";
            public const string DefaultAcrValues = "default_acr_values";
            public const string RequireAuthenticationTime = "require_auth_time";
            public const string DefaultMaxAge = "default_max_age";
            public const string TokenEndpointAuthenticationSigningAlgorithm = "token_endpoint_auth_signing_alg";
            public const string TokenEndpointAuthenticationMethod = "token_endpoint_auth_method";
            public const string SubjectType = "subject_type";
            public const string SectorIdentifierUri = "sector_identifier_uri";
            public const string Jwks = "jwks";
            public const string JwksUri = "jwks_uri";
            public const string RevocationUri = "revocation_uri";
            public const string RecipientBaseUri = "recipient_base_uri";
            public const string TosUri = "tos_uri";
            public const string PolicyUri = "policy_uri";
            public const string ClientUri = "client_uri";
            public const string LogoUri = "logo_uri";
            public const string ClientName = "client_name";
            public const string Contacts = "contacts";
            public const string ApplicationType = "application_type";
            public const string GrantTypes = "grant_types";
            public const string ResponseTypes = "response_types";
            public const string RequestObjectEncryptionAlgorithm = "request_object_encryption_alg";
            public const string RequestObjectEncryptionEncryption = "request_object_encryption_enc";
            public const string SoftwareId = "software_id";
            public const string SoftwareStatement = "software_statement";
            public const string LegalEntityId = "legal_entity_id";
            public const string LegalEntityName = "legal_entity_name"; 
            public const string OrgId = "org_id";
            public const string OrgName = "org_name";
            public const string SoftwareRoles = "software_roles";
            public const string ClientDescription = "client_description";
        }

        public static class RegistrationRequest
        {
            public const string ClientMetadata = "client_metadata";
            public const string SoftwareStatement = "software_statement";
            public const string RedirectUri = "redirect_uri";
            public const string Scope = "scope";
            public const string ClientIdentifier = "client_identifier";
        }

        public static class RegistrationResponse
        {
            public const string Error = "error";
            public const string ErrorDescription = "error_description";
            public const string ClientId = "client_id";
            public const string ClientIdIssuedAt = "client_id_issued_at";
        }

        public static class RegistrationErrors
        {
            public const string InvalidRedirectUri = "invalid_redirect_uri";
            public const string InvalidClientMetadata = "invalid_client_metadata";
            public const string InvalidSoftwareStatement = "invalid_software_statement";
            public const string UnapprovedSoftwareStatement = "unapproved_software_statement";
        }

        public static class TokenIntrospectionRequest
        {
            public const string Token = "token";
            public const string TokenTypeHint = "token_type_hint";
        }

        public static class TokenResponse
        {
            public const string AccessToken = "access_token";
            public const string ExpiresIn = "expires_in";
            public const string TokenType = "token_type";
            public const string RefreshToken = "refresh_token";
            public const string IdentityToken = "id_token";
            public const string Error = "error";
            public const string ErrorDescription = "error_description";
            public const string BearerTokenType = "Bearer";
            public const string IssuedTokenType = "issued_token_type";
            public const string Scope = "scope";
        }

        public static class TokenErrors
        {
            public const string InvalidRequest = "invalid_request";
            public const string InvalidClient = "invalid_client";
            public const string InvalidGrant = "invalid_grant";
            public const string UnauthorizedClient = "unauthorized_client";
            public const string UnsupportedGrantType = "unsupported_grant_type";
            public const string UnsupportedResponseType = "unsupported_response_type";
            public const string AuthorizationPending = "authorization_pending";
            public const string AccessDenied = "access_denied";
            public const string SlowDown = "slow_down";
            public const string ExpiredToken = "expired_token";
        }

        public static class ParsedSecretTypes
        {
            public const string RevocationSecret = "ClientRevocationSecret";
            public const string TokenSecret = "ClientTokenSecret";
            public const string ArrangementRevocationSecret = "ClientArrangementRevocationSecret";
        }

        public static class TokenRequestTypes
        {
            public const string Bearer = "bearer";
        }

        public static class PushedAuthorizationRequest
        {
            public const string Scope = "scope";
            public const string CodeChallengeMethod = "code_challenge_method";
            public const string CodeChallenge = "code_challenge";
            public const string AcrValues = "acr_values";
            public const string LoginHint = "login_hint";
            public const string IdTokenHint = "id_token_hint";
            public const string UiLocales = "ui_locales";
            public const string MaxAge = "max_age";
            public const string Prompt = "prompt";
            public const string Display = "display";
            public const string Nonce = "nonce";
            public const string ResponseMode = "response_mode";
            public const string State = "state";
            public const string RedirectUri = "redirect_uri";
            public const string ClientId = "client_id";
            public const string ResponseType = "response_type";
            public const string Request = "request";
            public const string RequestUri = "request_uri";
            public const string GrantType = "grant_type";
            public const string ClientAssertionType = "client_assertion_type";
            public const string ClientAssertion = "client_assertion";
        }

        public static class TokenRequest
        {
            public const string Token = "token";
            public const string GrantType = "grant_type";
            public const string SubjectTokenType = "subject_token_type";
            public const string SubjectToken = "subject_token";
            public const string RequestedTokenType = "requested_token_type";
            public const string Audience = "audience";
            public const string Resource = "resource";
            public const string DeviceCode = "device_code";
            public const string Key = "key";
            public const string Algorithm = "alg";
            public const string TokenType = "token_type";
            public const string CodeVerifier = "code_verifier";
            public const string Password = "password";
            public const string UserName = "username";
            public const string Scope = "scope";
            public const string RefreshToken = "refresh_token";
            public const string Code = "code";
            public const string Assertion = "assertion";
            public const string ClientAssertionType = "client_assertion_type";
            public const string ClientAssertion = "client_assertion";
            public const string ClientSecret = "client_secret";
            public const string ClientId = "client_id";
            public const string RedirectUri = "redirect_uri";
            public const string ActorToken = "actor_token";
            public const string ActorTokenType = "actor_token_type";
            public const string OngoingConsent = "Ongoing";
            public const string SingleConsent = "Single";
        }

        public static class CdrArrangementRevocationRequest
        {
            public const string CdrArrangementId = "cdr_arrangement_id";
            public const string CdrArrangementJwt = "cdr_arrangement_jwt";
            public const string ClientAssertionType = "client_assertion_type";
            public const string ClientAssertion = "client_assertion";
            public const string ClientId = "client_id";
        }

        public static class EndSessionRequest
        {
            public const string IdTokenHint = "id_token_hint";
            public const string PostLogoutRedirectUri = "post_logout_redirect_uri";
            public const string State = "state";
            public const string Sid = "sid";
            public const string Issuer = "iss";
        }

        public static class AuthorizeResponse
        {
            public const string Code = "code";
            public const string AccessToken = "access_token";
            public const string ExpiresIn = "expires_in";
            public const string TokenType = "token_type";
            public const string RefreshToken = "refresh_token";
            public const string IdentityToken = "id_token";
            public const string State = "state";

            // Standard OIDC error fields
            public const string Error = "error";
            public const string ErrorDescription = "error_description";
        }

        public static class AuthorizeError
        {
            public const string Error = "error";
            public const string ErrorDescription = "error_description";            
        }

        public static class AuthorizeErrorCodes
        {
            public const string InvalidRequest = "invalid_request";
            public const string UnauthorizedClient = "unauthorized_client";
            public const string AccessDenied = "access_denied";
            public const string UnsupportedResponseType = "unsupported_response_type";
            public const string ServerError = "server_error";
            public const string TemporarilyUnavailable = "temporarily_unavailable";
            public const string InteractionRequired = "interaction_required";
            public const string LoginRequired = "login_required";
            public const string AccountSelectionRequired = "account_selection_required";
            public const string ConsentRequired = "consent_required";
            public const string InvalidRequestUri = "invalid_request_uri";
            public const string InvalidRequestObject = "invalid_request_object";
            public const string RequestNotSupported = "request_not_supported";
            public const string RequestUriNotSupported = "request_uri_not_supported";
            public const string RegistrationNotSupported = "registration_not_supported";            
        }

        public static class AuthorizeErrorCodeDescription
        {            
            public const string MissingOpenIdScope = "Missing openid scope";
            public const string UnknownClient = "Unknown client or client not enabled";
            public const string InvalidClientId = "Invalid client_id";
            public const string InvalidScope = "Invalid scope";
            public const string InvalidRedirectUri = "Invalid redirect_uri";
            public const string InvalidJWTRequest = "Invalid JWT request";
            public const string InvalidGrantType = "Invalid grant type for client";
            public const string UnknownClientRegistration = "The client is unknown.";
        }

        public static class CdrArrangementRevocationErrorCodes
        {
            public const string InvalidRequest = "invalid_request";
            public const string UnauthorizedClient = "unauthorized_client";
            public const string InvalidClient = "invalid_client";
        }

        public static class IntrospectionErrorCodes
        {
            public const string InvalidClient = "invalid_client";
            public const string InvalidRequest = "invalid_request";
            public const string UnsupportedTokenType = "unsupported_token_type";
            public const string UnsupportedGrantType = "unsupported_grant_type";
        }

        public static class AuthorizeErrorDescriptions
        {
            public const string InvalidRequestJwt = "Invalid request_jwt";
            public const string InvalidRedirectUri = "Invalid redirect_uri";
            public const string ClientIdNotFound = "Client Id not found in client store";
        }

        public static class UserInfoErrorCodes
        {
            public const string InvalidLegalStatusInactive= "Invalid legal_status_inactive";
            public const string InvalidLegalStatusRemoved = "Invalid legal_status_removed";
            public const string InvalidLegalStatusSuspended = "Invalid legal_status_suspended";
            public const string InvalidLegalStatusRevoked = "Invalid legal_status_revoked";
            public const string InvalidLegalStatusSurrendered = "Invalid legal_status_surrendered";
            
            public const string InvalidSoftwareProductStatusInactive = "Invalid software_status_inactive";
            public const string InvalidSoftwareProductStatusRemoved = "Invalid software_status_removed";
        }

        public static class UserInfoStatusErrorDescriptions
        {
            public const string StatusInactive = "Inactive";
            public const string StatusRemoved = "Removed";
            public const string StatusSuspended = "Suspended";
            public const string StatusRevoked = "Revoked";
            public const string StatusSurrendered = "Surrendered";
        }

        public static class RequestPath
        {
            public const string Authorize = "/connect/authorize";

            public const string Token = "/connect/token";

            public const string ArrangementRevocation = "/connect/arrangements/revoke";

            public const string Revocation = "/connect/revocation";

            public const string Par = "/connect/par";

            public const string Introspection = "/connect/introspect";
        }

        public static class StandardClaims
        {
            public const string Expiry = "exp";
            public const string RefreshTokenExpiresAt = "refresh_token_expires_at";
            public const string SharingDurationExpiresAt = "sharing_expires_at";
            public const string SharingDuration = "sharing_duration";
            public const string ACR = "acr";
            public const string IDP = "idp";
            public const string Sub = "sub";
            public const string AuthTime = "auth_time";
            public const string ACR2Value = "urn:cds.au:cdr:2";
            public const string ACR3Value = "urn:cds.au:cdr:3";
            public const string CDRArrangementId = "cdr_arrangement_id";
            public const string AccountId = "account_id";
            public const string GrantType = "grant_type";
        }

        public static class RevocationRequest
        {
            public const string Token = "token";
            public const string TokenTypeHint = "token_type_hint";
            public const string ClientAssertionType = "client_assertion_type";
            public const string ClientAssertion = "client_assertion";
            public const string ClientSecret = "client_secret";
            public const string ClientId = "client_id";
        }

        public static class IntrospectionRequestElements
        {
            public const string Token = "token";
            public const string TokenTypeHint = "token_type_hint";
            public const string ClientAssertionType = "client_assertion_type";
            public const string ClientAssertion = "client_assertion";
            public const string ClientSecret = "client_secret";
            public const string ClientId = "client_id";
            public const string GrantType = "grant_type";
            public const string AllowedGrantType = "client_credentials";
            public const string AllowedClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
        }

        public static class JwtToken
        {
            public const string JwtType = "JWT";
        }

        public static class ProtocolTypes
        {
            public const string OpenIdConnect = "oidc";
        }

        public static class SecretTypes
        {
            public const string JsonWebKey = "JWK";
            public const string SigningJsonWebKey = "Sign-JWK";
            public const string EncyptionJsonWebKey = "Enc-JWK";
        }

        public static class SecretDescription
        {
            public const string Signing = "Sign-JWK";
            public const string Encyption = "Enc-JWK";
        }

        public enum ValidationCheck
        {
            [Description("MTLS missing or failed validation")]
            SSLClientCertThumbprintMissing = 600,
            [Description("Token is missing Cnf claim")]
            TokenMissingClientCertThumbprint = 601,
            [Description("MTLS misconfigured or failed validation")]
            TokenNotMatchingHeaderClientCertThumbprint = 602,
            [Description("Request header X-SSLClientCertCN not found")]
            SSLClientCertCNNotFound = 603,
            [Description("Parsed Secret is null")]
            ParsedSecretIsNull = 604,
            [Description("Secret validator cannot process type")]
            SecretValidatorCannotProcessType = 605,
            [Description("ParsedSecret.Credential is not valid")]
            CredentialNotValid = 606,
            [Description("Could not parse secrets")]
            CannotParseSecret = 607,
            [Description("There are no keys available to validate client assertion")]
            NoKeysToValidateClientAssertion = 608,
            [Description("No thumbprint found in X509 client certificate.")]
            NoThumbprintInCertificate = 609,
            [Description("No common name found in X509 client certificate.")]
            NoCommonNameInCertificate = 610,
            [Description("No matching x509 client certificate found.")]
            NoMatchingCertificateFound = 611,

            [Description("Token is invalid")]
            TokenFailedValidationWithIdentityServer = 700,
            [Description("Token is missing 'scope' claim for issuer")]
            TokenMissingScopeClaimForIssuer = 701,
            [Description("Token is missing valid 'scope' claim for issuer")]
            TokenMissingRequiredScopeClaimValue = 702,
            [Description("Token is missing 'client_id' claim")]
            TokenMissingClientClaimValue = 703,
            [Description("Token client_id does not match request param dataRecipientBrandId")]
            TokenClientClaimValueNotMatchingDataRecipientBrandId = 704,
            [Description("Client assertion token exceeds maximum length")]
            ClientAssertionExceedsLength = 705,
            [Description("Client assertion token sub claim (client_id) is missing")]
            ClientAssertioClientIdNotFound = 706,
            [Description("Client assertion token sub claim (client_id) exceeds maximum length")]
            ClientAssertioClientIdExceedsLength = 707,
            [Description("Client assertion token sub claim (client_id) does not match token request body client_id")]
            ClientIdNotMatch = 708,
            [Description("'jti' in the client assertion token must have a value")]
            JTIIsMissing = 709,
            [Description("'jti' in the client assertion token must be unique.")]
            JTINotUnique = 710,
            [Description("JWT token validation error")]
            JWTTokenValidationError = 711,
            [Description("Both 'sub' and 'iss' in the client assertion token must have a value of client_id.")]
            NoClientIdInSubIss = 712,

            [Description("Client Id not found in client store")]
            ClientIdNotFound = 800,
            [Description("Jwk URL returns 404.")]
            JwkUrl404 = 801,
            [Description("Jwk not found.")]
            JwkNotFound = 802,
            [Description("Jwk returns invalid response.")]
            JwkInvalidResponse = 803,
            [Description("No encryption Jwk found.")]
            JwkResponseNoEncryptionKey = 804,

            [Description("Industry param is invalid")]
            RequestIndustryParamInvalid = 900,
            [Description("Invalid params or request")]
            RequestFailedToGenerateAnSSA = 901,
            [Description("No JWT client assertion found in post body")]
            ClientAssertionNotFound = 902,

            [Description("Invalid JWT request")]
            RequestParamInvalid = 1001,
            [Description("Sharing Duration is invalid")]
            SharingDurationInvalid = 1002,
            [Description("Scope is invalid")]
            ScopeInvalid = 1003,
            [Description("ResponseType is invalid")]
            ResponseTypeInvalid = 1004,
            [Description("ACR is invalid")]
            ACRInvalid = 1005,
            [Description("Client Id is invalid")]
            ClientIdInvalid = 1007,
            [Description("Claims is invalid")]
            ClaimsInvalid = 1008,
            [Description("Software Product Status is invalid")]
            SoftwareProductStatusInvalid = 1009,
            [Description("Authorisation request is missing PKCE parameters")]
            AuthorisationRequestMissingPkce = 1010,

            [Description("Token request invalid parameters")]
            TokenRequestInvalidParameters = 1100,
            [Description("Token request invalid redirect uri")]
            TokenRequestInvalidUri = 1101,
            [Description("Token request invalid scope")]
            TokenRequestInvalidScope = 1102,

            [Description("Revocation request invalid parameters")]
            RevocationRequestInvalidParameters = 1200,
            [Description("No token found in request")]
            RevocationRequestNoTokenFoundInRequest = 1201,
            [Description("Unsupported token type")]
            RevocationRequestUnsupportedTokenType = 1202,

            [Description("Invalid response to revocation request with invalid audience")]
            RevocationResponseInvalidForInvalidAudience = 1300,
            [Description("Invalid response to revocation request with invalid issuer")]
            RevocationResponseInvalidForInvalidIssuer = 1301,
            [Description("Invalid response to revocation request with invalid sub")]
            RevocationResponseInvalidForInvalidSub = 1302,
            [Description("Invalid response to revocation request with invalid expiry")]
            RevocationResponseInvalidForInvalidExpiry = 1303,
            [Description("Invalid response to revocation request with invalid token value")]
            RevocationResponseInvalidForInvalidToken = 1304,
            [Description("Invalid response to revocation request with invalid token type hint")]
            RevocationResponseInvalidForInvalidTokenTypeHint = 1305,
            [Description("Invalid response to revocation request with missing bearer token")]
            RevocationResponseInvalidForMissingBearerToken = 1306,
            [Description("Invalid response to valid revocation request")]
            RevocationResponseInvalidForValidRequest = 1307,
            [Description("Invalid response to revocation request with invalid signature")]
            RevocationResponseInvalidForInvalidSignature = 1308,

            [Description("Invalid response to arrangement revocation request with invalid audience")]
            ArrangementRevocationResponseInvalidForInvalidAudience = 1800,
            [Description("Invalid response to arrangement revocation request with invalid issuer")]
            ArrangementRevocationResponseInvalidForInvalidIssuer = 1801,
            [Description("Invalid response to arrangement revocation request with invalid sub")]
            ArrangementRevocationResponseInvalidForInvalidSub = 1802,
            [Description("Invalid response to arrangement revocation request with invalid expiry")]
            ArrangementRevocationResponseInvalidForInvalidExpiry = 1803,
            [Description("Invalid response to arrangement revocation request with missing bearer token")]
            ArrangementRevocationResponseInvalidForMissingBearerToken = 1804,
            [Description("Invalid response to valid arrangement revocation request")]
            ArrangementRevocationResponseInvalidForValidRequest = 1805,
            [Description("Invalid response to arrangement revocation request with invalid signature")]
            ArrangementRevocationResponseInvalidForInvalidSignature = 1806,
            [Description("Invalid response to arrangement revocation request with invalid cdr arrangement id")]
            ArrangementRevocationResponseInvalidForInvalidCdrArrangementId = 1807,

            [Description("Registration Request has an invalid SSA JWT")]
            RegistrationRequestInvalidSSAJWT = 1401,
            [Description("Registration Request has an invalid RedirectUri")]
            RegistrationRequestInvalidRedirectUri = 1402,
            [Description("Registration Request has an invalid ClientMetadata")]
            RegistrationRequestInvalidClientMetadata = 1403,
            [Description("Registration Request is Unsupported Media Type")]
            RegistrationRequestUnsupportedMediaType = 1404,

            [Description("Invalid CDR Arrangement Id")]
            CdrArrangementRevocationInvalidCDRArrangementId = 1502,
            [Description("Invalid Request")]
            CdrArrangementRevocationInvalidRequest = 1503,
            [Description("Invalid Client Id")]
            CdrArrangementRevocationInvalidClientId = 1504,

            [Description("Request missing form content")]
            PARMissingFormBody = 1601,
            [Description("Invalid cdr arrangement id")]
            PARInvalidCdrArrangementId = 1602,
            [Description("Invalid request")]
            PARInvalidRequest = 1603,
            [Description("Request Jwt failed validation")]
            PARRequestJwtFailedValidation = 1604,
            [Description("Invalid client")]
            PARRequestInvalidClient = 1605,

            [Description("Invalid request")]
            AuthorizeRequestInvalid = 1701,
            [Description("Invalid request_uri")]
            AuthorizeRequestInvalidRequestUri = 1702,
            [Description("Invalid client_id")]
            AuthorizeRequestInvalidClientId = 1703,
            [Description("redirect_uri is expired")]
            AuthorizeRequestExpiredRequestUri = 1704,
            [Description("Invalid redirect_uri")]
            AuthorizeRequestInvalidRedirectUri = 1705,
            [Description("Invalid response_type")]
            AuthorizeRequestInvalidResponseType = 1706,
            [Description("Invalid scope")]
            AuthorizeRequestInvalidScope = 1707,
            [Description("Invalid state")]
            AuthorizeRequestInvalidState = 1708,
            [Description("Invalid nonce")]
            AuthorizeRequestInvalidNonce = 1709,
            [Description("Invalid parameters")]
            AuthorizeRequestInvalidParameters = 1710,
            [Description("Request_uri could not be found")]
            AuthorizeRequestNotFoundRequestUri = 1711,
        }

        public static class ValidationErrorMessages
        {
            public const string MissingClaim = "The '{0}' claim is missing from the token.";
            public const string MustEqual = "The '{0}' claim value must be set to '{1}'.";
            public const string MustBeOne = "The '{0}' claim value must be one of '{1}'.";
            public const string MustContain = "The '{0}' claim value must contain the '{1}' value.";
            public const string InvalidRedirectUri = "One or more redirect uri is invalid";
        }
    }
}