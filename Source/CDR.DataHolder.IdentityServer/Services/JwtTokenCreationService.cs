using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public class JwtTokenCreationService : DefaultTokenCreationService, IJwtTokenCreationService
    {
        private readonly IConfigurationSettings _configurationSettings;
        private readonly ISecurityService _securityService;

        public JwtTokenCreationService(
                IConfigurationSettings configurationSettings,
                ISystemClock clock,
                IKeyMaterialService keys,
                ILogger<DefaultTokenCreationService> logger,
                IdentityServerOptions options,
                ISecurityService securityService)
                : base(clock, keys, options, logger)
        {
            _securityService = securityService;
            _configurationSettings = configurationSettings;
        }

        protected override async Task<string> CreateJwtAsync(JwtSecurityToken jwt)
        {
            var plaintext = $"{jwt.EncodedHeader}.{jwt.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);

            //using var hasher = CryptoHelper.GetHashAlgorithmForSigningAlgorithm(jwt.SignatureAlgorithm);
            //byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(plaintext));

            //var signingKeyName = jwt.SignatureAlgorithm switch
            //{
            //    SecurityAlgorithms.RsaSsaPssSha256 => _configurationSettings.KeyStore.SigningKeyRsa,
            //    SecurityAlgorithms.EcdsaSha256 => _configurationSettings.KeyStore.SigningKeyEcdsa,
            //    _ => throw new NotSupportedException(),
            //};

            //var signature = await _securityService.Sign(signingKeyName, jwt.SignatureAlgorithm, hash);
            var signature = await _securityService.Sign(jwt.SignatureAlgorithm, digest);

            return $"{plaintext}.{Base64UrlTextEncoder.Encode(signature)}";
        }
    }
}