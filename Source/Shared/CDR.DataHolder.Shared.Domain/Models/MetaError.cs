namespace CDR.DataHolder.Shared.Domain.Models
{
    public class MetaError
    {
        public MetaError(string urn)
        {
            Urn = urn;
        }

        public string Urn { get; set; }
    }
}
