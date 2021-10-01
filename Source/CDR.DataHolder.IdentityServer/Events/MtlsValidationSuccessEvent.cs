using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class MtlsValidationSuccessEvent : ValidationSuccessEvent
    {
        public MtlsValidationSuccessEvent(string message = null)
            : base(MTLS, "Valid MTLS", 99101, message)
        {
        }
    }
}