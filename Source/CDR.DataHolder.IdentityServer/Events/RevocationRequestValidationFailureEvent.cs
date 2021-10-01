using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class RevocationRequestValidationFailureEvent : ValidationFailureEvent
    {
        public RevocationRequestValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, ClientAssertion, "Bad revocation request", 99300, message)
        {
        }
    }
}
