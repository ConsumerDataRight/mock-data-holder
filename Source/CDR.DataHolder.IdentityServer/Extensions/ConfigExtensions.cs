using CDR.DataHolder.IdentityServer.Configuration;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class ConfigExtensions
    {
        public static FapiComplianceLevel FapiComplianceLevel(this IConfiguration config)
        {
            return config.GetValue<FapiComplianceLevel>("FapiComplianceLevel", CdsConstants.FapiComplianceLevel.Fapi1Phase1);
        }

        public static IEnumerable<string> GetValidAudiences(this IConfiguration config)
        {
            return new List<string>
            {
                config["IssuerUri"],
                config["AuthorizeUri"],
                config["TokenUri"],
                config["IntrospectionUri"],
                config["UserinfoUri"],
                config["RegisterUri"],
                config["ParUri"],
                config["ArrangementRevocationUri"],
                config["RevocationUri"],
            };
        }

        public static string GetBaseUri(this IConfiguration config)
        {
            var baseUri = config.GetValue<string>(Constants.ConfigurationKeys.BaseUri, "");
            if (!string.IsNullOrEmpty(baseUri))
            {
                return baseUri;
            }

            return config[Constants.ConfigurationKeys.IssuerUri];
        }

        public static string GetBasePath(this IConfiguration config)
        {
            return config.GetValue<string>(Constants.ConfigurationKeys.BasePath);
        }

        public static string EnsurePath(this IConfiguration config, string path)
        {
            if (path.StartsWith("https://"))
            {
                return path;
            }

            var basePath = config.GetBasePath();
            if (string.IsNullOrEmpty(basePath))
            {
                return path;
            }

            if (path.StartsWith(basePath, System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return string.Concat(basePath, path);
        }
    }
}
