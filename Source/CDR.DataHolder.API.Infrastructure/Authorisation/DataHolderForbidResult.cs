using CDR.DataHolder.API.Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.API.Infrastructure.Authorization
{
	public class DataHolderForbidResult : ObjectResult
	{
		public DataHolderForbidResult(ResponseErrorList errorList) : base(errorList)
		{
			this.StatusCode = StatusCodes.Status403Forbidden;
		}

		public DataHolderForbidResult(Error error) : this(new ResponseErrorList(error))
		{
		}
	}
}
