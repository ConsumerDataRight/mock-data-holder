namespace CDR.DataHolder.Shared.Business.Models
{
    public class Links
    {
        /// <summary>Fully qualified link to this API call.</summary>
        // [Required(AllowEmptyStrings = true)]
        public Uri Self { get; set; } = null!;
    }
}
