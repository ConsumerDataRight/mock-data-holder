using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Serialization
{
    public class ScopeValidatorLite
    {
        public Resources RequestedResources { get; set; }

        public Resources GrantedResources { get; set; }
    }
}
