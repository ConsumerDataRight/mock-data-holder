using System.Linq;
using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CDR.DataHolder.Shared.API.Infrastructure.Middleware
{

    public static class ModelStateErrorMiddleware
    {
        public static IActionResult ExecuteResult(ActionContext context)
        {
            var modelStateEntries = context.ModelState.Where(e => e.Value?.Errors.Count > 0).ToArray();

            var responseErrorList = new ResponseErrorList();

            if (modelStateEntries.Any())
            {
                foreach (var modelStateEntry in modelStateEntries)
                {
                    foreach (var modelStateError in modelStateEntry.Value!.Errors)
                    {
                        try
                        {
                            var error = JsonConvert.DeserializeObject<Error>(modelStateError.ErrorMessage);
                            if(error != null)
                            {
                                responseErrorList.Errors.Add(error);
                            }
                        }
                        catch
                        {
                            // This is for default and unhandled model errors.
                            responseErrorList.AddInvalidField($"The {modelStateEntry.Key} field is not valid"); //TODO: Inconsistent with standard, update when possible
                        }
                    }
                }
            }

            return new BadRequestObjectResult(responseErrorList);
        }
    }
}
