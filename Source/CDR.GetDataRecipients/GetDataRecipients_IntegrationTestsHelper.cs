
#if INTEGRATION_TESTS

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CDR.GetDataRecipients
{
    public static class GetDataRecipients_IntegrationTestsHelper
    {
        // This http trigger is used the integration tests so that DATARECIPIENTS can be triggered on demand and not wait for timer
        [FunctionName("INTEGRATIONTESTS_DATARECIPIENTS")]
        public static async Task<IActionResult> INTEGRATIONTESTS_DATARECIPIENTS(
            // [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,            
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"{nameof(GetDataRecipients_IntegrationTestsHelper)}.{nameof(INTEGRATIONTESTS_DATARECIPIENTS)}");

            // Call the actual Azure function
            await GetDataRecipientsFunction.DATARECIPIENTS(null, log, context);

            return new OkResult();            
        }
    }
}

#endif