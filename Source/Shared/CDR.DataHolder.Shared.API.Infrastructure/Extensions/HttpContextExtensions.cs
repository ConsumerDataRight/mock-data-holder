using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using static CDR.DataHolder.Shared.API.Infrastructure.Constants;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Extracts api version from request headers.
        /// </summary>
        /// <param name="httpContext">Current HttpContext.</param>
        /// <returns>The Api version or null if not found.</returns>
        public static string? ApiVersion(this HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue(CustomHeaders.ApiVersionHeaderKey, out StringValues apiVersion))
            {
                return apiVersion;
            }

            return null;
        }

        public static string? LastPathSegment(this HttpContext httpContext)
        {
            if (!httpContext.Request.Path.HasValue)
            {
                return string.Empty;
            }

            var pathValues = httpContext.Request.Path!.Value.Split('/');
            return pathValues[pathValues.Length - 1];
        }

        public static string? FirstPathSegment(this HttpContext httpContext)
            => httpContext.Request.Path.HasValue
                ? Array.Find(httpContext.Request.Path.Value.Split("/"), x => x.IsPresent())
                : null;
    }
}
