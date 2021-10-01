using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class RevocationResponseValidationSuccessEvent : ValidationSuccessEvent
    {
        public RevocationResponseValidationSuccessEvent(string message = null)
            : base(Response, "valid revocation response", 99401, message)
        {
        }
    }
}
