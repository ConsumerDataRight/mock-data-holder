using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("IntegrationTests")]
    [TestCaseOrderer("CDR.DataHolder.Energy.Tests.IntegrationTests.XUnit.Orderers.AlphabeticalOrderer", "CDR.DataHolder.Energy.Tests.IntegrationTests")]
    abstract public class BaseTest : SharedBaseTest
    {
        protected BaseTest(ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config) : base(testOutputHelperAccessor, config)
        {
        }
    }
}
