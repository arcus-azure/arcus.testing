using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arcus.Testing.Sample.TestingFrameworkMigration
{
    /// <summary>
    /// Base template for providing common and much-needed functionality to integration tests for the MSTest testing framework.
    /// </summary>
    public abstract class MSTestIntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MSTestIntegrationTest" /> class.
        /// </summary>
        protected MSTestIntegrationTest()
        {
            Config = TestConfig.Create();
            ScenarioFiles = ResourceDirectory.CurrentDirectory.WithSubDirectory("ScenarioFiles");
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
        protected ILogger Logger => new MSTestLogger(TestContext);

        /// <summary>
        /// Gets the test context for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }
    }
}
