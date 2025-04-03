namespace CDR.DataHolder.Shared.Business.Models
{
    public class PageModel<TModel>
    {
        public TModel Data { get; set; } = default!;

        public Links Links { get; set; } = new Links();

        public MetaPaginated Meta { get; set; } = new MetaPaginated();
    }
}
