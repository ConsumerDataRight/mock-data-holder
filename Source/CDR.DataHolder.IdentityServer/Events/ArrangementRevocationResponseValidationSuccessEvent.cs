using static CDR.DataHolder.IdentityServer.Events.CustomEventCategories;

namespace CDR.DataHolder.IdentityServer.Events
{
    public class ArrangementRevocationResponseValidationSuccessEvent : ValidationSuccessEvent
    {
        public ArrangementRevocationResponseValidationSuccessEvent(string message = null)
            : base(Response, "Valid Arrangement revocation response", 91401, message)
        {
        }
    }
}
