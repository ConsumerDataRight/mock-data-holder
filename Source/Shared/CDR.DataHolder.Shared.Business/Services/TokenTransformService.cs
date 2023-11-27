using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Infra = CDR.DataHolder.Shared.API.Infrastructure;

namespace CDR.DataHolder.Shared.Business.Services
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
            var newIdentity = (ClaimsIdentity?)clone.Identity;
            if (newIdentity == null)
            {
                return Task.FromResult(principal);
            }

            var subClaim = newIdentity.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);
            if (subClaim == null)
            {
                return Task.FromResult(principal);
            }

            // Perform PPID decryption on sub claim
            var subParam = new SubPermanenceParameters()
            {
                SectorIdentifierUri = newIdentity.FindFirst(Infra.Constants.TokenClaimTypes.SectorIdentifier)?.Value ?? string.Empty,
                SoftwareProductId = newIdentity.FindFirst(Infra.Constants.TokenClaimTypes.SoftwareId)?.Value ?? string.Empty
            };
            var decryptedSubValue = _idPermanenceManager.DecryptSub(subClaim.Value, subParam);
            newIdentity.RemoveClaim(subClaim);
            newIdentity.AddClaim(new Claim(
                subClaim.Type,
                decryptedSubValue,
                subClaim.ValueType));

            // Perform PPID decryption on account ids
            var accountClaims = newIdentity.FindAll(c => c.Type == Infra.Constants.TokenClaimTypes.AccountId).ToArray();
            var idParam = new IdPermanenceParameters()
            {
                CustomerId = decryptedSubValue,
                SoftwareProductId = newIdentity.FindFirst(Infra.Constants.TokenClaimTypes.SoftwareId)?.Value ?? string.Empty
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
