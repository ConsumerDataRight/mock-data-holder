using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public interface IIntrospectionRequestValidator
    {
        Task<IEnumerable<ValidationResult>> ValidateAsync(IntrospectionRequest request, HttpContext context, Client client);

        Task<IEnumerable<ValidationResult>> ValidateClientAssertionAsync(HttpContext context, Client client);
    }
}