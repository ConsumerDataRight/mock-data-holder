using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.IdentityServer.Controllers
{
    /// <summary>
    /// This is for internal admin related functionalities. Once we have a admin UI/area, these will be moved
    /// </summary>
    [Route("admin")]
    public class AdminController : Controller
    {
		private readonly IClientArrangementRevocationEndpointRequestService _revocationEndpointRequestService;
		private readonly IClientService _clientService;

		public AdminController(
            IClientArrangementRevocationEndpointRequestService revocationEndpointRequestService,
            IClientService clientService)
		{
			_revocationEndpointRequestService = revocationEndpointRequestService;
			_clientService = clientService;
		}

        /// <summary>
        /// DH initiated DR arrangement revocation.
        /// </summary>
        /// <param name="drClientId">Client ID of the DR</param>
        /// <param name="dhClientId">Client ID of the DH on register</param>
        /// <param name="arrangementId">CDR arrangement ID to be revokend</param>
        [HttpPost]
        [Route("dr/revoke-arrangement")]
        public async Task<IActionResult> RevokeDataResipientArrangementTest(
            [FromForm(Name = "client_id")] string drClientId,
            [FromForm(Name = "dh_client_id")] string dhClientId,
            [FromForm(Name = "cdr_arrangement_id")] string arrangementId,
            [FromForm(Name = "arrangement_revocation_uri")] string arrangementRevocationUri = null)
        {
            // Get the client details
            var client = await _clientService.FindClientById(drClientId);
            if (client == null)
            {
                return Unauthorized();
            }

            // Generate the private key jwt for the client authentication with the DR
            var recipientBaseUri = client.Claims?.Get("recipient_base_uri");
            var revocationUri = arrangementRevocationUri ?? client.Claims?.Get("revocation_uri");
            var jwtSecurityToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                claims: new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, dhClientId),
                    new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, System.Guid.NewGuid().ToString()),
                },
                issuer: dhClientId,
                audience: recipientBaseUri,
                expires: System.DateTime.UtcNow.AddMinutes(5));
            var signedBearerTokenJwt = await _revocationEndpointRequestService.GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            // Call the DR revocation endpoint
            var clientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(clientHandler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signedBearerTokenJwt);
            var content = new StringContent($"cdr_arrangement_id={arrangementId}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var response = await httpClient.PostAsync(revocationUri, content);
            if (response.IsSuccessStatusCode)
            {
                return NoContent();
            }

            return BadRequest();
        }
    }
}
