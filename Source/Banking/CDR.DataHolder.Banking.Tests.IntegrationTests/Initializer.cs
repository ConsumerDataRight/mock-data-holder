#undef DEPRECATED_REMOVAL_OF_IDENTITYSERVER_TESTS
#if DEPRECATED_REMOVAL_OF_IDENTITYSERVER_TESTS

#if RELEASE
using System.IO;
using Microsoft.Extensions.Configuration;
#endif
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("CDR.DataHolder.Banking.Tests.IntegrationTests.Initializer", "CDR.DataHolder.Banking.Tests.IntegrationTests")]

namespace CDR.DataHolder.Banking.Tests.IntegrationTests
{
    public class Initializer : XunitTestFramework
    {
        public Initializer(IMessageSink messageSink)
          : base(messageSink)
        {

#if RELEASE
            var configuration =
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.Release.json", true)
                .AddEnvironmentVariables()
                .Build();
#endif
        }

        public new void Dispose()
        {
            // Place tear down code here
            base.Dispose();
        }
    }
}

#endif