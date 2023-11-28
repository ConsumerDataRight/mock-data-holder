namespace CDR.DataHolder.Shared.Business.Models
{
    public class LinksPaginated : Links
    {
        /// <summary>URI to the first page of this set. Mandatory if this response is not the first page</summary>
        public Uri? First { get; set; }

        /// <summary>URI to the last page of this set. Mandatory if this response is not the last page</summary>
        public Uri? Last { get; set; }

        /// <summary>URI to the next page of this set. Mandatory if this response is not the last page</summary>
        public Uri? Next { get; set; }

        /// <summary>URI to the previous page of this set. Mandatory if this response is not the first page</summary>
        public Uri? Prev { get; set; }
    }
}
