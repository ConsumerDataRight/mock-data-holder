using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.IdentityServer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Controllers
{
    /// <summary>
    /// This is for internal admin related functionalities. Once we have a admin UI/area, these will be moved
    /// </summary>
    [Route("admin")]
    public class AdminController : Controller
    {
		private readonly IClientArrangementRevocationEndpointRequestService _revocationEndpointRequestService;


        public AdminController(
            IClientArrangementRevocationEndpointRequestService revocationEndpointRequestService)
		{
			_revocationEndpointRequestService = revocationEndpointRequestService;
        }

        /// <summary>
        /// This controller method is provided to trigger an arrangement revocation at a data recipient.
        /// Normally, this would be done from the DH dashboard.  
        /// However, until a dashboard is in place this method can be used to trigger a request.
        /// </summary>
        /// <returns>IActionResult</returns>
        /// <remarks>
        /// Note: this controller action would not be implemented in a production system and is provided for testing purposes.
        /// </remarks>
        [HttpGet]
        [Route("dr/revoke-arrangement/{cdrArrangementId}")]
        public async Task<IActionResult> TriggerDataRecipientArrangementRevocation(string cdrArrangementId)
        {
            return await InvokeDataRecipientArrangementRevocation(cdrArrangementId, false);
        }

        /// <summary>
        /// This controller method is provided to trigger an arrangement revocation at a data recipient.
        /// Normally, this would be done from the DH dashboard.  
        /// However, until a dashboard is in place this method can be used to trigger a request.
        /// </summary>
        /// <returns>IActionResult</returns>
        /// <remarks>
        /// Note: this controller action would not be implemented in a production system and is provided for testing purposes.
        /// </remarks>
        [HttpGet]
        [Route("dr/revoke-arrangement-jwt/{cdrArrangementId}")]
        public async Task<IActionResult> TriggerDataRecipientArrangementRevocationByJwt(string cdrArrangementId)
        {
            return await InvokeDataRecipientArrangementRevocation(cdrArrangementId, true);
        }

        private async Task<IActionResult> InvokeDataRecipientArrangementRevocation(string cdrArrangementId, bool useJwt = false)
        {
            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                return new UnprocessableEntityObjectResult(new ResponseErrorList(Error.NotFound($"Invalid {CdsConstants.StandardClaims.CDRArrangementId}")));
            }

            var result = await _revocationEndpointRequestService.SendRevocationRequest(cdrArrangementId, useJwt);
            if (result.Status != System.Net.HttpStatusCode.NoContent)
            {
                return new UnprocessableEntityObjectResult(result);
            }

            return NoContent();
        }
    }
}
