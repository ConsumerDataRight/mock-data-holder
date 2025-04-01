namespace CDR.DataHolder.Banking.Repository.Entities
{
    public class Person : Shared.Repository.Entities.Person
    {
        public virtual Customer Customer { get; set; } = null!;
    }
}
