namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class Person : Shared.Repository.Entities.Person
    {
        public virtual Customer Customer { get; set; } = null!;
    }
}
