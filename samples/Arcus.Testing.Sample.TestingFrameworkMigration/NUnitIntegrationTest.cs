using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Arcus.Testing.Sample.TestingFrameworkMigration
{
    /// <summary>
    /// Base template for providing common and much-needed functionality to integration tests in for the NUnit testing framework.
    /// </summary>
    public abstract class NUnitIntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XunitIntegrationTest" /> class.
        /// </summary>
        protected NUnitIntegrationTest()
        {
            Config = TestConfig.Create();
            ScenarioFiles = ResourceDirectory.CurrentDirectory.WithSubDirectory("ScenarioFiles");
            Logger = new NUnitTestLogger(TestContext.Out, TestContext.Error);
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