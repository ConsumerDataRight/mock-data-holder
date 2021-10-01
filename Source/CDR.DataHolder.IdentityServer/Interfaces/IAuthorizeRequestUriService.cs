using IdentityServer4.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IAuthorizeRequestUriService
    {
        Task<IEndpointResult> ProcessAsync(string request_uri_key, string client_id, NameValueCollection nameValueCollection);
    }
}
