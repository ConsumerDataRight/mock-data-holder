using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class MtlsValidationFailureEvent : ValidationFailureEvent
    {
        public MtlsValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, MTLS, "Invalid MTLS", 99100, message)
        {
        }
    }
}