using Microsoft.AspNetCore.Authorization;

namespace CDR.DataHolder.API.Infrastructure.Authorisation
{
    public class AccessTokenRequirement : IAuthorizationRequirement
    {
        public AccessTokenRequirement()
        {
        }
    }
}
