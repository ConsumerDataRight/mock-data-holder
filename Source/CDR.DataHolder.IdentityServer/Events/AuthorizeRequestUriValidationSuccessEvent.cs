using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class AuthorizeRequestUriValidationSuccessEvent : ValidationSuccessEvent
    {
        public AuthorizeRequestUriValidationSuccessEvent(string message = null)
            : base(Request, "Valid Authorize Par Request", 99601, message)
        {
        }
    }
}
