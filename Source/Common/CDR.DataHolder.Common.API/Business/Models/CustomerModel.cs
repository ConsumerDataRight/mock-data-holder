namespace CDR.DataHolder.Common.Resource.API.Business.Models
{
	public class CustomerModel
    {
        public string CustomerUType { get; set; } = string.Empty;
        public CommonPerson? Person { get; set; }
        public CommonOrganisation? Organisation { get; set; }
    }
}
