using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Events;
using IdentityServer4.Services;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomEventSink : IEventSink
    {
        private static readonly string[] _presistingEventCategories = new string[]
        {
            EventCategories.Token,
            CustomEventCategories.Claims,
            CustomEventCategories.ClientAssertion,
            CustomEventCategories.MTLS,
            CustomEventCategories.TokenRequest,
            CustomEventCategories.Request,
            CustomEventCategories.Response,
            EventCategories.Authentication,
        };

        //private readonly ITestOutcomeService _testOutcome;

        //public CustomEventSink(ITestOutcomeFactory testOutcomeFactory)
        public CustomEventSink()
        {
            //_testOutcome = testOutcomeFactory.GetTestOutcomeService();
        }

        public async Task PersistAsync(Event evt)
        {
            //if (_presistingEventCategories.Contains(evt.Category))
            //{
            //    if (evt.EventType == EventTypes.Failure)
            //    {
            //        await _testOutcome.Fail(evt);
            //    }
            //    else if (evt.EventType == EventTypes.Success)
            //    {
            //        await _testOutcome.Pass(evt);
            //    }
            //}
        }
    }
}