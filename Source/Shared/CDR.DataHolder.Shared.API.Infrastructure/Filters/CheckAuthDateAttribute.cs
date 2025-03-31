using CDR.DataHolder.Shared.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Globalization;

namespace CDR.DataHolder.Shared.Resource.API.Business.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class CheckAuthDateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Get x-fapi-auth-date from request header
            var authDateValue = context.HttpContext.Request.Headers["x-fapi-auth-date"];
            if (authDateValue.Count == 0)
            {
                context.Result = new BadRequestObjectResult(new ResponseErrorList().AddMissingRequiredHeader("x-fapi-auth-date"));
            }

            if (authDateValue.Count > 0 &&
                !DateTime.TryParseExact(authDateValue, CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern, CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None, out DateTime _))
            {
                context.Result = new BadRequestObjectResult(new ResponseErrorList().AddInvalidHeader("x-fapi-auth-date"));
            }

            base.OnActionExecuting(context);
        }
    }
}
