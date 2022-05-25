using System.Security.Cryptography.X509Certificates;
using CDR.DataHolder.IdentityServer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer
{
    public static class IdSvrExtensions
    {
        public static IIdentityServerBuilder AddCertificateSigningCredentials(
            this IIdentityServerBuilder builder, 
            IConfiguration configuration)
        {
            var securityService = new SecurityService(configuration);

            foreach (var cred in securityService.SigningCredentials)
            {
                builder.AddSigningCredential(cred);
            }

            return builder;
        }
    }
}
