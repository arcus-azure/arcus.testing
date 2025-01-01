using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents a temporary Azure Service Bus topic subscription rule that will be deleted when the instance is disposed.
    /// </summary>
    public class TemporaryTopicSubscriptionRule : IAsyncDisposable
    {
        private readonly ServiceBusAdministrationClient _adminClient;
        private readonly RuleFilter _originalFilter;
        private readonly RuleAction _originalAction;
        private readonly bool _createdByUs;
        private readonly ILogger _logger;

        private TemporaryTopicSubscriptionRule(
            ServiceBusAdministrationClient adminClient,
            string fullyQualifiedNamespace,
            string topicName,
            string subscriptionName,
            RuleProperties currentRule,
            RuleFilter originalFilter,
            RuleAction originalAction,
            bool createdByUs,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(adminClient);
            ArgumentNullException.ThrowIfNull(fullyQualifiedNamespace);
            ArgumentNullException.ThrowIfNull(topicName);
            ArgumentNullException.ThrowIfNull(subscriptionName);
            ArgumentNullException.ThrowIfNull(currentRule);

            _adminClient = adminClient;
            _originalFilter = originalFilter;
            _originalAction = originalAction;
            _createdByUs = createdByUs;
            _logger = logger ?? NullLogger.Instance;

            Name = currentRule.Name;
            Filter = currentRule.Filter;
            SubscriptionName = subscriptionName;
            TopicName = topicName;
            FullyQualifiedNamespace = fullyQualifiedNamespace;
        }

        /// <summary>
        /// Gets the name of the Azure Service bus topic subscription rule that is currently managed by the test fixture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the current set subscription rule filter on the Azure Service bus topic subscription, managed by the test fixture.
        /// </summary>
        public RuleFilter Filter { get; }

        /// <summary>
        /// Gets the name of the Azure Service bus topic subscription for which this test fixture managed a rule.
        /// </summary>
        public string SubscriptionName { get; }

        /// <summary>
        /// Gets the name of the Azure Service bus topic for which this test fixture managed a subscription rule.
        /// </summary>
        public string TopicName { get; }

        /// <summary>
        /// Gets the fully-qualified name of the Azure Service bus namespace for which this test fixture managed a subscription rule.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Creates an instance of the <see cref="TemporaryTopicSubscriptionRule"/> which creates a new Azure Service bus topic subscription rule if it doesn't exist yet.
        /// </summary>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="topicName">The name of the Azure Service bus topic in which the subscription rule should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service bus topic.</param>
        /// <param name="ruleName">The name of the subscription rule in the configured Azure Service bus topic subscription.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service bus topic exists with the provided <paramref name="topicName"/> in the given <paramref name="fullyQualifiedNamespace"/>.
        /// </exception>
        public static async Task<TemporaryTopicSubscriptionRule> CreateIfNotExistsAsync(
            string fullyQualifiedNamespace,
            string topicName,
            string subscriptionName,
            string ruleName,
            ILogger logger,
            Action<CreateRuleOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus namespace to create a temporary topic subscription rule", nameof(fullyQualifiedNamespace));
            }

            var adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            return await CreateIfNotExistsAsync(adminClient, topicName, subscriptionName, ruleName, logger, configureOptions);
        }

        /// <summary>
        /// Creates an instance of the <see cref="TemporaryTopicSubscriptionRule"/> which creates a new Azure Service bus topic subscription rule if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service bus resource where the topic subscription rule should be created.</param>
        /// <param name="topicName">The name of the Azure Service bus topic in which the subscription rule should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service bus topic.</param>
        /// <param name="ruleName">The name of the subscription rule in the configured Azure Service bus topic subscription.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus topic subscription.</param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscriptionRule> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            string ruleName,
            ILogger logger)
        {
            return await CreateIfNotExistsAsync(adminClient, topicName, subscriptionName, ruleName, logger, configureOptions: null);
        }

        /// <summary>
        /// Creates an instance of the <see cref="TemporaryTopicSubscriptionRule"/> which creates a new Azure Service bus topic subscription rule if it doesn't exist yet.
        /// </summary>
        /// <param name="adminClient">The administration client to interact with the Azure Service bus resource where the topic subscription rule should be created.</param>
        /// <param name="topicName">The name of the Azure Service bus topic in which the subscription rule should be created.</param>
        /// <param name="subscriptionName">The name of the subscription in the configured Azure Service bus topic.</param>
        /// <param name="ruleName">The name of the subscription rule in the configured Azure Service bus topic subscription.</param>
        /// <param name="logger">The logger to write diagnostic messages during the lifetime of the Azure Service bus topic subscription.</param>
        /// <param name="configureOptions">
        ///     The function to configure the additional options that describes how the Azure Service bus topic subscription should be created.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when one of the passed arguments is blank.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the no Azure Service bus topic exists with the provided <paramref name="topicName"/>
        ///     in the given namespace where the given <paramref name="adminClient"/> points to.
        /// </exception>
        public static async Task<TemporaryTopicSubscriptionRule> CreateIfNotExistsAsync(
            ServiceBusAdministrationClient adminClient,
            string topicName,
            string subscriptionName,
            string ruleName,
            ILogger logger,
            Action<CreateRuleOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(adminClient);

            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus topic name to create a temporary topic subscription rule", nameof(topicName));
            }

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus topic subscription name to create a temporary topic subscription rule", nameof(subscriptionName));
            }

            if (string.IsNullOrWhiteSpace(ruleName))
            {
                throw new ArgumentException(
                    "Requires a non-blank Azure Service bus topic subscription rule name to create a temporary topic subscription rule", nameof(ruleName));
            }

            logger ??= NullLogger.Instance;

            var options = new CreateRuleOptions(ruleName);
            configureOptions?.Invoke(options);

            NamespaceProperties properties = await adminClient.GetNamespacePropertiesAsync();
            string serviceBusNamespace = properties.Name;

            if (!await adminClient.TopicExistsAsync(topicName))
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] cannot create temporary subscription rule '{ruleName}' on Azure Service bus topic subscription '{serviceBusNamespace}/{topicName}/{subscriptionName}' " +
                    $"because the topic '{topicName}' does not exists in the provided Azure Service bus namespace. " +
                    $"Please make sure to have an available Azure Service bus topic before using the temporary topic subscription rule test fixture");
            }

            if (!await adminClient.SubscriptionExistsAsync(topicName, subscriptionName))
            {
                throw new InvalidOperationException(
                    $"[Test:Setup] cannot create temporary subscription rule '{ruleName}' on Azure Service bus topic subscription '{serviceBusNamespace}/{topicName}/{subscriptionName}' " +
                    $"because the subscription '{subscriptionName}' does not exists in the provided Azure Service bus topic '{topicName}'. " +
                    $"Please make sure to have an available Azure Service bus topic subscription before using the temporary topic subscription rule test fixture");
            }

            if (await adminClient.RuleExistsAsync(topicName, subscriptionName, ruleName))
            {
                RuleProperties currentRule = await adminClient.GetRuleAsync(topicName, subscriptionName, ruleName);

                logger.LogDebug("[Test:Setup] Replace already existing Azure Service bus topic subscription rule '{RuleName}' on subscription '{Namespace}/{TopicName}/{SubscriptionName}'", ruleName, serviceBusNamespace, topicName, subscriptionName);

                RuleFilter originalFilter = currentRule.Filter;
                RuleAction originalAction = currentRule.Action;

                currentRule.Filter = options.Filter;
                currentRule.Action = options.Action;

                await adminClient.UpdateRuleAsync(topicName, subscriptionName, currentRule);

                return new TemporaryTopicSubscriptionRule(
                    adminClient, serviceBusNamespace, topicName, subscriptionName, currentRule, originalFilter, originalAction, createdByUs: false, logger);
            }
            else
            {
                logger.LogDebug("[Test:Teardown] Create new Azure Service bus topic subscription rule '{RuleName}' on subscription '{Namespace}/{TopicName}/{SubscriptionName}'", ruleName, serviceBusNamespace, topicName, subscriptionName);
                RuleProperties currentRule = await adminClient.CreateRuleAsync(topicName, subscriptionName, options);

                return new TemporaryTopicSubscriptionRule(
                    adminClient, serviceBusNamespace, topicName, subscriptionName, currentRule, originalFilter: null, originalAction: null, createdByUs: true, logger);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (await _adminClient.RuleExistsAsync(TopicName, SubscriptionName, Name))
            {
                if (_createdByUs)
                {
                    _logger.LogDebug("[Test:Teardown] Delete Azure Service bus topic subscription rule '{RuleName}' on subscription '{Namespace}/{TopicName}/{SubscriptionName}'", Name, FullyQualifiedNamespace, TopicName, SubscriptionName);
                    await _adminClient.DeleteRuleAsync(TopicName, SubscriptionName, Name);
                }
                else
                {
                    _logger.LogDebug("[Test:Teardown] Revert Azure Service bus topic subscription rule '{RuleName}' on subscription '{Namespace}/{TopicName}/{SubscriptionName}'", Name, FullyQualifiedNamespace, TopicName, SubscriptionName);

                    RuleProperties currentRule = await _adminClient.GetRuleAsync(TopicName, SubscriptionName, Name);
                    currentRule.Filter = _originalFilter;
                    currentRule.Action = _originalAction;

                    await _adminClient.UpdateRuleAsync(TopicName, SubscriptionName, currentRule);
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}