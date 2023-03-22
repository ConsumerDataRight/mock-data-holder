using System.Linq;
using CDR.DataHolder.API.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.API.Infrastructure.Middleware
{

    public static class ModelStateErrorMiddleware
    {
        public static IActionResult ExecuteResult(ActionContext context)
        {
            var modelStateEntries = context.ModelState.Where(e => e.Value.Errors.Count > 0).ToArray();

            var responseErrorList = new ResponseErrorList();

            if (modelStateEntries.Any())
            {
                foreach (var modelStateEntry in modelStateEntries)
                {
                    foreach (var modelStateError in modelStateEntry.Value.Errors)
                    {
                        responseErrorList.Errors.Add(Error.InvalidField($"The {modelStateEntry.Key} field is not valid"));
                    }
                }
            }

            return new BadRequestObjectResult(responseErrorList);
        }
    }
}
