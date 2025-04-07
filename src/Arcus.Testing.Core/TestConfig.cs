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
        /// Adds the current user-configured options to the current configuration <paramref name="builder"/>.
        /// </summary>
        internal void ApplyOptions(IConfigurationBuilder builder)
        {
            builder.AddJsonFile(MainJsonPath, optional: true);

            foreach (string path in _localAppSettingsNames)
            {
                builder.AddJsonFile(path, optional: true);
            }
        }
    }

    /// <summary>
    /// <para>Represents a set of key/value test application configuration properties, used during throughout the test suite.</para>
    /// <para>See also: <a href="https://testing.arcus-azure.net/features/core"/></para>
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
            : this((options, _) => configureOptions?.Invoke(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestConfig"/> class with custom 'appsettings.json' files based on the configured options.
        /// </summary>
        /// <param name="configureConfig">
        ///   <para>The function to configure the options that describe where the test configuration should be retrieved from, for example:</para>
        ///   <para>See also: <a href="https://testing.arcus-azure.net/features/core"/></para>
        ///   <example>
        ///     <code>
        ///       var config = TestConfig.Create((options, builder) =>
        ///       {
        ///           builder.AddEnvironmentVariables();
        ///       });
        ///     </code>
        ///   </example>
        /// </param>
        protected TestConfig(Action<TestConfigOptions, IConfigurationBuilder> configureConfig)
        {
            var options = new TestConfigOptions();
            var builder = new ConfigurationBuilder();

            configureConfig?.Invoke(options, builder);
            options.ApplyOptions(builder);

            _implementation = builder.Build();
            _options = options;
        }

        /// <summary>
        /// <para>Creates an <see cref="TestConfig"/> instance with default 'appsettings.json' and 'appsettings.local.json' variant as configuration sources.</para>
        /// <para>See also: <a href="https://testing.arcus-azure.net/features/core"/></para>
        /// </summary>
        public static TestConfig Create()
        {
            return new TestConfig();
        }

        /// <summary>
        /// <para>Creates an <see cref="TestConfig"/> instance with custom 'appsettings.json' files based on the configured options.</para>
        /// </summary>
        /// <param name="configureOptions">
        ///     <para>The function to configure the options that describe where the test configuration should be retrieved from.</para>
        ///     <para>See also: <a href="https://testing.arcus-azure.net/features/core"/></para>
        /// </param>
        public static TestConfig Create(Action<TestConfigOptions> configureOptions)
        {
            return new TestConfig(configureOptions);
        }

        /// <summary>
        /// <para>Creates an <see cref="TestConfig"/> instance with custom 'appsettings.json' files based on the configured options.</para>
        /// </summary>
        /// <param name="configureConfig">
        ///   <para>The function to configure the options that describe where the test configuration should be retrieved from, for example:</para>
        ///   <para>See also: <a href="https://testing.arcus-azure.net/features/core"/></para>
        ///   <example>
        ///     <code>
        ///       var config = TestConfig.Create((options, builder) =>
        ///       {
        ///           builder.AddEnvironmentVariables();
        ///       });
        ///     </code>
        ///   </example>
        /// </param>
        public static TestConfig Create(Action<TestConfigOptions, IConfigurationBuilder> configureConfig)
        {
            return new TestConfig(configureConfig);
        }

        /// <summary>
        /// Gets a test configuration subsection with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching subsection is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _implementation.GetSection(key);
        }

        /// <summary>
        /// Gets the immediate descendant test configuration subsections.
        /// </summary>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _implementation.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="IChangeToken" /> that can be used to observe when this test configuration is reloaded.
        /// </summary>
        public IChangeToken GetReloadToken()
        {
            return _implementation.GetReloadToken();
        }

        /// <summary>
        /// Gets or sets a test configuration value.
        /// </summary>
        /// <param name="key">The configuration key.</param>
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
                        $"please make sure that you use a non-blank key and that has a corresponding value specified in your (local or remote) '{mainFile}' file " +
                        $"and/or custom configuration sources, more info: https://testing.arcus-azure.net/features/core");
                }

                string value = _implementation[key];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new KeyNotFoundException(
                        $"[Test] Cannot find any non-blank test configuration value for the key: '{key}', " +
                        $"please make sure that this key is specified in your (local or remote) '{mainFile}' file and it is copied to the build output in your .csproj/.fsproj project file: " +
                        $"<CopyToOutputDirectory>Always/CopyToOutputDirectory>" +
                        Environment.NewLine +
                        "Alternatively, check your custom provided configuration sources, more info: https://testing.arcus-azure.net/features/core");
                }

                if (value.StartsWith("#{", StringComparison.InvariantCulture)
                    && value.EndsWith("}#", StringComparison.InvariantCulture))
                {
                    throw new KeyNotFoundException(
                        $"[Test] Cannot find test configuration value for the key '{key}', as it is still having the token '{value}' and is not being replaced by the real value, " +
                        $"please make sure to add a local alternative in the (ex: 'appsettings.{{Env}}.local.json') for the token with the real value required for this key." +
                        Environment.NewLine +
                        "Alternatively, check your custom provided configuration sources, more info: https://testing.arcus-azure.net/features/core");
                }

                return value;
            }
            set => _implementation[key] = value;
        }
    }
}
