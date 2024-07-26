using System;
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
    }

    /// <summary>
    /// Represents a temporary active debug session to link data flows under test.
    /// </summary>
    public class TemporaryDataFlowDebugSession : IAsyncDisposable
    {
        private readonly ILogger _logger;

        private TemporaryDataFlowDebugSession(Guid? sessionId, DataFactoryResource resource, ILogger logger)
        {
            DataFactory = resource ?? throw new ArgumentNullException(nameof(resource));
            _logger = logger ?? NullLogger.Instance;

            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        }

        /// <summary>
        /// Gets the DataFactory resource where the active DataFlow debug session is started.
        /// </summary>
        internal DataFactoryResource DataFactory { get; }

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
            return await StartDebugSessionAsync(
                dataFactoryResourceId ?? throw new ArgumentNullException(nameof(dataFactoryResourceId)),
                logger,
                configureOptions: null);
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
            if (dataFactoryResourceId is null)
            {
                throw new ArgumentNullException(nameof(dataFactoryResourceId));
            }

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
            return await StartDebugSessionAsync(resource ?? throw new ArgumentNullException(nameof(resource)), logger, configureOptions: null);
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
            if (resource is null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            logger ??= NullLogger.Instance;

            var options = new TemporaryDataFlowDebugSessionOptions();
            configureOptions?.Invoke(options);

            ArmOperation<DataFactoryDataFlowCreateDebugSessionResult> result = 
                await resource.CreateDataFlowDebugSessionAsync(WaitUntil.Completed, new DataFactoryDataFlowDebugSessionContent
                {
                    TimeToLiveInMinutes = options.TimeToLiveInMinutes
                });

            Guid sessionId = result.Value.SessionId ?? throw new InvalidOperationException($"Starting DataFactory '{resource.Data.Name}' DataFlow debug session did not result in a session ID");
            logger.LogTrace("Started DataFactory '{Name}' DataFlow debug session '{SessionId}'", resource.Data.Name, sessionId);

            return new TemporaryDataFlowDebugSession(sessionId, resource, logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace("End DataFactory '{Name}' DataFlow debug session '{SessionId}'", DataFactory.Data.Name, SessionId);
            await DataFactory.DeleteDataFlowDebugSessionAsync(new DeleteDataFlowDebugSessionContent { SessionId = SessionId });
        }
    }
}
