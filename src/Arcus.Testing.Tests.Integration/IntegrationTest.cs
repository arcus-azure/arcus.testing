﻿using Arcus.Template.Tests.Integration.Logging;
using Arcus.Testing.Tests.Integration.Configuration;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

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
            Configuration = TestConfig.Create();
        }

        /// <summary>
        /// Gets the current configuration loaded for this integration test suite.
        /// </summary>
        protected TestConfig Configuration { get; }

        /// <summary>
        /// Gets the service principal that has access to the interacted with test resources currently being tested.
        /// </summary>
        protected ServicePrincipal ServicePrincipal => Configuration.GetServicePrincipal();

        /// <summary>
        /// Gets the logger to write diagnostic messages during the integration test execution.
        /// </summary>
        protected ILogger Logger { get; }
    }
}