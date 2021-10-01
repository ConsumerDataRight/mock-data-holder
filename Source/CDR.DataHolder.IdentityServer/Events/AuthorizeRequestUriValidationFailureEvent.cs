using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class AuthorizeRequestUriValidationFailureEvent : ValidationFailureEvent
    {
        public AuthorizeRequestUriValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Request, "Invalid Authorize Par Request", 99600, message)
        {
        }
    }
}
