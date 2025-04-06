using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options for the <see cref="TestConfig"/>.
    /// </summary>
    public class TestConfigOptions
    {
        private readonly Collection<string> _localAppSettingsNames = new() { "appsettings.local.json" };

        /// <summary>
        /// Override the default 'appsettings.json' JSON path where the test configuration values are retrieved from.
        /// </summary>
        /// <param name="path">The new default path (currently: 'appsettings.json').</param>
        public TestConfigOptions UseMainJsonFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Requires a non-blank relative path to the '*.json' file used as the default for the test configuration", nameof(path));
            }

            MainJsonPath = path;
            return this;
        }

        /// <summary>
        /// Adds the JSON configuration provider at <paramref name="path" /> the configuration.
        /// </summary>
        /// <param name="path">The path relative to the project output folder of the test suite project.</param>
        public TestConfigOptions AddOptionalJsonFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Requires a non-blank relative path to the '*.json' file used for the test configuration", nameof(path));
            }

            _localAppSettingsNames.Add(path);
            return this;
        }

        /// <summary>
        /// Gets the main JSON path to the configuration source.
        /// </summary>
        internal string MainJsonPath { get; private set; } = "appsettings.json";

        /// <summary>
        /// Gets all the configured additional JSON paths to files that acts as configuration sources.
        /// </summary>
        internal IEnumerable<string> OptionalJsonPaths => _localAppSettingsNames;
    }

    /// <summary>
    /// Represents a set of key/value test application configuration properties, used during throughout the test suite.
    /// </summary>
    public class TestConfig : IConfiguration
    {
        private readonly IConfiguration _implementation;
        private readonly TestConfigOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestConfig"/> class with default 'appsettings.json' and 'appsettings.local.json' variant as configuration sources.
        /// </summary>
        protected TestConfig() : this(configureOptions: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestConfig"/> class with custom 'appsettings.json' files based on the configured options.
        /// </summary>
        /// <param name="configureOptions">The function to configure the options that describe where the test configuration should be retrieved from.</param>
        protected TestConfig(Action<TestConfigOptions> configureOptions)
        {
            var options = new TestConfigOptions();
            configureOptions?.Invoke(options);

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile(options.MainJsonPath, optional: true);

            foreach (string path in options.OptionalJsonPaths)
            {
                builder.AddJsonFile(path, optional: true);
            }

            _implementation = builder.Build();
            _options = options;
        }

        /// <summary>
        /// Creates an <see cref="TestConfig"/> instance with default 'appsettings.json' and 'appsettings.local.json' variant as configuration sources.
        /// </summary>
        public static TestConfig Create()
        {
            return new TestConfig();
        }

        /// <summary>
        /// Creates an <see cref="TestConfig"/> instance with custom 'appsettings.json' files based on the configured options.
        /// </summary>
        /// <param name="configureOptions">The function to configure the options that describe where the test configuration should be retrieved from.</param>
        public static TestConfig Create(Action<TestConfigOptions> configureOptions)
        {
            return new TestConfig(configureOptions);
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _implementation.GetSection(key);
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _implementation.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" /> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" />.</returns>
        public IChangeToken GetReloadToken()
        {
            return _implementation.GetReloadToken();
        }

        /// <summary>
        /// Gets or sets a configuration value.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no value is available for the given <paramref name="key"/>.</exception>
        public string this[string key]
        {
            get
            {
                string mainFile = _options?.MainJsonPath ?? "app settings";
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new KeyNotFoundException(
                        $"[Test] Cannot find any test configuration value for the blank key: '{key}', " +
                        $"please make sure that you use a non-blank key and that has a corresponding value specified in your (local or remote) '{mainFile}' file");
                }

                string value = _implementation[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new KeyNotFoundException(
                        $"[Test] Cannot find any non-blank test configuration value for the key: '{key}', " +
                        $"please make sure that this key is specified in your (local or remote) '{mainFile}' file and it is copied to the build output in your .csproj/.fsproj project file: " +
                        $"<CopyToOutputDirectory>Always/CopyToOutputDirectory>");
                }

                if (value.StartsWith("#{", StringComparison.InvariantCulture)
                    && value.EndsWith("}#", StringComparison.InvariantCulture))
                {
                    throw new KeyNotFoundException(
                        $"[Test] Cannot find test configuration value for the key '{key}', as it is still having the token '{value}' and is not being replaced by the real value, " +
                        $"please make sure to add a local alternative in the (ex: 'appsettings.{{Env}}.local.json') for the token with the real value required for this key");
                }

                return value;
            }
            set => _implementation[key] = value;
        }
    }
}
