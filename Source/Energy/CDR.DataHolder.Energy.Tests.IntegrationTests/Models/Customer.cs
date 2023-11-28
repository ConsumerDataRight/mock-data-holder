using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using System.Collections.Generic;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class Customer
    {
        public string CustomerID { get; set; } = null!;
        public string? CustomerUType { get; set; }
        public string LoginId { get; set; } = null!;
        public Person? Person { get; set; }
        public List<Account>? Accounts { get; set; }
    }
}
