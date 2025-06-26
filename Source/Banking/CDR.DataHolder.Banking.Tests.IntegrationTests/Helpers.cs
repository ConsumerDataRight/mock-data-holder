using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using System.IdentityModel.Tokens.Jwt;

namespace CDR.DataHolder.Banking.Tests.IntegrationTests;

public static class Helpers
{
    /// <summary>
    /// Extract loginId (by decrypting "sub" claim).
    /// Note:
    /// Currently duplicated between DHB and DHE testing projects due to reliance on IdPermanence in Infrastructure project.
    /// Can resolve when we have a Nuget Package used across Implementation and Testing (currently only test automation package).
    /// </summary>
    public static void ExtractClaimsFromToken(string? accessToken, out string loginId, out string softwareProductId)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        softwareProductId = jwt.Claim("software_id").Value;

        // Decrypt sub to extract loginId
        var sub = jwt.Claim("sub").Value;
        loginId = IdPermanenceHelper.DecryptSub(
            sub,
            new SubPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                SectorIdentifierUri = Constants.SoftwareProducts.SoftwareProductSectorIdentifierUri,
            },
            Constants.IdPermanence.IdPermanencePrivateKey);
    }

    /// <summary>
    /// IdPermanence encryption
    /// Note:
    /// Currently duplicated between DHB and DHE testing projects due to reliance on IdPermanence in Infrastructure project.
    /// Can resolve when we have a Nuget Package used across Implementation and Testing (currently only test automation package).
    /// </summary>
    public static string IdPermanenceEncrypt(string plainText, string loginId, string softwareProductId)
    {
        loginId = loginId.ToLower();
        softwareProductId = softwareProductId.ToLower();

        var idParameters = new IdPermanenceParameters
        {
            SoftwareProductId = softwareProductId,
            CustomerId = loginId,
        };

        var encrypted = IdPermanenceHelper.EncryptId(plainText, idParameters, Constants.IdPermanence.IdPermanencePrivateKey);

        return encrypted;
    }
}
