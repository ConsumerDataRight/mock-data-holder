using IdentityServer4.Events;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Events
{
    public abstract class ValidationFailureEvent : Event
    {
        protected ValidationFailureEvent(ValidationCheck check, string category, string name, int id, string message = null)
            : base(category, name, EventTypes.Failure, id, message)
        {
            Check = check;
        }

        protected ValidationFailureEvent(string category, string name, int id, string message = null)
            : base(category, name, EventTypes.Failure, id, message)
        {
        }

        public ValidationCheck Check { get; set; }
    }
}