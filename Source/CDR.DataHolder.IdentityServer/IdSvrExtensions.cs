using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer
{
    public static class IdSvrExtensions
    {
        public static IIdentityServerBuilder AddCertificateSigningCredential(this IIdentityServerBuilder builder, IConfiguration configuration)
        {
            var filePath = configuration["SigningCertificate:Path"];
            var pwd = configuration["SigningCertificate:Password"];
            var cert = new X509Certificate2(filePath, pwd);
            var certificateVersionSecurityKey = new X509SecurityKey(cert);
            var credentials = new SigningCredentials(certificateVersionSecurityKey, SecurityAlgorithms.RsaSsaPssSha256);

            builder.AddSigningCredential(credentials);

            return builder;
        }
    }
}
