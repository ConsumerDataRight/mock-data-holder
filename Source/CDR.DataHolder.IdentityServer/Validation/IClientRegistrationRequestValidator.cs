using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public interface IClientRegistrationRequestValidator
    {
        Task<IEnumerable<ValidationResult>> ValidateAsync(IClientRegistrationRequest request);
    }
}
