using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IClientArrangementRevocationEndpointHttpClient
    {
        public Task<(HttpStatusCode? Status, string Detail)> PostToArrangementRevocationEndPoint(Dictionary<string, string> formValues, string bearerTokenJwt, Uri arrangementRevocationUri);
    }
}
