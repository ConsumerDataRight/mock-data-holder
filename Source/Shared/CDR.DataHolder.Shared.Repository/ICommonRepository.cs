namespace CDR.DataHolder.Shared.Repository
{
    public interface ICommonRepository
    {
        Task<Domain.Entities.Customer?> GetCustomer(Guid customerId);
        Task<Domain.Entities.Customer?> GetCustomerByLoginId(string loginId);
        Task<bool> CanAccessAccount(string accountId);
    }
}
