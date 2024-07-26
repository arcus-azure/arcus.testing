using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the run options when calling the <see cref="TemporaryDataFlowDebugSessionsExtensions.RunDataFlowAsync(TemporaryDataFlowDebugSession,string,string,Action{RunDataFlowOptions})"/>.
    /// </summary>
    public class RunDataFlowOptions
    {
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
    }

    /// <summary>
    /// Represents the final result of the <see cref="TemporaryDataFlowDebugSessionsExtensions.RunDataFlowAsync(TemporaryDataFlowDebugSession,string,string)"/>.
    /// </summary>
    public class DataFlowRunResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowRunResult" /> class.
        /// </summary>
        public DataFlowRunResult(string status, BinaryData data)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status should not be blank", nameof(status));
            }

            Status = status;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Gets the run status of data preview, statistics or expression preview.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the result data of data preview, statistics or expression preview.
        /// </summary>
        public BinaryData Data { get; }
    }

    /// <summary>
    /// Extensions on the <see cref="TemporaryDataFlowDebugSession"/> to provide DataFlow-related functionality.
    /// </summary>
    public static class TemporaryDataFlowDebugSessionsExtensions
    {
        /// <summary>
        /// Starts a given DataFlow within the debug <paramref name="session"/>,
        /// which should give a result in the <paramref name="targetSinkName"/>.
        /// </summary>
        /// <param name="session">The active DataFlow debug session to start the DataFlow in.</param>
        /// <param name="dataFlowName">The name of the DataFlow to start.</param>
        /// <param name="targetSinkName">The name of the target sink to get the result from.</param>
        /// <returns>The final result of the DataFlow run.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="session"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dataFlowName"/> or <paramref name="targetSinkName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the DataFlow execution did not result in a successful status.</exception>
        /// <exception cref="RequestFailedException">Thrown when one or more interactions with the Azure DataFactory resource failed.</exception>
        public static async Task<DataFlowRunResult> RunDataFlowAsync(this TemporaryDataFlowDebugSession session, string dataFlowName, string targetSinkName)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (string.IsNullOrWhiteSpace(dataFlowName))
            {
                throw new ArgumentException($"Name of the DataFlow in DataFactory '{session.DataFactory.Data.Name}' should not be blank", nameof(dataFlowName));
            }

            if (string.IsNullOrWhiteSpace(targetSinkName))
            {
                throw new ArgumentException($"Name of the target sink for the DataFlow '{dataFlowName}' in DataFactory '{session.DataFactory.Data.Name}' should not be blank", nameof(targetSinkName));
            }

            return await RunDataFlowAsync(session, dataFlowName, targetSinkName, configureOptions: null);
        }

        /// <summary>
        /// Starts a given DataFlow within the debug <paramref name="session"/>,
        /// which should give a result in the <paramref name="targetSinkName"/>.
        /// </summary>
        /// <param name="session">The active DataFlow debug session to start the DataFlow in.</param>
        /// <param name="dataFlowName">The name of the DataFlow to start.</param>
        /// <param name="targetSinkName">The name of the target sink to get the result from.</param>
        /// <param name="configureOptions">The function to configure the options of the DataFlow run.</param>
        /// <returns>The final result of the DataFlow run.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="session"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="dataFlowName"/> or <paramref name="targetSinkName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the DataFlow execution did not result in a successful status.</exception>
        /// <exception cref="RequestFailedException">Thrown when one or more interactions with the Azure DataFactory resource failed.</exception>
        public static async Task<DataFlowRunResult> RunDataFlowAsync(
            this TemporaryDataFlowDebugSession session,
            string dataFlowName,
            string targetSinkName,
            Action<RunDataFlowOptions> configureOptions)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (string.IsNullOrWhiteSpace(dataFlowName))
            {
                throw new ArgumentException($"Name of the DataFlow in DataFactory '{session.DataFactory.Data.Name}' should not be blank", nameof(dataFlowName));
            }

            if (string.IsNullOrWhiteSpace(targetSinkName))
            {
                throw new ArgumentException($"Name of the target sink for the DataFlow '{dataFlowName}' in DataFactory '{session.DataFactory.Data.Name}' should not be blank", nameof(targetSinkName));
            }

            var options = new RunDataFlowOptions();
            configureOptions?.Invoke(options);

            await StartDataFlowAsync(session, dataFlowName, options);
            
            return await GetDataFlowResultAsync(session, dataFlowName, targetSinkName);
        }

        private static async Task StartDataFlowAsync(TemporaryDataFlowDebugSession session, string dataFlowName, RunDataFlowOptions options)
        {
            DataFactoryDataFlowResource dataFlow = await session.DataFactory.GetDataFactoryDataFlowAsync(dataFlowName);

            var debug = new DataFactoryDataFlowDebugPackageContent
            {
                DataFlow = new DataFactoryDataFlowDebugInfo(dataFlow.Data.Properties) { Name = dataFlowName },
                DebugSettings = CreateDebugSettings(options),
                SessionId = session.SessionId
            };

            await debug.AddDebugVariantsOfDataFlowSourcesAsync(session.DataFactory, dataFlow);
            await debug.AddDebugVariantsOfDataFlowSinksAsync(session.DataFactory, dataFlow);
            await debug.AddLinkedServiceAsync(session.DataFactory, "datafactory_sales_keyvaultLS");

            await session.DataFactory.AddDataFlowToDebugSessionAsync(debug);
        }

        private static DataFlowDebugPackageDebugSettings CreateDebugSettings(RunDataFlowOptions options)
        {
            var settings = new DataFlowDebugPackageDebugSettings();
            foreach (KeyValuePair<string, BinaryData> parameter in options.DataFlowParameters)
            {
                settings.Parameters[parameter.Key] = parameter.Value;
            }

            return settings;
        }

        private static async Task AddDebugVariantsOfDataFlowSourcesAsync(
            this DataFactoryDataFlowDebugPackageContent debug,
            DataFactoryResource dataFactory,
            DataFactoryDataFlowResource dataFlow)
        {
            if (dataFlow.Data.Properties is DataFactoryMappingDataFlowProperties properties)
            {
                foreach (DataFlowSource source in properties.Sources)
                {
                    debug.DebugSettings.SourceSettings.Add(new DataFlowSourceSetting { SourceName = source.Name, RowLimit = 100 });

                    DataFactoryDatasetResource dataset = await debug.AddDataSetAsync(dataFactory, source.Name);
                    await debug.AddLinkedServiceAsync(dataFactory, dataset.Data.Properties.LinkedServiceName.ReferenceName);
                }
            }
        }

        private static async Task AddDebugVariantsOfDataFlowSinksAsync(
            this DataFactoryDataFlowDebugPackageContent debug,
            DataFactoryResource dataFactory,
            DataFactoryDataFlowResource dataFlow)
        {
            if (dataFlow.Data.Properties is DataFactoryMappingDataFlowProperties properties)
            {
                foreach (DataFlowSink sink in properties.Sinks)
                {
                    DataFactoryDatasetResource dataset = await debug.AddDataSetAsync(dataFactory, sink.Dataset.ReferenceName);
                    await debug.AddLinkedServiceAsync(dataFactory, dataset.Data.Properties.LinkedServiceName.ReferenceName);
                }
            }
        }

        private static async Task<DataFactoryDatasetResource> AddDataSetAsync(this DataFactoryDataFlowDebugPackageContent debug, DataFactoryResource dataFactory, string datasetName)
        {
            DataFactoryDatasetResource dataset = await dataFactory.GetDataFactoryDatasetAsync(datasetName);
            debug.Datasets.Add(new DataFactoryDatasetDebugInfo(dataset.Data.Properties) { Name = dataset.Data.Name });

            return dataset;
        }

        private static async Task AddLinkedServiceAsync(this DataFactoryDataFlowDebugPackageContent debug, DataFactoryResource dataFactory, string serviceName)
        {
            DataFactoryLinkedServiceResource linkedService = await dataFactory.GetDataFactoryLinkedServiceAsync(serviceName);
            debug.LinkedServices.Add(new DataFactoryLinkedServiceDebugInfo(linkedService.Data.Properties) { Name = linkedService.Data.Name });
        }

        private static async Task<DataFlowRunResult> GetDataFlowResultAsync(TemporaryDataFlowDebugSession session, string dataFlowName, string targetSinkName)
        {
            ArmOperation<DataFactoryDataFlowDebugCommandResult> result = 
                await session.DataFactory.ExecuteDataFlowDebugSessionCommandAsync(WaitUntil.Completed, new DataFlowDebugCommandContent
                {
                    Command = "executePreviewQuery",
                    CommandPayload = new DataFlowDebugCommandPayload(targetSinkName)
                    {
                        RowLimits = 100
                    },
                    SessionId = session.SessionId
                });

            if (result.Value.Status != "Succeeded")
            {
                throw new InvalidOperationException(
                    $"Executing DataFlow '{dataFlowName}' in DataFactory '{session.DataFactory.Data.Name}' did not result in a successful status: '{result.Value.Status}', " +
                    $"cannot return result");
            }

            return new DataFlowRunResult(result.Value.Status, BinaryData.FromString(result.Value.Data));
        }
    }
}
