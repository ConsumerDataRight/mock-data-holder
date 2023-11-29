using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CDR.DataHolder.Shared.Resource.API.Business.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class CheckScopeAttribute : ActionFilterAttribute
    {
        private readonly string _scope;
        private const string SCOPE_CLAIM_NAME = "scope";

        public CheckScopeAttribute(string scope)
        {
            _scope = scope;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            // If user does not have the scope claim, get out of here
            if (!user.HasClaim(c => c.Type == SCOPE_CLAIM_NAME))
            {
                context.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
            }
            else
            {
                // Split the scopes string into an array
                var scopes = user.FindAll(c => c.Type == SCOPE_CLAIM_NAME).Select(c=>c.Value);

                // Succeed if the scope array contains the required scope
                if (!scopes.Any(s => s == _scope))
                {
                    context.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
