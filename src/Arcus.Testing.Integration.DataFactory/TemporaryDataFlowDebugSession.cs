using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the options to configure the <see cref="TemporaryDataFlowDebugSession"/>.
    /// </summary>
    public class TemporaryDataFlowDebugSessionOptions
    {
        private int _timeToLiveInMinutes = 90;
        private Guid _activeSessionId;

        /// <summary>
        /// Gets or sets the time to live setting of the cluster in the debug session in minutes (default: 90 minutes).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than 1.</exception>
        public int TimeToLiveInMinutes
        {
            get => _timeToLiveInMinutes;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                _timeToLiveInMinutes = value;
            }
        }

        /// <summary>
        /// Gets or sets the optional session ID of an 'active' debug session in the Azure Data Factory resource.
        /// </summary>
        /// <remarks>
        ///     This is useful when developing locally when you do not want to start/stop the debug session on every run.
        ///     But this also means that in case an active session is found, it will not be teardown when the test fixture disposes.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is an empty GUID.</exception>
        public Guid ActiveSessionId
        {
            get => _activeSessionId;
            set
            {
                ArgumentOutOfRangeException.ThrowIfEqual(value, Guid.Empty);
                _activeSessionId = value;
            }
        }
    }

    /// <summary>
    /// Represents a temporary active debug session to link data flows under test.
    /// </summary>
    public class TemporaryDataFlowDebugSession : IAsyncDisposable
    {
        private readonly bool _startedByUs;
        private readonly ILogger _logger;

        private TemporaryDataFlowDebugSession(bool startedByUs, Guid sessionId, DataFactoryResource resource, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(resource);

            _startedByUs = startedByUs;
            _logger = logger ?? NullLogger.Instance;

            DataFactory = resource;
            SessionId = sessionId;
        }

        /// <summary>
        /// Gets the Azure Data Factory resource where the active data flow debug session is started.
        /// </summary>
        private DataFactoryResource DataFactory { get; }

        /// <summary>
        /// Gets the session ID of the active data flow debug session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Starts a new active Azure Data Factory data flow debug session for the given <paramref name="dataFactoryResourceId"/>.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> for authentication;
        ///     use the <see cref="StartDebugSessionAsync(DataFactoryResource,ILogger)"/> overload to provide a custom authentication mechanism.
        /// </remarks>
        /// <param name="dataFactoryResourceId">
        ///   <para>The resource ID to the Azure Data Factory instance where to start the active data flow debug session.</para>
        ///   <para>The resource ID can be constructed via <see cref="DataFactoryResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier dataFactoryResourceId =
        ///           DataFactoryResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;factory-name&gt;");
        ///     </code>
        ///   </example>  
        /// </param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dataFactoryResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the data flow debug session did not result in a session ID.</exception>
        public static async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(ResourceIdentifier dataFactoryResourceId, ILogger logger)
        {
            return await StartDebugSessionAsync(dataFactoryResourceId, logger, configureOptions: null);
        }

        /// <summary>
        /// Starts a new active Azure Data Factory data flow debug session for the given <paramref name="dataFactoryResourceId"/>.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> for authentication;
        ///     use the <see cref="StartDebugSessionAsync(DataFactoryResource,ILogger,Action{TemporaryDataFlowDebugSessionOptions})"/> overload to provide a custom authentication mechanism.
        /// </remarks>
        /// <param name="dataFactoryResourceId">
        ///   <para>The resource ID to the Azure Data Factory instance where to start the active data flow debug session.</para>
        ///   <para>The resource ID can be constructed via <see cref="DataFactoryResource.CreateResourceIdentifier"/>:</para>
        ///   <example>
        ///     <code>
        ///       ResourceIdentifier dataFactoryResourceId =
        ///           DataFactoryResource.CreateResourceIdentifier("&lt;subscription-id&gt;", "&lt;resource-group&gt;", "&lt;factory-name&gt;");
        ///     </code>
        ///   </example>  
        /// </param>        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <param name="configureOptions">The function to configure the options of the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dataFactoryResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the data flow debug session did not result in a session ID.</exception>
        public static async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(
            ResourceIdentifier dataFactoryResourceId,
            ILogger logger,
            Action<TemporaryDataFlowDebugSessionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(dataFactoryResourceId);

            var armClient = new ArmClient(new DefaultAzureCredential());
            DataFactoryResource resource = armClient.GetDataFactoryResource(dataFactoryResourceId);

            return await StartDebugSessionAsync(resource, logger, configureOptions);
        }

        /// <summary>
        /// Starts a new active Azure Data Factory data flow debug session for the given <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">
        ///   <para>The resource to start the active data flow debug session for.</para>
        ///   <para>The resource should be retrieved via the <see cref="ArmClient"/>:</para>
        ///   <example>
        ///     <code>
        ///       var credential = new DefaultAzureCredential();
        ///       var arm = new ArmClient(credential);
        ///       DataFactoryResource resource = arm.GetDataFactoryResource(dataFactoryResourceId);
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="resource"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the data flow debug session did not result in a session ID.</exception>
        public static async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(DataFactoryResource resource, ILogger logger)
        {
            return await StartDebugSessionAsync(resource, logger, configureOptions: null);
        }

        /// <summary>
        /// Starts a new active Azure Data Factory data flow debug session for the given <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">
        ///   <para>The resource to start the active data flow debug session for.</para>
        ///   <para>The resource should be retrieved via the <see cref="ArmClient"/>:</para>
        ///   <example>
        ///     <code>
        ///       var credential = new DefaultAzureCredential();
        ///       var arm = new ArmClient(credential);
        ///       DataFactoryResource resource = arm.GetDataFactoryResource(dataFactoryResourceId);
        ///     </code>
        ///   </example>
        /// </param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <param name="configureOptions">The function to configure the options of the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="resource"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the data flow debug session did not result in a session ID.</exception>
        public static async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(
            DataFactoryResource resource,
            ILogger logger,
            Action<TemporaryDataFlowDebugSessionOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(resource);
            logger ??= NullLogger.Instance;

            var options = new TemporaryDataFlowDebugSessionOptions();
            configureOptions?.Invoke(options);

            DataFlowDebugSessionInfo activeSession = await GetActiveDebugSessionOrDefaultAsync(resource, options.ActiveSessionId);
            if (activeSession is not null)
            {
                logger.LogDebug("[Test:Setup] Re-using Azure Data Factory '{Name}' data flow debug session '{SessionId}'", resource.Id.Name, activeSession.SessionId);
                return new TemporaryDataFlowDebugSession(startedByUs: false, activeSession.SessionId ?? throw new InvalidOperationException($"Re-using Azure Data Factory '{resource.Id.Name}' DataFlow debug session did not result in a session ID"), resource, logger);
            }

            logger.LogTrace("[Test:Setup] Starting Azure Data Factory '{Name}' data flow debug session... (might take up to 3 min to start up)", resource.Id.Name);
            ArmOperation<DataFactoryDataFlowCreateDebugSessionResult> result =
                await resource.CreateDataFlowDebugSessionAsync(WaitUntil.Completed, new DataFactoryDataFlowDebugSessionContent
                {
                    TimeToLiveInMinutes = options.TimeToLiveInMinutes
                });

            Guid sessionId = result.Value.SessionId ?? throw new InvalidOperationException($"Starting Data Factory '{resource.Id.Name}' data flow debug session did not result in a session ID");
            logger.LogDebug("[Test:Setup] Started Azure Data Factory '{Name}' data flow debug session '{SessionId}'", resource.Id.Name, sessionId);

            return new TemporaryDataFlowDebugSession(startedByUs: true, sessionId, resource, logger);
        }

        private static async Task<DataFlowDebugSessionInfo> GetActiveDebugSessionOrDefaultAsync(DataFactoryResource resource, Guid existingSessionId)
        {
            if (existingSessionId == Guid.Empty)
            {
                return null;
            }

            await foreach (DataFlowDebugSessionInfo session in resource.GetDataFlowDebugSessionsAsync())
            {
                if (existingSessionId == session.SessionId)
                {
                    return session;
                }
            }

            return null;
        }

        /// <summary>
        /// Starts a given data flow within the debug session,
        /// which should give a result in the <paramref name="targetSinkName"/>.
        /// </summary>
        /// <param name="dataFlowName">The name of the data flow to start.</param>
        /// <param name="targetSinkName">The name of the target sink to get the result from.</param>
        /// <returns>The final result of the data flow run.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dataFlowName"/> or <paramref name="targetSinkName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the data flow execution did not result in a successful status.</exception>
        /// <exception cref="RequestFailedException">Thrown when one or more interactions with the Azure DataFactory resource failed.</exception>
        public async Task<DataFlowRunResult> RunDataFlowAsync(string dataFlowName, string targetSinkName)
        {
            return await RunDataFlowAsync(dataFlowName, targetSinkName, configureOptions: null);
        }

        /// <summary>
        /// Starts a given data flow within the debug session,
        /// which should give a result in the <paramref name="targetSinkName"/>.
        /// </summary>
        /// <param name="dataFlowName">The name of the data flow to start.</param>
        /// <param name="targetSinkName">The name of the target sink to get the result from.</param>
        /// <param name="configureOptions">The function to configure the options of the data flow run.</param>
        /// <returns>The final result of the data flow run.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dataFlowName"/> or <paramref name="targetSinkName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the data flow execution did not result in a successful status.</exception>
        /// <exception cref="RequestFailedException">Thrown when one or more interactions with the Azure DataFactory resource failed.</exception>
        public async Task<DataFlowRunResult> RunDataFlowAsync(
            string dataFlowName,
            string targetSinkName,
            Action<RunDataFlowOptions> configureOptions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dataFlowName);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetSinkName);

            var options = new RunDataFlowOptions();
            configureOptions?.Invoke(options);

            await StartDataFlowAsync(dataFlowName, options);

            return await GetDataFlowResultAsync(dataFlowName, targetSinkName, options);
        }

        private async Task StartDataFlowAsync(string dataFlowName, RunDataFlowOptions options)
        {
            _logger.LogTrace("[Test:Setup] Adding data flow '{DataFlowName}' of Azure Data Factory '{DataFactoryName}' to debug session", dataFlowName, DataFactory.Id.Name);
            DataFactoryDataFlowResource dataFlow = await DataFactory.GetDataFactoryDataFlowAsync(dataFlowName);

            var debug = new DataFactoryDataFlowDebugPackageContent
            {
                DataFlow = new DataFactoryDataFlowDebugInfo(dataFlow.Data.Properties) { Name = dataFlowName },
                DebugSettings = CreateDebugSettings(options),
                SessionId = SessionId
            };

            foreach (string serviceName in options.LinkedServiceNames)
            {
                await AddLinkedServiceAsync(debug, DataFactory, serviceName);
            }

            await AddDebugVariantsOfDataFlowSourcesAsync(debug, DataFactory, dataFlow);
            await AddDebugVariantsOfDataFlowSinksAsync(debug, DataFactory, dataFlow);
            await AddDebugVariantsOfFlowletsAsync(debug, DataFactory, options);

            await DataFactory.AddDataFlowToDebugSessionAsync(debug);
        }

        private DataFlowDebugPackageDebugSettings CreateDebugSettings(RunDataFlowOptions options)
        {
            var settings = new DataFlowDebugPackageDebugSettings();

            foreach (KeyValuePair<string, BinaryData> parameter in options.DataFlowParameters)
            {
                _logger.LogDebug("[Test:Setup] Add data flow parameter '{Name}' to debug session", parameter.Key);
                settings.Parameters[parameter.Key] = parameter.Value;
            }

            foreach (KeyValuePair<string, IDictionary<string, object>> datasetParameter in options.DataSetParameters)
            {
                foreach (KeyValuePair<string, object> parameter in datasetParameter.Value)
                {
                    _logger.LogDebug("[Test:Setup] Add dataset parameter '{ParameterName}' for dataset '{DataSetName}' to debug session", parameter.Key, datasetParameter.Key);
                }
            }

            string jsonString = System.Text.Json.JsonSerializer.Serialize(options.DataSetParameters);
            settings.DatasetParameters = BinaryData.FromString(jsonString);

            return settings;
        }

        private async Task AddDebugVariantsOfDataFlowSourcesAsync(
            DataFactoryDataFlowDebugPackageContent debug,
            DataFactoryResource dataFactory,
            DataFactoryDataFlowResource dataFlow)
        {
            if (dataFlow.Data.Properties is DataFactoryMappingDataFlowProperties properties)
            {
                foreach (DataFlowSource source in properties.Sources)
                {
                    debug.DebugSettings.SourceSettings.Add(new DataFlowSourceSetting { SourceName = source.Name, RowLimit = 100 });
                    if (source.Dataset != null)
                    {
                        DataFactoryDatasetResource dataset = await AddDataSetAsync(debug, dataFactory, source.Dataset.ReferenceName);
                        await AddLinkedServiceAsync(debug, dataFactory, dataset.Data.Properties.LinkedServiceName.ReferenceName);
                    }
                }
            }
        }

        private async Task AddDebugVariantsOfDataFlowSinksAsync(
            DataFactoryDataFlowDebugPackageContent debug,
            DataFactoryResource dataFactory,
            DataFactoryDataFlowResource dataFlow)
        {
            if (dataFlow.Data.Properties is DataFactoryMappingDataFlowProperties properties)
            {
                DataFlowSink[] sinks = properties.Sinks.Where(s => s != null).ToArray();
                foreach (DataFlowSink sink in sinks)
                {
                    DataFactoryDatasetResource dataset = await AddDataSetAsync(debug, dataFactory, sink.Dataset.ReferenceName);
                    await AddLinkedServiceAsync(debug, dataFactory, dataset.Data.Properties.LinkedServiceName.ReferenceName);
                }
            }
        }

        private async Task AddDebugVariantsOfFlowletsAsync(
            DataFactoryDataFlowDebugPackageContent debug,
            DataFactoryResource dataFactory,
            RunDataFlowOptions options)
        {
            if (options.FlowletNames.Count == 0)
            {
                return;
            }

            foreach (var flowletName in options.FlowletNames)
            {
                _logger.LogDebug("[Test:Setup] Add flowlet '{FlowletName}' of Azure Data Factory '{DataFactoryName}' to debug session", flowletName, dataFactory.Id.Name);

                DataFactoryDataFlowResource flowlet = (await dataFactory.GetDataFactoryDataFlowAsync(flowletName)).Value;

                var dataFactoryFlowletDebugInfo = new DataFactoryDataFlowDebugInfo(flowlet.Data.Properties)
                {
                    Name = flowletName
                };
                debug.DataFlows.Add(dataFactoryFlowletDebugInfo);
            }
        }

        private async Task<DataFactoryDatasetResource> AddDataSetAsync(DataFactoryDataFlowDebugPackageContent debug, DataFactoryResource dataFactory, string datasetName)
        {
            _logger.LogDebug("[Test:Setup] Add dataset '{DataSetName}' of Azure Data Factory '{DataFactoryName}' to debug session", datasetName, dataFactory.Id.Name);

            DataFactoryDatasetResource dataset = await dataFactory.GetDataFactoryDatasetAsync(datasetName);
            debug.Datasets.Add(new DataFactoryDatasetDebugInfo(dataset.Data.Properties) { Name = dataset.Data.Name });

            return dataset;
        }

        private async Task AddLinkedServiceAsync(DataFactoryDataFlowDebugPackageContent debug, DataFactoryResource dataFactory, string serviceName)
        {
            _logger.LogDebug("[Test:Setup] Add linked service '{ServiceName}' of Azure Data Factory '{DatFactoryName}' to debug session", serviceName, dataFactory.Id.Name);

            DataFactoryLinkedServiceResource linkedService = await dataFactory.GetDataFactoryLinkedServiceAsync(serviceName);
            debug.LinkedServices.Add(new DataFactoryLinkedServiceDebugInfo(linkedService.Data.Properties) { Name = linkedService.Data.Name });
        }

        private async Task<DataFlowRunResult> GetDataFlowResultAsync(string dataFlowName, string targetSinkName, RunDataFlowOptions options)
        {
            _logger.LogInformation("[Test] Run data flow '{DataFlowName}' until result is available on sink '{SinkName}'", dataFlowName, targetSinkName);

            ArmOperation<DataFactoryDataFlowDebugCommandResult> result =
                await DataFactory.ExecuteDataFlowDebugSessionCommandAsync(WaitUntil.Completed, new DataFlowDebugCommandContent
                {
                    Command = "executePreviewQuery",
                    CommandPayload = new DataFlowDebugCommandPayload(targetSinkName)
                    {
                        RowLimits = options.MaxRows
                    },
                    SessionId = SessionId
                });

            if (result.Value.Status != "Succeeded")
            {
                throw new InvalidOperationException(
                    $"[Test] Executing data flow '{dataFlowName}' and waiting for a result in sink '{targetSinkName}' in Azure Data Factory '{DataFactory.Id.Name}' " +
                    $"did not result in a successful status: '{result.Value.Status}', please check whether the data flow is correctly set up and can be run within a debug session");
            }

            return new DataFlowRunResult(result.Value.Status, BinaryData.FromString(result.Value.Data));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_startedByUs)
            {
                _logger.LogDebug("[Test:Teardown] Stop Azure Data Factory '{Name}' data flow debug session '{SessionId}'", DataFactory.Id.Name, SessionId);
                await DataFactory.DeleteDataFlowDebugSessionAsync(new DeleteDataFlowDebugSessionContent { SessionId = SessionId });
            }

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents the run options when calling the <see cref="TemporaryDataFlowDebugSession.RunDataFlowAsync(string,string,Action{RunDataFlowOptions})"/>.
    /// </summary>
    public class RunDataFlowOptions
    {
        private int _maxRows = 100;

        internal Collection<string> LinkedServiceNames { get; } = [];
        internal IDictionary<string, BinaryData> DataFlowParameters { get; } = new Dictionary<string, BinaryData>();
        internal IDictionary<string, IDictionary<string, object>> DataSetParameters { get; } = new Dictionary<string, IDictionary<string, object>>();
        internal Collection<string> FlowletNames { get; } = [];

        /// <summary>
        /// Adds a parameter to the data flow to run.
        /// </summary>
        /// <remarks>
        ///     <para>For string parameters, the value must be enclosed in single quotes (example: <c>"'myValue'"</c>).</para>
        ///     <para>For boolean parameters, the value must be either <c>"true()"</c> or <c>"false()"</c>.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="value"/> is null.</exception>
        public RunDataFlowOptions AddDataFlowParameter(string name, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            DataFlowParameters[name] = BinaryData.FromObjectAsJson(value);
            return this;
        }

        /// <summary>
        /// Adds a parameter to a dataset that is part of the targeted data flow.
        /// </summary>
        /// <remarks>
        ///     The <paramref name="sourceOrSinkName"/> should be the "Output stream name" of the source or sink dataset in the data flow, not than the actual dataset name, see <a href="https://learn.microsoft.com/en-us/azure/data-factory/data-flow-source#source-settings" />.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="sourceOrSinkName"/> or the <paramref name="parameterName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="parameterValue"/> is null.</exception>
        public RunDataFlowOptions AddDataSetParameter(string sourceOrSinkName, string parameterName, object parameterValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceOrSinkName);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
            ArgumentNullException.ThrowIfNull(parameterValue);

            if (!DataSetParameters.ContainsKey(sourceOrSinkName))
            {
                DataSetParameters[sourceOrSinkName] = new Dictionary<string, object>();
            }

            DataSetParameters[sourceOrSinkName].Add(parameterName, parameterValue);
            return this;
        }

        /// <summary>
        /// Gets or sets the limit of rows for the preview response of the data flow run (default: 100 rows).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        public int MaxRows
        {
            get => _maxRows;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
                _maxRows = value;
            }
        }

        /// <summary>
        /// Adds an additional linked service to the Azure Data Factory debug session.
        /// </summary>
        /// <remarks>
        ///     This can be used to add, for example, Azure Key vault linked services that are needed when datasets require a vault for their authentication.
        /// </remarks>
        /// <param name="serviceName">The name of the linked service in Azure Data Factory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="serviceName"/> is blank.</exception>
        public RunDataFlowOptions AddLinkedService(string serviceName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
            LinkedServiceNames.Add(serviceName);
            return this;
        }

        /// <summary>
        /// Adds a flowlet to the Azure Data Factory debug session.
        /// </summary>
        /// <remarks>
        ///     This can be used to add flowlets that are needed when the data flow contains flowlets.
        /// </remarks>
        /// <param name="flowletName">The name of the linked service in Azure Data Factory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="flowletName"/> is blank.</exception>
        public RunDataFlowOptions AddFlowlet(string flowletName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(flowletName);
            FlowletNames.Add(flowletName);
            return this;
        }
    }
}
