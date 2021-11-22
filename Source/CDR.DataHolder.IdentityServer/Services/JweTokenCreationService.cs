using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Interfaces;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Jose;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using JsonWebKey = Microsoft.IdentityModel.Tokens.JsonWebKey;

namespace CDR.DataHolder.IdentityServer.Services
{
    /// <summary>
    /// Encrypting done using https://github.com/dvsekhvalnov/jose-jwt.
    /// </summary>
    public class JweTokenCreationService : ITokenCreationService
    {
        private readonly IClientService _clientService;
        private readonly IJwtTokenCreationService _jwtTokenCreationService;
        private readonly ILogger<JweTokenCreationService> _logger;

        public JweTokenCreationService(
            IClientService clientService,
            IJwtTokenCreationService jwtTokenCreationService,
            ILogger<JweTokenCreationService> logger)
        {
            _clientService = clientService;
            _jwtTokenCreationService = jwtTokenCreationService;
            _logger = logger;
        }

        public async Task<string> CreateTokenAsync(Token token)
        {
            if (token.Type != IdentityServerConstants.TokenTypes.IdentityToken)
            {
                return await _jwtTokenCreationService.CreateTokenAsync(token);
            }

            var client = await _clientService.FindClientById(token.ClientId);
            var clientEncryptionAlg = client.Claims.FirstOrDefault(x => x.Type == ClientMetadata.IdentityTokenEncryptedResponseAlgorithm)?.Value;
            var clientEncryptionEnc = client.Claims.FirstOrDefault(x => x.Type == ClientMetadata.IdentityTokenEncryptedResponseEncryption)?.Value;

            // Get the client enc jwk
            var clientEncJwks = client.ClientSecrets
                .Where(s => s.Type == SecretTypes.JsonWebKey && s.Description == SecretDescription.Encyption)
                .Select(s => new JsonWebKey(s.Value));
            var clientJwk = clientEncJwks.FirstOrDefault(jwk => jwk.Alg == clientEncryptionAlg);
            var rsaEncryption = GetEncryptionKey(clientJwk);

            try
            {
                var tokenSigned = await _jwtTokenCreationService.CreateTokenAsync(token);
                _logger.LogDebug("Encrypting Id Token with Alg {Alg}, Enc {Enc}", clientEncryptionAlg, clientEncryptionEnc);

                // Encode the token and add the kid
                // Additional info: FAPIValidateEncryptedIdTokenHasKid (OIDCC-10.1)
                return JWT.Encode(tokenSigned, rsaEncryption, GetJweAlgorithm(clientEncryptionAlg), GetJweEncryption(clientEncryptionEnc),
                    extraHeaders:new Dictionary<string,object>() {{"kid", clientJwk.Kid }});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encrypting Id Token with Alg {alg}, Enc {enc} failed", clientEncryptionAlg, clientEncryptionEnc);
                throw new Exception("Error encrypting id token jwt", ex);
            }
            finally
            {
                if (rsaEncryption != null)
                {
                    rsaEncryption.Dispose();
                }
            }
        }

        private RSA GetEncryptionKey(JsonWebKey clientJwk)
        {
            if (clientJwk == null)
            {
                return null;
            }

            var rsaEncryption = RSA.Create(new RSAParameters
            {
                Modulus = Base64Url.Decode(clientJwk.N),
                Exponent = Base64Url.Decode(clientJwk.E),
            });
            return rsaEncryption;
        }

        private JweAlgorithm GetJweAlgorithm(string clientAlg)
        {
            return clientAlg switch
            {
                Algorithms.Jwe.Alg.RSAOAEP => JweAlgorithm.RSA_OAEP,
                Algorithms.Jwe.Alg.RSAOAEP256 => JweAlgorithm.RSA_OAEP_256,
                Algorithms.Jwe.Alg.RSA15 => JweAlgorithm.RSA1_5,
                _ => throw new Exception($"Client Algorithm {clientAlg} not supported for encryption of Id Token"),
            };
        }

        private JweEncryption GetJweEncryption(string clientEnc)
        {
            return clientEnc switch
            {
                Algorithms.Jwe.Enc.A128GCM => JweEncryption.A128GCM,
                Algorithms.Jwe.Enc.A192GCM => JweEncryption.A192GCM,
                Algorithms.Jwe.Enc.A256GCM => JweEncryption.A256GCM,
                Algorithms.Jwe.Enc.A128CBCHS256 => JweEncryption.A128CBC_HS256,
                Algorithms.Jwe.Enc.A192CBCHS384 => JweEncryption.A192CBC_HS384,
                Algorithms.Jwe.Enc.A256CBCHS512 => JweEncryption.A256CBC_HS512,
                _ => throw new Exception($"Client Encoding {clientEnc} not supported for encryption of Id Token"),
            };
        }
    }
}