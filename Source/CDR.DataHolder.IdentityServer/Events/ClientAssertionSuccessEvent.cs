using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class ClientAssertionSuccessEvent : ValidationSuccessEvent
    {
        public ClientAssertionSuccessEvent(string message = null)
            : base(ClientAssertion, "Valid Client Assertion", 99201, message)
        {
        }
    }
}