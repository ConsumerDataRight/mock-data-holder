using CDR.DataHolder.Resource.API.Business.Models;

namespace CDR.DataHolder.Resource.API.Business.Responses
{
	public class ResponseCommonCustomer
	{
		public CustomerModel Data { get; set; }

		public Links Links { get; set; } = new Links();
		public Meta Meta { get; private set; } = new Meta();
	}
}
