using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class ArrangementRevocationResponseValidationFailureEvent : ValidationFailureEvent
    {
        public ArrangementRevocationResponseValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Response, "Bad arrangement revocation response", 91400, message)
        {
        }
    }
}
