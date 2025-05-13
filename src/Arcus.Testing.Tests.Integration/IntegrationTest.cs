using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration
{
    /// <summary>
    /// Base template for providing common and much-needed functionality to integration tests for the xUnit testing framework.
    /// </summary>
    public abstract class IntegrationTest
    {
        protected static readonly Faker Bogus = new();

        protected IntegrationTest(ITestOutputHelper outputWriter)
        {
            Logger = new XunitTestLogger(outputWriter);
            Configuration = TestConfig.Create(options =>
            {
                options.AddOptionalJsonFile("appsettings.default.json")
                       .AddOptionalJsonFile("appsettings.local.json");
            });
        }

        /// <summary>
        /// Gets the current configuration loaded for this integration test suite.
        /// </summary>
        protected TestConfig Configuration { get; }

        /// <summary>
        /// Gets the logger to write diagnostic messages during the integration test execution.
        /// </summary>
        protected ILogger Logger { get; }
    }
}