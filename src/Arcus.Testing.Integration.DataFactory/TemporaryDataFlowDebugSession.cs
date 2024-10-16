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
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Time to live in minutes must be at least 1 minute");
                }

                _timeToLiveInMinutes = value;
            }
        }

        /// <summary>
        /// Gets or sets the optional session ID of an 'active' debug session in the Data Factory resource.
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
                if (value == Guid.Empty)
                {
                    throw new ArgumentException("Requires  non-empty GUID to represent the session ID of an active debug session", nameof(value));
                }

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
        /// Gets the DataFactory resource where the active DataFlow debug session is started.
        /// </summary>
        private DataFactoryResource DataFactory { get; }

        /// <summary>
        /// Gets the session ID of the active data flow debug session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Starts a new active DataFactory DataFlow debug session for the given <paramref name="dataFactoryResourceId"/>.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> for authentication;
        ///     use the <see cref="StartDebugSessionAsync(DataFactoryResource,ILogger)"/> overload to provide a custom authentication mechanism.
        /// </remarks>
        /// <param name="dataFactoryResourceId">The resource ID to the DataFactory instance where to start the active DataFlow debug session.</param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dataFactoryResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the DataFlow debug session did not result in a session ID.</exception>
        public static async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(ResourceIdentifier dataFactoryResourceId, ILogger logger)
        {
            return await StartDebugSessionAsync(dataFactoryResourceId, logger, configureOptions: null);
        }

        /// <summary>
        /// Starts a new active DataFactory DataFlow debug session for the given <paramref name="dataFactoryResourceId"/>.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="DefaultAzureCredential"/> for authentication;
        ///     use the <see cref="StartDebugSessionAsync(DataFactoryResource,ILogger,Action{TemporaryDataFlowDebugSessionOptions})"/> overload to provide a custom authentication mechanism.
        /// </remarks>
        /// <param name="dataFactoryResourceId">The resource ID to the DataFactory instance where to start the active DataFlow debug session.</param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <param name="configureOptions">The function to configure the options of the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="dataFactoryResourceId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the DataFlow debug session did not result in a session ID.</exception>
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
        /// Starts a new active DataFactory DataFlow debug session for the given <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The resource to start the active DataFlow debug session for.</param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="resource"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the DataFlow debug session did not result in a session ID.</exception>
        public static async Task<TemporaryDataFlowDebugSession> StartDebugSessionAsync(DataFactoryResource resource, ILogger logger)
        {
            return await StartDebugSessionAsync(resource, logger, configureOptions: null);
        }

        /// <summary>
        /// Starts a new active DataFactory DataFlow debug session for the given <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The resource to start the active DataFlow debug session for.</param>
        /// <param name="logger">The logger to write diagnostic messages during the debug session.</param>
        /// <param name="configureOptions">The function to configure the options of the debug session.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="resource"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the starting of the DataFlow debug session did not result in a session ID.</exception>
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
                logger.LogTrace("[Test:Setup] Re-using Azure DataFactory '{Name}' DataFlow debug session '{SessionId}'", resource.Id.Name, activeSession.SessionId);
                return new TemporaryDataFlowDebugSession(startedByUs: false, activeSession.SessionId ?? throw new InvalidOperationException($"Re-using DataFactory '{resource.Id.Name}' DataFlow debug session did not result in a session ID"), resource, logger);
            }

            logger.LogTrace("[Test:Setup] Starting Azure DataFactory '{Name}' DataFlow debug session... (might take up to 3 min to start up)", resource.Id.Name);
            ArmOperation<DataFactoryDataFlowCreateDebugSessionResult> result = 
                await resource.CreateDataFlowDebugSessionAsync(WaitUntil.Completed, new DataFactoryDataFlowDebugSessionContent
                {
                    TimeToLiveInMinutes = options.TimeToLiveInMinutes
                });

            Guid sessionId = result.Value.SessionId ?? throw new InvalidOperationException($"Starting DataFactory '{resource.Id.Name}' DataFlow debug session did not result in a session ID");
            logger.LogTrace("[Test:Setup] Started Azure DataFactory '{Name}' DataFlow debug session '{SessionId}'", resource.Id.Name, sessionId);

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
        /// Starts a given DataFlow within the debug session,
        /// which should give a result in the <paramref name="targetSinkName"/>.
        /// </summary>
        /// <param name="dataFlowName">The name of the DataFlow to start.</param>
        /// <param name="targetSinkName">The name of the target sink to get the result from.</param>
        /// <returns>The final result of the DataFlow run.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dataFlowName"/> or <paramref name="targetSinkName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the DataFlow execution did not result in a successful status.</exception>
        /// <exception cref="RequestFailedException">Thrown when one or more interactions with the Azure DataFactory resource failed.</exception>
        public async Task<DataFlowRunResult> RunDataFlowAsync(string dataFlowName, string targetSinkName)
        {
            return await RunDataFlowAsync(dataFlowName, targetSinkName, configureOptions: null);
        }

        /// <summary>
        /// Starts a given DataFlow within the debug session,
        /// which should give a result in the <paramref name="targetSinkName"/>.
        /// </summary>
        /// <param name="dataFlowName">The name of the DataFlow to start.</param>
        /// <param name="targetSinkName">The name of the target sink to get the result from.</param>
        /// <param name="configureOptions">The function to configure the options of the DataFlow run.</param>
        /// <returns>The final result of the DataFlow run.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dataFlowName"/> or <paramref name="targetSinkName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the DataFlow execution did not result in a successful status.</exception>
        /// <exception cref="RequestFailedException">Thrown when one or more interactions with the Azure DataFactory resource failed.</exception>
        public async Task<DataFlowRunResult> RunDataFlowAsync(
            string dataFlowName,
            string targetSinkName,
            Action<RunDataFlowOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(dataFlowName))
            {
                throw new ArgumentException($"Name of the DataFlow in DataFactory '{DataFactory.Id.Name}' should not be blank", nameof(dataFlowName));
            }

            if (string.IsNullOrWhiteSpace(targetSinkName))
            {
                throw new ArgumentException($"Name of the target sink for the DataFlow '{dataFlowName}' in DataFactory '{DataFactory.Id.Name}' should not be blank", nameof(targetSinkName));
            }

            var options = new RunDataFlowOptions();
            configureOptions?.Invoke(options);

            await StartDataFlowAsync(dataFlowName, options);
            
            return await GetDataFlowResultAsync(dataFlowName, targetSinkName, options);
        }

        private async Task StartDataFlowAsync(string dataFlowName, RunDataFlowOptions options)
        {
            _logger.LogTrace("[Test:Setup] Adding DataFlow '{DataFlowName}' of DataFactory '{DataFactoryName}' to debug session", dataFlowName, DataFactory.Id.Name);
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

            await DataFactory.AddDataFlowToDebugSessionAsync(debug);
        }

        private DataFlowDebugPackageDebugSettings CreateDebugSettings(RunDataFlowOptions options)
        {
            var settings = new DataFlowDebugPackageDebugSettings();
            foreach (KeyValuePair<string, BinaryData> parameter in options.DataFlowParameters)
            {
                _logger.LogTrace("[Test:Setup] Add DataFlow parameter '{Name}' to debug session", parameter.Key);
                settings.Parameters[parameter.Key] = parameter.Value;
            }

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

        private async Task<DataFactoryDatasetResource> AddDataSetAsync(DataFactoryDataFlowDebugPackageContent debug, DataFactoryResource dataFactory, string datasetName)
        {
            _logger.LogTrace("[Test:Setup] Add DataSet '{DataSetName}' of DataFactory '{DataFactoryName}' to debug session", datasetName, dataFactory.Id.Name);

            DataFactoryDatasetResource dataset = await dataFactory.GetDataFactoryDatasetAsync(datasetName);
            debug.Datasets.Add(new DataFactoryDatasetDebugInfo(dataset.Data.Properties) { Name = dataset.Data.Name });

            return dataset;
        }

        private async Task AddLinkedServiceAsync(DataFactoryDataFlowDebugPackageContent debug, DataFactoryResource dataFactory, string serviceName)
        {
            _logger.LogTrace("[Test:Setup] Add LinkedService '{ServiceName}' of DataFactory '{DatFactoryName}' to debug session", serviceName, dataFactory.Id.Name);

            DataFactoryLinkedServiceResource linkedService = await dataFactory.GetDataFactoryLinkedServiceAsync(serviceName);
            debug.LinkedServices.Add(new DataFactoryLinkedServiceDebugInfo(linkedService.Data.Properties) { Name = linkedService.Data.Name });
        }

        private async Task<DataFlowRunResult> GetDataFlowResultAsync(string dataFlowName, string targetSinkName, RunDataFlowOptions options)
        {
            _logger.LogTrace("[Test] Run DataFlow '{DataFlowName}' until result is available on sink '{SinkName}'", dataFlowName, targetSinkName);

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
                    $"[Test] Executing DataFlow '{dataFlowName}' and waiting for a result in sink '{targetSinkName}' in DataFactory '{DataFactory.Id.Name}' " +
                    $"did not result in a successful status: '{result.Value.Status}', please check whether the DataFlow is correctly set up and can be run within a debug session");
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
                _logger.LogTrace("[Test:Teardown] Stop Azure DataFactory '{Name}' DataFlow debug session '{SessionId}'", DataFactory.Id.Name, SessionId);
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

        internal Collection<string> LinkedServiceNames { get; } = new();
        internal IDictionary<string, BinaryData> DataFlowParameters { get; } = new Dictionary<string, BinaryData>();

        /// <summary>
        /// Adds a parameter to the DataFlow to run.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        public RunDataFlowOptions AddDataFlowParameter(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("DataFlow parameter name should not be blank", nameof(name));
            }

            DataFlowParameters[name] = BinaryData.FromObjectAsJson(value);
            return this;
        }

        /// <summary>
        /// Gets or sets the limit of rows for the preview response of the DataFlow run (default: 100 rows).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        public int MaxRows
        {
            get => _maxRows;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value), value, "Requires a maximum row limit greater than zero for the preview response of the DataFlow run");
                }

                _maxRows = value;
            }
        }

        /// <summary>
        /// Adds an additional linked service to the Azure DataFactory debug session.
        /// </summary>
        /// <remarks>
        ///     This can be used to add, for example, Azure Key vault linked services that are needed when datasets require a vault for their authentication.
        /// </remarks>
        /// <param name="serviceName">The name of the linked service in Azure DataFactory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="serviceName"/> is blank.</exception>
        public RunDataFlowOptions AddLinkedService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException("Linked service name should not be blank", nameof(serviceName));
            }

            LinkedServiceNames.Add(serviceName);
            return this;
        }
    }
}
