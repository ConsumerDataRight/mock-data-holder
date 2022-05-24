using IdentityServer4.Validation;
namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class ValidatedAuthorizeRequestExtensions
    {
        public static bool IsPkce(this ValidatedAuthorizeRequest authorizationRequest)
        {
            return !string.IsNullOrEmpty(authorizationRequest.CodeChallenge) && !string.IsNullOrEmpty(authorizationRequest.CodeChallengeMethod);
        }
    }
}