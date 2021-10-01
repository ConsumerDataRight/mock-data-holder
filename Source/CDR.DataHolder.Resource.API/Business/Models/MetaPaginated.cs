namespace CDR.DataHolder.Resource.API.Business.Models
{
	public class MetaPaginated: Meta
	{
		public int? TotalRecords { get; set; }
		public int? TotalPages { get; set; }
	}
}
