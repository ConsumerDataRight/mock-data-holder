using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using Microsoft.AspNetCore.Authentication;

namespace CDR.DataHolder.Resource.API.Business.Services
{
	public class TokenTransformService : IClaimsTransformation
	{
		private readonly IIdPermanenceManager _idPermanenceManager;

		public TokenTransformService(IIdPermanenceManager idPermanenceManager)
		{
			_idPermanenceManager = idPermanenceManager;
		}

		public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
		{
			// Clone current identity
			var clone = principal.Clone();
			var newIdentity = (ClaimsIdentity)clone.Identity;

			var subClaim = newIdentity.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);
			if (subClaim == null)
			{
				return Task.FromResult(principal);
			}

			// Perform PPID decryption on sub claim
			var subParam = new SubPermanenceParameters()
			{
				SectorIdentifierUri = newIdentity.FindFirst(Constants.TokenClaimTypes.SectorIdentifier)?.Value,
				SoftwareProductId = newIdentity.FindFirst(Constants.TokenClaimTypes.SoftwareId)?.Value
			};
			var decryptedSubValue = _idPermanenceManager.DecryptSub(subClaim.Value, subParam);
			newIdentity.RemoveClaim(subClaim);
			newIdentity.AddClaim(new Claim(
				subClaim.Type,
				decryptedSubValue,
				subClaim.ValueType));

			// Perform PPID decryption on account ids
			var accountClaims = newIdentity.FindAll(c => c.Type == Constants.TokenClaimTypes.AccountId).ToArray();
			var idParam = new IdPermanenceParameters()
			{
				CustomerId = decryptedSubValue,
				SoftwareProductId = newIdentity.FindFirst(Constants.TokenClaimTypes.SoftwareId)?.Value
			};
			foreach (var claim in accountClaims)
			{
				newIdentity.RemoveClaim(claim);
				newIdentity.AddClaim(new Claim(
					claim.Type,
					_idPermanenceManager.DecryptId(claim.Value, idParam),
					claim.ValueType));
			}

			return Task.FromResult(clone);
		}
	}
}
