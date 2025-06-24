﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available options on the <see cref="TemporaryTopicSubscription"/> when setting up the test fixture.
    /// </summary>
    public class OnSetupTemporaryTopicSubscriptionOptions
    {
        private readonly Collection<Action<CreateSubscriptionOptions>> _configureSubscriptionOptions = new();

        /// <summary>
        /// Configures the <see cref="Azure.Messaging.ServiceBus.Administration.CreateSubscriptionOptions"/> used when the test fixture creates the topic subscription.
        /// </summary>
        /// <remarks>
        ///     Multiple calls gets aggregated together.
        /// </remarks>
        /// <param name="configureOptions">The custom function to alter the way the topic subscription gets created.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureOptions"/> is <c>null</c>.</exception>
        public OnSetupTemporaryTopicSubscriptionOptions CreateSubscriptionWith(
            Action<CreateSubscriptionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            _configureSubscriptionOptions.Add(configureOptions);

            return this;
        }

        internal CreateSubscriptionOptions CreateSubscriptionOptions(string topicName, string subscriptionName)
        {
            var options = new CreateSubscriptionOptions(topicName, subscriptionName);
            foreach (var configureOptions in _configureSubscriptionOptions)
            {
                configureOptions(options);
            }

            return options;
        }
    }

    /// <summary>
    /// Represents the available options on the <see cref="TemporaryTopicSubscription"/>.
    /// </summary>
    public class TemporaryTopicSubscriptionOptions
    {
        /// <summary>
        /// Gets the options related to setting up the <see cref="TemporaryTopicSubscription"/>.
        /// </summary>
        public OnSetupTemporaryTopicSubscriptionOptions OnSetup { get; } = new OnSetupTemporaryTopicSubscriptionOptions();
    }

    /// <summary>
    /// Represents a temporary Azure Service Bus topic subscription that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryTopicSubscription : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _client;
        private readonly Collection<CreateRuleOptions> _rules = new();

        private readonly CreateSubscriptionOptions _options;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryTopicSubscription(
            ServiceBusAdministrationClient client,
            string serviceBusNamespace,
            CreateSubscriptionOptions options,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(serviceBusNamespace);

            _client = client;
            _options = options;
            _createdByUs = createdByUs;
            _logger = logger;

            Name = _options.SubscriptionName;
            TopicName = _options.TopicName;
            FullyQualifiedNamespace = serviceBusNamespace;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic subscription that is possibly created by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic where this topic subscription is test-managed.
        /// </summary>
        public string TopicName { get; }

        /// <summary>
        /// Gets the fully-qualified name of the Azure Service Bus namespace for which this test fixture managed a topic subscription.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/> in the given <paramref name="fullyQualifiedNamespace"/>.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(string fullyQualifiedNamespace, string topicName, string subscriptionName, ILogger logger)
        {
            return await CreateIfNotExistsAsync(fullyQualifiedNamespace, topicName, subscriptionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/> in the given <paramref name="fullyQualifiedNamespace"/>.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string topicName,
            string subscriptionName,
            ILogger logger,
            Action<TemporaryTopicSubscriptionOptions> configureOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);

            var client = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            return await CreateIfNotExistsAsync(client, topicName, subscriptionName, logger, configureOptions);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic subscription should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, topicName, subscriptionName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TemporaryTopicSubscription"/> which creates a new Azure Service Bus topic subscription if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service Bus resource where the topic subscription should be created.</param>
        /// <param name="topicName">The name of the Azure Service Bus topic in which the subscription should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service Bus topic.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service Bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service Bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="adminClient"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service Bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscription> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            ILogger logger,
            Action<TemporaryTopicSubscriptionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
            logger ??= NullLogger.Instance;

            var options = new TemporaryTopicSubscriptionOptions();
            configureOptions?.Invoke(options);
            
            CreateSubscriptionOptions createOptions = options.OnSetup.CreateSubscriptionOptions(topicName, subscriptionName);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (!await adminClient.TopicExistsAsync(createOptions.TopicName))
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] cannot create temporary subscription '{createOptions.SubscriptionName}' on Azure Service Bus topic '{serviceBusNamespace}/{createOptions.TopicName}' " +
                    $"because the topic '{createOptions.TopicName}' does not exists in the provided Azure Service Bus namespace. " +
                    $"Please make sure to have an available Azure Service Bus topic before using the temporary topic subscription test fixture");
            }

            if (await adminClient.SubscriptionExistsAsync(createOptions.TopicName, createOptions.SubscriptionName))
            {
                logger.LogTrace("[Test:Setup] Use already existing Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", createOptions.SubscriptionName, serviceBusNamespace, createOptions.TopicName);
                return new TemporaryTopicSubscription(adminClient, serviceBusNamespace, createOptions, createdByUs: false, logger);
            }

            logger.LogTrace("[Test:Setup] Create new Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", createOptions.SubscriptionName, serviceBusNamespace, createOptions.TopicName);
            await adminClient.CreateSubscriptionAsync(createOptions);

            return new TemporaryTopicSubscription(adminClient, serviceBusNamespace, createOptions, createdByUs: true, logger);
        }

        /// <summary>
        /// Adds an Azure Service Bus topic subscription rule to the test fixture, which will get disposed of when the test fixture gets disposed.
        /// </summary>
        /// <param name="ruleName">The name to describe the subscription rule.</param>
        /// <param name="ruleFilter">The filter expression used to match messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="ruleName"/> or <paramref name="ruleFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="ruleName"/> represents the default rule name.</exception>
        public async Task AddRuleIfNotExistsAsync(string ruleName, RuleFilter ruleFilter)
        {
            ArgumentNullException.ThrowIfNull(ruleName);
            ArgumentNullException.ThrowIfNull(ruleFilter);

            if (ruleName == CreateRuleOptions.DefaultRuleName)
            {
                throw new ArgumentException(
                    "Only custom Azure Service Bus topic subscription rules can be added to the test fixture, please provide a custom name for your test-managed rule", nameof(ruleName));
            }

            if (await _client.RuleExistsAsync(TopicName, Name, ruleName))
            {
                _logger.LogDebug("[Test] Skip creation of Azure Service Bus topic subscription rule '{RuleName}' in '{Namespace}/{TopicName}/{SubscriptionName}' as it already exists", ruleName, FullyQualifiedNamespace, TopicName, Name);
            }
            else
            {
                _logger.LogDebug("[Test] Create new Azure Service Bus topic subscription rule '{RuleName}' in '{Namespace}/{TopicName}/{SubscriptionName}'", ruleName, FullyQualifiedNamespace, TopicName, Name);

                var options = new CreateRuleOptions(ruleName, ruleFilter);
                await _client.CreateRuleAsync(TopicName, Name, options);

                _rules.Add(options);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await using var disposables = new DisposableCollection(_logger);

            if (await _client.SubscriptionExistsAsync(_options.TopicName, _options.SubscriptionName))
            {
                if (_createdByUs)
                {
                    disposables.Add(AsyncDisposable.Create(async () =>
                    {
                        _logger.LogDebug("[Test:Teardown] Delete Azure Service Bus topic subscription '{SubscriptionName}' in '{Namespace}/{TopicName}'", _options.SubscriptionName, FullyQualifiedNamespace, _options.TopicName);
                        await _client.DeleteSubscriptionAsync(_options.TopicName, _options.SubscriptionName);
                    }));
                }
                else
                {
                    disposables.AddRange(_rules.Select(r => AsyncDisposable.Create(async () => 
                    {
                        if (await _client.RuleExistsAsync(TopicName, Name, r.Name))
                        {
                            _logger.LogDebug("[Test:Teardown] Delete Azure Service Bus topic subscription rule '{RuleName}' in {Namespace}/{TopicName}/{SubscriptionName}", r.Name, FullyQualifiedNamespace, _options.TopicName, _options.SubscriptionName);
                            await _client.DeleteRuleAsync(TopicName, Name, r.Name);
                        }
                    })));
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
