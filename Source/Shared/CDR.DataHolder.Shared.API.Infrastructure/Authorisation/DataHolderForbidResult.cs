using CDR.DataHolder.Shared.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.Shared.API.Infrastructure.Authorization
{
    public class DataHolderForbidResult : ObjectResult
    {
        public DataHolderForbidResult(ResponseErrorList errorList)
            : base(errorList)
        {
            this.StatusCode = StatusCodes.Status403Forbidden;
        }

        public DataHolderForbidResult(Error error)
            : this(new ResponseErrorList(error))
        {
        }
    }
}
