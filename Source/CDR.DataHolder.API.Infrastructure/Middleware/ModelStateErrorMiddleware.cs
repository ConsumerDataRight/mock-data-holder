using System.Linq;
using System.Net;
using CDR.DataHolder.API.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CDR.DataHolder.API.Infrastructure.Middleware
{

	public class ModelStateErrorMiddleware
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

                        //try
                        //{
                        //    var error = JsonConvert.DeserializeObject<Error>(modelStateError.ErrorMessage);
                        //    error.Detail = string.Format(error.Detail, modelStateEntry.Key);
                        //    responseErrorList.Errors.Add(error);
                        //}
                        //catch
                        //{
                        //    // This is for default and unhandled model errors.
                        //    responseErrorList.Errors.Add(Error.InvalidField($"The {modelStateEntry.Key} field is not valid"));
                        //}
                    }
                }
            }

            return new BadRequestObjectResult(responseErrorList);
        }
    }
}
