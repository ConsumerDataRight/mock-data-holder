using System.Collections.Specialized;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IPushedAuthorizationRequestService
    {
        public Task<PushedAuthorizationResult> ProcessAuthoriseRequest(NameValueCollection parameters);
    }
}
