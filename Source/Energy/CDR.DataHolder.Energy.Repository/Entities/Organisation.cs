namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class Organisation : Shared.Repository.Entities.Organisation
	{
        public virtual Customer Customer { get; set; } = new Customer();
    }
}