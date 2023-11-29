namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class Transaction : Shared.Repository.Entities.Transaction
    {
        public virtual Account Account { get; set; } = new Account();
    }
}
