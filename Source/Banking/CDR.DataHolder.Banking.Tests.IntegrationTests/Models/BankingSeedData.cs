using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;

namespace CDR.DataHolder.Banking.Tests.IntegrationTests.Models
{
    public class BankingSeedData
    {
        public List<Customer> Customers { get; set; } = new List<Customer>();

        public List<LegalEntity> LegalEntities { get; set; } = new List<LegalEntity>();
    }
}
