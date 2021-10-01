using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using CDR.DataHolder.IdentityServer.Extensions;
using Jose;

namespace CDR.DataHolder.IdentityServer.UnitTests
{
	public static class JwtHelper
    {
        public static string GetJwtValidSignature(object payload, string kid = "123")
        {
            // Create the JWT header
            var jwtHeader = new Dictionary<string, object>()
                {
                    { JwtHeaderParameterNames.Alg, "PS256" },
                    { JwtHeaderParameterNames.Typ, "JWT" },
                    { JwtHeaderParameterNames.Kid, kid },
                };

            return JWT.Encode(payload.ToJson(), X509CertificateForValidClientAssertionTesting().GetRSAPrivateKey(), JwsAlgorithm.PS256, extraHeaders: jwtHeader);
        }

        public static string GetAuthorizeRequestJwtInvalidSignature(AuthorizeRequestJwt authorizeRequest)
        {
            // Create the JWT header
            var jwtHeader = new Dictionary<string, object>()
                {
                    { JwtHeaderParameterNames.Alg, "PS256" },
                    { JwtHeaderParameterNames.Typ, "JWT" },
                };

            return JWT.Encode(authorizeRequest.ToJson(), X509CertificateForInvalidTesting().GetRSAPrivateKey(), JwsAlgorithm.PS256, extraHeaders: jwtHeader);
        }

        public static X509Certificate2 X509CertificateForValidClientAssertionTesting()
        {
            return new X509Certificate2("Certificates/ssa.pfx", "#M0ckRegister#");
        }


        public static X509Certificate2 X509CertificateForInvalidTesting()
        {
            return new X509Certificate2("Certificates/server.pfx", "#M0ckDataHolder#");
        }
    }
} 