using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class RequestValidationFailureEvent : ValidationFailureEvent
    {
        public RequestValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Request, "Invalid Request", 99007, message)
        {
        }
    }
}