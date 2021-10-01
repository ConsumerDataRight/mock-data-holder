using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Validation;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IPushedAuthorizationRequestValidator
    {
        Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal subject = null);
    }
}
