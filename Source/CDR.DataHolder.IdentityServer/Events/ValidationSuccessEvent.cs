using IdentityServer4.Events;

namespace CDR.DataHolder.IdentityServer.Events
{
    public abstract class ValidationSuccessEvent : Event
    {
        protected ValidationSuccessEvent(string category, string name, int id, string message = null)
            : base(category, name, EventTypes.Success, id, message)
        {
        }
    }
}