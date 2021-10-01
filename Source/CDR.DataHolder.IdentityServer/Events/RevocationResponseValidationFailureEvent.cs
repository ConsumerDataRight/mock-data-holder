using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class RevocationResponseValidationFailureEvent : ValidationFailureEvent
    {
        public RevocationResponseValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Response, "Bad revocation response", 99400, message)
        {
        }
    }
}
