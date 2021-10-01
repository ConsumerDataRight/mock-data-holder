using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class PushedAuthorizationRequestValidationFailureEvent : ValidationFailureEvent
    {
        public PushedAuthorizationRequestValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Request, "Invalid Pushed Authorization Request", 991434, message)
        {
        }
    }
}
