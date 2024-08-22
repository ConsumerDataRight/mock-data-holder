using CDR.DataHolder.Shared.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.Shared.API.Infrastructure.Authorization
{
    public class DataHolderUnauthorizedResult : ObjectResult
    {
        public DataHolderUnauthorizedResult(ResponseErrorList errorList) : base(errorList)
        {
            this.StatusCode = StatusCodes.Status401Unauthorized;
        }

        public DataHolderUnauthorizedResult() : base(null)
        {
            //Return 400 not 401
            this.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}