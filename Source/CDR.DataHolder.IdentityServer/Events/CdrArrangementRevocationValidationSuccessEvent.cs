using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class CdrArrangementRevocationValidationSuccessEvent : ValidationSuccessEvent
    {
        public CdrArrangementRevocationValidationSuccessEvent(string message = null)
            : base(Request, "Valid CDR Arrangement Revocation Request", 99123, message)
        {
        }
    }
}
