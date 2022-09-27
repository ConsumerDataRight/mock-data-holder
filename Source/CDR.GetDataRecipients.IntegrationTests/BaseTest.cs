#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

#nullable enable

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

using CDR.GetDataRecipients.IntegrationTests.Fixtures;
using System.Configuration;

namespace CDR.GetDataRecipients.IntegrationTests
{
    class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
    {
        static int count = 0;

        public override void Before(MethodInfo methodUnderTest)
        {
            Console.WriteLine($"Test #{++count} - {methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }

    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("IntegrationTests")]
    [TestCaseOrderer("CDR.GetDataRecipients.IntegrationTests.XUnit.Orderers.AlphabeticalOrderer", "CDR.GetDataRecipients.IntegrationTests")]
    [DisplayTestMethodName]
    abstract public class BaseTest : IClassFixture<TestFixture>
    {      
        public static string AZUREFUNCTIONS_URL => Configuration["URL:AZUREFUNCTIONS"]
            ?? throw new ConfigurationErrorsException($"{nameof(AZUREFUNCTIONS_URL)} - configuration setting not found");

        static public string CONNECTIONSTRING_REGISTER_RW =>
            ConnectionStringCheck.Check(Configuration.GetConnectionString("Register_RW"));
        static public string CONNECTIONSTRING_MDH_RW =>
            ConnectionStringCheck.Check(Configuration.GetConnectionString("DataHolder_RW"));

        static private IConfigurationRoot? configuration;
        static public IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .Build();
                }

                return configuration;
            }
        }
    }
}