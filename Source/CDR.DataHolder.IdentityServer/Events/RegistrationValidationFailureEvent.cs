using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class RegistrationValidationFailureEvent : ValidationFailureEvent
    {
        public RegistrationValidationFailureEvent(ValidationCheck check, string message = null)
            : base(check, Claims, "Invalid Registration Claims", 99000, message)
        {
        }
    }
}
