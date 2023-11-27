using Microsoft.AspNetCore.Authorization;

namespace CDR.DataHolder.Shared.API.Infrastructure.Authorisation
{
    public class AccessTokenRequirement : IAuthorizationRequirement
    {
        public AccessTokenRequirement()
        {
        }
    }
}
