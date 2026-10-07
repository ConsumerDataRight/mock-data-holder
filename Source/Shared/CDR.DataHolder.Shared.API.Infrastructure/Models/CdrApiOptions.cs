using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace CDR.DataHolder.Shared.API.Infrastructure.Models
{
    public class CdrApiOptions
    {
        private const string Id = @"[^\/]+"; // any value other than a /
        private const string BasePathAdmin = @"\/cds-au\/v1\/admin";
        private const string BasePathCommon = @"\/cds-au\/v1\/common";
        private const string BasePathEnergy = @"\/cds-au\/v1\/energy";
        private const string BasePathBanking = @"\/cds-au\/v1\/banking";

        private static readonly CdrApiEndpointVersionOptions[] _supportedApiVersionsAdmin =
            [
                new CdrApiEndpointVersionOptions(@$"{BasePathAdmin}\/metrics", true, 5),
            ];

        private static readonly CdrApiEndpointVersionOptions[] _supportedApiVersionsCommon =
            [
                new CdrApiEndpointVersionOptions(@$"{BasePathCommon}\/customer", true, 1),
            ];

        private static readonly CdrApiEndpointVersionOptions[] _supportedApiVersionsBanking =
            [
                new CdrApiEndpointVersionOptions(@$"{BasePathBanking}\/accounts", true, 2),
                new CdrApiEndpointVersionOptions(@$"{BasePathBanking}\/accounts\/{Id}\/transactions", true, 1),
            ];

        private static readonly CdrApiEndpointVersionOptions[] _supportedApiVersionsEnergy =
            [
                new CdrApiEndpointVersionOptions(@$"{BasePathEnergy}\/accounts", true, 2),
                new CdrApiEndpointVersionOptions(@$"{BasePathEnergy}\/accounts\/{Id}\/concessions", true, 1),
            ];

        public List<CdrApiEndpointVersionOptions> EndpointVersionOptions { get; } =
            [
                .. _supportedApiVersionsAdmin,
                .. _supportedApiVersionsCommon,
                .. _supportedApiVersionsBanking,
                .. _supportedApiVersionsEnergy
            ];

        public string DefaultVersion { get; set; } = "1";

        public CdrApiEndpointVersionOptions? GetApiEndpointVersionOption(PathString path)
        {
            foreach (var supportedApi in this.EndpointVersionOptions.OrderByDescending(v => v.Path.Length))
            {
                var regEx = new System.Text.RegularExpressions.Regex(supportedApi.Path, System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
                if (regEx.IsMatch(path))
                {
                    return supportedApi;
                }
            }

            return null;
        }
    }
}
