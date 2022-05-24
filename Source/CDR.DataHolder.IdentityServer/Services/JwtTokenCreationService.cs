using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public class JwtTokenCreationService : DefaultTokenCreationService, IJwtTokenCreationService
    {
        private readonly ISecurityService _securityService;

        public JwtTokenCreationService(
                ISystemClock clock,
                IKeyMaterialService keys,
                ILogger<DefaultTokenCreationService> logger,
                IdentityServerOptions options,
                ISecurityService securityService)
                : base(clock, keys, options, logger)
        {
            _securityService = securityService;
        }

        protected override async Task<string> CreateJwtAsync(JwtSecurityToken jwt)
        {
            var plaintext = $"{jwt.EncodedHeader}.{jwt.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);
            var signature = await _securityService.Sign(jwt.SignatureAlgorithm, digest);

            return $"{plaintext}.{Base64UrlTextEncoder.Encode(signature)}";
        }
    }
}