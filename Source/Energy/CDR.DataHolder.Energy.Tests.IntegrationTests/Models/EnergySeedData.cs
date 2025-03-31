using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class EnergySeedData
    {
        public List<Customer> Customers { get; set; } = new List<Customer>();

        public List<Plan> Plans { get; set; } = new List<Plan>();

        public List<LegalEntity> LegalEntities { get; set; } = new List<LegalEntity>();
    }
}
