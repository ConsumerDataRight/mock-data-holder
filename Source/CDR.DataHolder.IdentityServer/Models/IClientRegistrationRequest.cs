using System;
using System.Collections.Generic;

namespace CDR.DataHolder.IdentityServer.Models
{
    public interface IClientRegistrationRequest
    {
        Guid? ClientId { get; }

        /// <summary>Gets the Software Statement Assertion, as defined in [Dynamic Client Registration](https://cdr-register.github.io/register/#dynamic-client-registration).</summary>
        string SoftwareStatementJwt { get; }

        string Kid { get; }

        string SigningJwk { get; set; }

        string EncryptionJwk { get; set; }

        /// <summary>Gets the Software Statement Assertion, as defined in [Dynamic Client Registration](https://cdr-register.github.io/register/#dynamic-client-registration).</summary>
        SoftwareStatement SoftwareStatement { get; }

        string ClientRegistrationRequestJwt { get; }

        /// <summary>Gets the time at which the request was issued by the TPP  expressed as seconds since 1970-01-01T00:00:00Z as measured in UTC.</summary>
        int? Iat { get; }

        /// <summary>Gets the time at which the request expires expressed as seconds since 1970-01-01T00:00:00Z as measured in UTC.</summary>
        int? Exp { get; }

        /// <summary>Gets Unique identifier for the Data Holder issued by the CDR Register.</summary>
        string Iss { get; }

        /// <summary>Gets Unique identifier for the JWT, used to prevent replay of the token.</summary>
        string Jti { get; }

        /// <summary>Gets the audience for the request. This should be the Data Holder authorisation server URI.</summary>
        string Aud { get; }

        /// <summary>Gets array of redirection URI strings for use in redirect-based flows.</summary>
        IEnumerable<string> RedirectUris { get; }

        /// <summary>Gets the requested authentication method for the token endpoint.</summary>
        string TokenEndpointAuthMethod { get; }

        /// <summary>Gets the algorithm used for signing the JWT.</summary>
        string TokenEndpointAuthSigningAlg { get; }

        /// <summary>Gets array of OAuth 2.0 grant type strings that the client can use at the token endpoint.</summary>
        IEnumerable<string> GrantTypes { get; }

        /// <summary>Gets array of the OAuth 2.0 response type strings that the client can use at the authorization endpoint.</summary>
        IEnumerable<string> ResponseTypes { get; }

        /// <summary>Gets kind of the application. The only supported application type will be &#x60;web&#x60;.</summary>
        string ApplicationType { get; }

        /// <summary>Gets algorithm with which an id_token is to be signed.</summary>
        string IdTokenSignedResponseAlg { get; }

        /// <summary>Gets JWE &#x60;alg&#x60; algorithm with which an id_token is to be encrypted.</summary>
        string IdTokenEncryptedResponseAlg { get; }

        /// <summary>Gets JWE &#x60;enc&#x60; algorithm with which an id_token is to be encrypted.</summary>
        string IdTokenEncryptedResponseEnc { get; }

        /// <summary>Gets algorithm which the ADR expects to sign the request object if a request object will be part of the authorization request sent to the Data Holder.</summary>
        string RequestObjectSigningAlg { get; }

    }
}