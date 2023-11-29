
using CDR.DataHolder.Common.Resource.API.Business.Models;

namespace CDR.DataHolder.Common.Resource.API.Business.Responses
{
	public class ResponseCommonCustomer
	{
		public CustomerModel Data { get; set; } = new CustomerModel();

		public Shared.Business.Models.Links Links { get; set; } = new Shared.Business.Models.Links();
		public Meta Meta { get; private set; } = new Meta();
	}
}
