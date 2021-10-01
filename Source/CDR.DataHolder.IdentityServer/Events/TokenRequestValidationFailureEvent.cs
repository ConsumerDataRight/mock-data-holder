using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class TokenRequestValidationFailureEvent : ValidationFailureEvent
    {
        public TokenRequestValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, ClientAssertion, "Bad token request", 99300, message)
        {
        }
    }
}