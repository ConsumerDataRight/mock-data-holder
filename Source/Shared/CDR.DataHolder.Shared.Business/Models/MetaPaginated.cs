namespace CDR.DataHolder.Shared.Business.Models
{
    public class MetaPaginated : Meta
    {
        public int? TotalRecords { get; set; }

        public int? TotalPages { get; set; }
    }
}
