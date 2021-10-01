using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class CdrArrangementRevocationValidationFailureEvent : ValidationFailureEvent
    {
        public CdrArrangementRevocationValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Request, "Invalid CDR Arrangement Revocation Request", 991234, message)
        {
        }
    }
}
