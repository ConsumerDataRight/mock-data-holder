using CDR.DataHolder.Shared.API.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CDR.DataHolder.Shared.API.Infrastructure.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PolicyAuthorizeAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly AuthorisationPolicy policy;

        public PolicyAuthorizeAttribute(AuthorisationPolicy policy)
        {
            this.policy = policy;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authorizationService = (IAuthorizationService)context.HttpContext.RequestServices.GetService(typeof(IAuthorizationService))!;
            var authorizationResult = await authorizationService.AuthorizeAsync(context.HttpContext.User, policy.ToString());

            bool validAccessToken = false;
            var ctxItems = context.HttpContext.Items;
            var accessTknItem = ctxItems.FirstOrDefault(g => g.Key.ToString() == "ValidAccessToken");
            if (accessTknItem.Key != null && accessTknItem.Value != null)
                validAccessToken = (bool)accessTknItem.Value;

            if (accessTknItem.Key != null && !validAccessToken)
            {
                context.Result = new DataHolderUnauthorizedResult();
            }
            else
            {
                if (authorizationResult.Succeeded)
                {
                    return;
                }
                if (authorizationResult.Failure!.FailedRequirements.Any(r => r.GetType() == typeof(MtlsRequirement)))
                {
                    context.Result = new DataHolderUnauthorizedResult(new ResponseErrorList(StatusCodes.Status401Unauthorized.ToString(), HttpStatusCode.Unauthorized.ToString(), "invalid_token"));
                    return;
                }
                context.Result = new DataHolderForbidResult(new ResponseErrorList("urn:au-cds:error:cds-all:Authorisation/InvalidConsent", "Consent Is Invalid", "The authorised consumer's consent is insufficient to execute the resource"));
            }
        }
    }
}