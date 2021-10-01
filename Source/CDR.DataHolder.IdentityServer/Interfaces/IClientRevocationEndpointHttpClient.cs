using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IClientRevocationEndpointHttpClient
    {
        public Task<HttpResponseMessage> PostToRevocationEndPoint(Dictionary<string, string> formValues, string bearerTokenJwt, Uri revocationUri);
    }
}
