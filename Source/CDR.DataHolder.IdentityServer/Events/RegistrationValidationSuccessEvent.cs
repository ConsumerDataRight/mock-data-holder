using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class RegistrationValidationSuccessEvent : ValidationSuccessEvent
    {
        public RegistrationValidationSuccessEvent(string message = null)
            : base(Claims, "Valid Registration Claims", 99001, message)
        {
        }
    }
}
