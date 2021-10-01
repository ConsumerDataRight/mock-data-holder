using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class PushedAuthorizationRequestValidationSuccessEvent : ValidationSuccessEvent
    {
        public PushedAuthorizationRequestValidationSuccessEvent(string message = null)
            : base(Request, "Valid Pushed Authorization Request", 99133, message)
        {
        }
    }
}
