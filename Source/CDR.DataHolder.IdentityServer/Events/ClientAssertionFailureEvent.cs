using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class ClientAssertionFailureEvent : ValidationFailureEvent
    {
        public ClientAssertionFailureEvent(ValidationCheck check, string message = null)
            : base(check, ClientAssertion, "Invalid Client Assertion", 99200, message)
        {
        }
    }
}