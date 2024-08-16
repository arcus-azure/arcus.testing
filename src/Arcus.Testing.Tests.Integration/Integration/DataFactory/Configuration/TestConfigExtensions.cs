using Azure.Core;

// ReSharper disable once CheckNamespace
namespace Arcus.Testing
{
    public class DataFactoryConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFactoryConfig" /> class.
        /// </summary>
        public DataFactoryConfig(
            string subscriptionId, 
            string resourceGroupName, 
            string resourceName,
            DataFlowConfig dataFlowCsv,
            DataFlowConfig dataFlowJson)
        {
            Name = resourceName;
            ResourceId = ResourceIdentifier.Parse($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DataFactory/factories/{resourceName}");

            DataFlowCsv = dataFlowCsv;
            DataFlowJson = dataFlowJson;
        }

        public string Name { get; }

        public ResourceIdentifier ResourceId { get; }

        public DataFlowConfig DataFlowCsv { get; }
        public DataFlowConfig DataFlowJson { get; }
    }

    public class DataFlowConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowConfig" /> class.
        /// </summary>
        public DataFlowConfig(string name, DataFlowSourceConfig source, string sinkName)
        {
            Name = name;
            Source = source;
            SinkName = sinkName;
        }

        public string Name { get; }

        public string SinkName { get; }

        public DataFlowSourceConfig Source { get; }
    }

    public class DataFlowSourceConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFlowSourceConfig" /> class.
        /// </summary>
        public DataFlowSourceConfig(string containerName, string fileName)
        {
            ContainerName = containerName;
            FileName = fileName;
        }

        public string FileName { get; }
        public string ContainerName { get; }
    }

    public static class TestConfigExtensions
    {
        public static DataFactoryConfig GetDataFactory(this TestConfig config)
        {
            var dataFlowCsv = new DataFlowConfig(
                config["Arcus:DataFactory:DataFlow:Csv:Name"],
                new DataFlowSourceConfig(
                    config["Arcus:DataFactory:DataFlow:Source:BlobStorage:ContainerName"],
                    config["Arcus:DataFactory:DataFlow:Csv:SourceFileName"]),
                config["Arcus:DataFactory:DataFlow:Csv:SinkName"]);

            var dataFlowJson = new DataFlowConfig(
                config["Arcus:DataFactory:DataFlow:Json:Name"],
                new DataFlowSourceConfig(
                    config["Arcus:DataFactory:DataFlow:Source:BlobStorage:ContainerName"],
                    config["Arcus:DataFactory:DataFlow:Json:SourceFileName"]),
                config["Arcus:DataFactory:DataFlow:Json:SinkName"]);

            return new DataFactoryConfig(
                config["Arcus:SubscriptionId"],
                config["Arcus:ResourceGroup:Name"],
                config["Arcus:DataFactory:Name"],
                dataFlowCsv,
                dataFlowJson);
        }
    }
}