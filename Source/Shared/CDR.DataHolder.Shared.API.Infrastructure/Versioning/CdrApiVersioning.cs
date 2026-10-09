using System;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CDR.DataHolder.Shared.API.Infrastructure.Models;

using Microsoft.Extensions.DependencyInjection;

namespace CDR.DataHolder.Shared.API.Infrastructure.Versioning
{
    public static class CdrApiVersioning
    {
        /// <summary>
        /// Add CDR-specific API versioning and API explorer.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="setupVersioningAction">Additional setup actions to configure <see cref="ApiVersioningOptions"/> beyond the default settings.</param>
        /// <param name="setupExplorerAction">Additional setup actions to configure <see cref="ApiExplorerOptions"/> beyond the default settings.</param>
        /// <returns>The versioning builder.</returns>
        public static IApiVersioningBuilder AddCdrApiVersioning(this IServiceCollection services, Action<ApiVersioningOptions>? setupVersioningAction = null, Action<ApiExplorerOptions>? setupExplorerAction = null)
        {
            return services
                .AddApiVersioning(options =>
                {
                    options.ApiVersionReader = new CdrVersionReader(new CdrApiOptions()); // uses default options atm
                    setupVersioningAction?.Invoke(options);
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = Constants.Versioning.GroupNameFormat;
                    setupExplorerAction?.Invoke(options);
                });
        }
    }
}
