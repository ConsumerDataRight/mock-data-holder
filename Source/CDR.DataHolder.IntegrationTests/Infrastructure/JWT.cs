using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Jose;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure
{
    static public class JWT2
    {
        static public string CreateJWT(string certificateFilename, string certificatePassword, Dictionary<string, object> subject)
        {
            var payload = JsonConvert.SerializeObject(subject);

            return CreateJWT(certificateFilename, certificatePassword, payload);
        }

        static private string CreateJWT(string certificateFilename, string certificatePassword, string payload)
        {
            var cert = new X509Certificate2(certificateFilename, certificatePassword);

            var securityKey = new X509SecurityKey(cert);

            var jwtHeader = new Dictionary<string, object>()
            {
                { JwtHeaderParameterNames.Alg, "PS256" },
                { JwtHeaderParameterNames.Typ, "JWT" },
                { JwtHeaderParameterNames.Kid, securityKey.KeyId},
            };

            var jwt = Jose.JWT.Encode(payload, cert.GetRSAPrivateKey(), JwsAlgorithm.PS256, jwtHeader);

            return jwt;
        }
    }
}