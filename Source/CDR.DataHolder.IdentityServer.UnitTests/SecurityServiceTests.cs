using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.UnitTests
{
    public class SecurityServiceTests
    {
        private readonly IConfiguration _configuration;
        public SecurityServiceTests()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _configuration = configuration;
        }

        [Fact]
        public async Task Sign_PS256_Success()
        {
            //Arrange
            var securityService = new SecurityService(_configuration);

            //Create jwt token
            var keys = await securityService.GetActiveSecurityKeys(SecurityAlgorithms.RsaSsaPssSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                   claims: GetClaims(),
                   issuer: _configuration["CTSDataHolderBrandId"],
                   audience: _configuration["ArrangementRevocationUri"],
                   expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["DefaultExpiryMinutes"])));

            jwtSecurityToken.Header["alg"] = Algorithms.Signing.PS256;
            jwtSecurityToken.Header["kid"] = keys.First().Key.KeyId;
            jwtSecurityToken.Header["typ"] = JwtToken.JwtType;

            var plaintext = $"{jwtSecurityToken.EncodedHeader}.{jwtSecurityToken.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);

            var signature = Base64UrlTextEncoder.Encode(await securityService.Sign(jwtSecurityToken.SignatureAlgorithm, digest));

            var token = $"{plaintext}.{signature}";

            //Validate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            //Read token
            var parsedJwt = tokenHandler.ReadToken(token) as JwtSecurityToken;

            //Create the certificate which has only public key
            var cert = new X509Certificate2(_configuration["SigningCertificatePublic:Path"], SecurityAlgorithms.RsaSsaPssSha256);

            //Get credentials from certificate
            var certificateSecurityKey = new X509SecurityKey(cert);

            //Set token validation parameters
            var validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey = certificateSecurityKey,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["CTSDataHolderBrandId"],
                ValidateIssuer = true,                
                ValidAudience = _configuration["ArrangementRevocationUri"],
                ValidateAudience = false,
                ValidateLifetime = false
            };

            SecurityToken validatedToken;
            //Act
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

            //Assert
            Assert.True(validatedToken != null);
            Assert.True(validatedToken.Issuer == _configuration["CTSDataHolderBrandId"]);
            var iss = principal.Claims.Single(x => x.Type == "iss").Value;
            var aud = principal.Claims.Single(x => x.Type == "aud").Value;
            Assert.True(iss == _configuration["CTSDataHolderBrandId"]);
            Assert.True(aud == _configuration["ArrangementRevocationUri"]);
        }

        [Fact]
        public async Task Sign_PS256_InvalidToken_Failure()
        {
            //Arrange
            var securityService = new SecurityService(_configuration);

            //Create jwt token
            var keys = await securityService.GetActiveSecurityKeys(SecurityAlgorithms.RsaSsaPssSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                   claims: GetClaims(),
                   issuer: _configuration["CTSDataHolderBrandId"],
                   audience: _configuration["ArrangementRevocationUri"],
                   expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["DefaultExpiryMinutes"])));

            jwtSecurityToken.Header["alg"] = Algorithms.Signing.PS256;
            jwtSecurityToken.Header["kid"] = keys.First().Key.KeyId;
            jwtSecurityToken.Header["typ"] = JwtToken.JwtType;

            var plaintext = $"{jwtSecurityToken.EncodedHeader}.{jwtSecurityToken.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);

            var signature = Base64UrlTextEncoder.Encode(await securityService.Sign(jwtSecurityToken.SignatureAlgorithm, digest));

            var token = $"{plaintext}.{signature}";

            //Create invalid token
            token = token.Replace('a', 'b');

            //Validate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            //Read token
            var parsedJwt = tokenHandler.ReadToken(token) as JwtSecurityToken;

            //Create the certificate which has only public key
            var cert = new X509Certificate2(_configuration["SigningCertificatePublic:Path"], SecurityAlgorithms.RsaSsaPssSha256);

            //Get credentials from certificate
            var certificateSecurityKey = new X509SecurityKey(cert);

            //Set token validation parameters
            var validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey = certificateSecurityKey,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["CTSDataHolderBrandId"],
                ValidateIssuer = true,
                ValidAudience = _configuration["ArrangementRevocationUri"],
                ValidateAudience = false,
                ValidateLifetime = false
            };

            try
            {
                SecurityToken validatedToken;
                //Act
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            }
            catch (Exception ex)
            {
                var isInvalidSignatureError = ex.Message.StartsWith("IDX10511: Signature validation failed.");
                //Assert
                Assert.True(isInvalidSignatureError);
            }
        }

        [Fact]
        public async Task Sign_PS256_InvalidCertificate_Failure()
        {
            //Arrange
            var securityService = new SecurityService(_configuration);

            //Create jwt token
            var keys = await securityService.GetActiveSecurityKeys(SecurityAlgorithms.RsaSsaPssSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                   claims: GetClaims(),
                   issuer: _configuration["CTSDataHolderBrandId"],
                   audience: _configuration["ArrangementRevocationUri"],
                   expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["DefaultExpiryMinutes"])));

            jwtSecurityToken.Header["alg"] = Algorithms.Signing.PS256;
            jwtSecurityToken.Header["kid"] = keys.First().Key.KeyId;
            jwtSecurityToken.Header["typ"] = JwtToken.JwtType;

            var plaintext = $"{jwtSecurityToken.EncodedHeader}.{jwtSecurityToken.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);

            var signature = Base64UrlTextEncoder.Encode(await securityService.Sign(jwtSecurityToken.SignatureAlgorithm, digest));

            var token = $"{plaintext}.{signature}";

            //Validate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            //Read token
            var parsedJwt = tokenHandler.ReadToken(token) as JwtSecurityToken;

            //Create the certificate which has only public key
            var cert = new X509Certificate2(_configuration["InvalidSigningCertificatePublic:Path"], SecurityAlgorithms.RsaSsaPssSha256);

            //Get credentials from certificate
            var certificateSecurityKey = new X509SecurityKey(cert);

            //Set token validation parameters
            var validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey = certificateSecurityKey,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["CTSDataHolderBrandId"],
                ValidateIssuer = true,
                ValidAudience = _configuration["ArrangementRevocationUri"],
                ValidateAudience = false,
                ValidateLifetime = false
            };

            try
            {
                SecurityToken validatedToken;
                //Act
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            }
            catch (Exception ex)
            {
                var isInvalidSignatureError = ex.Message.StartsWith("IDX10501: Signature validation failed.");
                //Assert
                Assert.True(isInvalidSignatureError);
            }
        }

        private Claim[] GetClaims(string subClaim = null)
        {
            subClaim ??= _configuration["CTSDataHolderBrandId"];

            return new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subClaim),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
        }
    }
}
