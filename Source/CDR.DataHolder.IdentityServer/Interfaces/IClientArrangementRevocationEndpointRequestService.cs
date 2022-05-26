using CDR.DataHolder.IdentityServer.Models;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IClientArrangementRevocationEndpointRequestService
    {
        public Task<DataRecipientRevocationResult> SendRevocationRequest(string cdrArrangementId, bool useJwt = false);
    }
}
