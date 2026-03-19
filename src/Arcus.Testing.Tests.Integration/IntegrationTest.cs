using System;
using Arcus.Testing.Tests.Integration.Configuration;
using Azure.Core;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Testing.Tests.Integration
{
    /// <summary>
    /// Base template for providing common and much-needed functionality to integration tests for the xUnit testing framework.
    /// </summary>
    public abstract class IntegrationTest : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        protected static readonly Faker Bogus = new();

        protected IntegrationTest(ITestOutputHelper outputWriter)
        {
            _loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace)
                       .AddXunitTestLogging(outputWriter);
            });

            Logger = new XunitTestLogger(outputWriter);
        }

        static IntegrationTest()
        {
            Configuration = TestConfig.Create(options =>
            {
                options.AddOptionalJsonFile("appsettings.default.json")
                       .AddOptionalJsonFile("appsettings.local.json");
            });
            Credential = Configuration.GetServicePrincipal().GetCredential();
        }

        /// <summary>
        /// Gets the shared configuration loaded for this integration test suite.
        /// </summary>
        public static TestConfig Configuration { get; }

        /// <summary>
        /// Gets the shared credential loaded for this integration test suite to authenticate with Azure resources.
        /// </summary>
        public static TokenCredential Credential { get; }

        /// <summary>
        /// Gets the logger to write diagnostic messages during the integration test execution.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Creates a category-logger to write diagnostic messages during the integration test execution.
        /// </summary>
        /// <typeparam name="TCategoryName">The custom type name to write log messages for.</typeparam>
        protected ILogger<TCategoryName> CreateLogger<TCategoryName>()
        {
            return _loggerFactory.CreateLogger<TCategoryName>();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _loggerFactory?.Dispose();
        }
    }
}
