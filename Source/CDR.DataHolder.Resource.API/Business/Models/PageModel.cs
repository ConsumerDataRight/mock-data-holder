namespace CDR.DataHolder.Resource.API.Business.Models
{
    public class PageModel<TModel>
	{
		public TModel Data { get; set; }

		public Links Links { get; set; } = new Links();
		public MetaPaginated Meta { get; set; } = new MetaPaginated();
	}
}
