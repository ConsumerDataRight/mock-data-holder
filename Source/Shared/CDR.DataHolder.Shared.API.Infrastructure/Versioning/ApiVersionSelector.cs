using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using static CDR.DataHolder.Shared.API.Infrastructure.Constants;

namespace CDR.DataHolder.Shared.API.Infrastructure.Versioning
{
    public class ApiVersionSelector : IApiVersionSelector
    {
        private readonly ApiVersion _defaultVersion;
        private readonly Dictionary<string, int[]> _supportedApiVersions = new()
        {
            { @"\/cds-au\/v1\/admin\/metrics", new int[] { 3 } },
            { @"\/cds-au\/v1\/common\/customer", new int[] { 1 } },
            { @"\/cds-au\/v1\/energy\/accounts", new int[] { 1, 2 } },
            { @"\/cds-au\/v1\/energy\/accounts\/[A-Za-z0-9\-]*\/concessions", new int[] { 1 } },
        };

        public ApiVersionSelector(ApiVersioningOptions options, Dictionary<string, int[]>? overrideDefaultVersions = null)
        {
            _defaultVersion = options.DefaultApiVersion;           

            if (overrideDefaultVersions != null)
            {
                foreach (var kvp in overrideDefaultVersions)
                {
                    if (_supportedApiVersions.ContainsKey(kvp.Key))
                    {
                        // Update the value in _supportedApiVersions with the value from overrideDefaultVersions
                        _supportedApiVersions[kvp.Key] = kvp.Value; 
                    }
                    else
                    {
                        // If the key doesn't exist in overrideDefaultVersions, add it with the value from overrideDefaultVersions
                        _supportedApiVersions.Add(kvp.Key, kvp.Value); 
                    }
                }
            }           
        }

        public ApiVersion SelectVersion(HttpRequest request, ApiVersionModel model)
        {
            // Try and get x-v value from request header            
            if (!request.Headers.TryGetValue(CustomHeaders.ApiVersionHeaderKey, out var x_v))
            {
                // Raise an error                
                throw new MissingRequiredHeaderException(CustomHeaders.ApiVersionHeaderKey);
            }

            // x-v must be a positive integer.
            if (!int.TryParse(x_v, out int xvVersion) || xvVersion < 1)
            {
                // Raise an error.
                throw new InvalidVersionException(CustomHeaders.ApiVersionHeaderKey);
            }

            // If requested version is 1, then just return.
            if (xvVersion == 1)
            {
                return _defaultVersion;
            }

            // Check if the requested version is supported by the API.
            var apiVersions = GetApiVersions(request.Path);

            // Matching api was not found.
            if (!apiVersions.Any())
            {
                return _defaultVersion;
            }

            // Version match.
            if (apiVersions.Contains(xvVersion))
            {
                return new ApiVersion(xvVersion, 0);
            }

            // No matching version, so check if a x-min-v header has been provided.
            if (!request.Headers.ContainsKey(CustomHeaders.ApiMinVersionHeaderKey))
            {
                // For not supported higher version of x-v; without x-min-v below exception is thrown
                // x-min-v has not been provided, so throw an unsupported version error.
                throw new UnsupportedVersionException(apiVersions.Min(), apiVersions.Max());
            }

            // Check if the x-min-v is a positive integer.
            var x_min_v = request.Headers[CustomHeaders.ApiMinVersionHeaderKey];

            // x-min-v must be a positive integer.
            if (!int.TryParse(x_min_v, out int xvMinVersion) || xvMinVersion < 1)
            {
                // Raise an invalid error.
                throw new InvalidVersionException(CustomHeaders.ApiMinVersionHeaderKey);
            }

            // If x-min-v is greater than x-v then ignore it.
            if (xvMinVersion > xvVersion)
            {
                xvMinVersion = xvVersion;
            }

            // Find the largest supported version between x-min-v and x-v.
            var supportedVersions = apiVersions
                .Where(v => (v >= xvMinVersion && v <= xvVersion));

            // No supported versions were found.
            if (!supportedVersions.Any())
            {
                throw new UnsupportedVersionException(apiVersions.Min(), apiVersions.Max());
            }

            // Return the highest support version.
            return new ApiVersion(supportedVersions.OrderByDescending(v => v).Take(1).Single(), 0);
        }

        private IEnumerable<int> GetApiVersions(PathString path)
        {
            foreach (var supportedApi in _supportedApiVersions.OrderByDescending(v => v.Key.Length))
            {
                var regEx = new System.Text.RegularExpressions.Regex(supportedApi.Key);
                if (regEx.IsMatch(path))
                {
                    return supportedApi.Value;
                }
            }

            return Array.Empty<int>();
        }
    }
}
