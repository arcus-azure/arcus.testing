using System;
using Microsoft.Extensions.Configuration;

namespace Arcus.Testing.Tests.Integration.Core.Fixture
{
    internal class CustomTestConfig : TestConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestConfig" /> class.
        /// </summary>
        public CustomTestConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestConfig" /> class.
        /// </summary>
        public CustomTestConfig(Action<TestConfigOptions> configureOptions) : base(configureOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestConfig" /> class.
        /// </summary>
        public CustomTestConfig(Action<TestConfigOptions, IConfigurationBuilder> configureConfig) : base(configureConfig)
        {
        }
    }
}
