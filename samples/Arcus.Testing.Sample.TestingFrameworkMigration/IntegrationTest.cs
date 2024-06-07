using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Testing.Sample.TestingFrameworkMigration
{
    /// <summary>
    /// Base template for providing common and much-needed functionality to integration tests.
    /// </summary>
    public abstract class IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest" /> class.
        /// </summary>
        protected IntegrationTest(ITestOutputHelper outputWriter)
        {
            Config = TestConfig.Create();
            ScenarioFiles = ResourceDirectory.CurrentDirectory.WithSubDirectory("ScenarioFiles");
            Logger = new XunitTestLogger(outputWriter);
        }

        /// <summary>
        /// Gets the current configuration loaded for this integration test suite.
        /// </summary>
        protected TestConfig Config { get; }

        /// <summary>
        /// Gets the resource directory containing the scenario files for this integration test suite.
        /// </summary>
        protected ResourceDirectory ScenarioFiles { get; }

        /// <summary>
        /// Gets the logger to write diagnostic messages during the integration test execution.
        /// </summary>
        protected ILogger Logger { get; }
    }
}
