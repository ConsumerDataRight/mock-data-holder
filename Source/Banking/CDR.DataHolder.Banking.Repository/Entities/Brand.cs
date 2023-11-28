namespace CDR.DataHolder.Banking.Repository.Entities
{
    public class Brand : Shared.Repository.Entities.Brand
	{
		public LegalEntity LegalEntity { get; set; } = new LegalEntity();
	}
}
