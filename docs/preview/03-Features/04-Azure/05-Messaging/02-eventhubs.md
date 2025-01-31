# Event Hubs
The `Arcus.Testing.Messaging.EventHubs` package provides test fixtures related to Azure Event Hubs. By using the common testing practice 'clean environment', it provides a temporary hub.

## Installation
The following functionality is available when installing this package:

```powershell
PM> Install-Package -Name Arcus.Testing.Messaging.EventHubs
```

## Temporary hub
The `TemporaryEventHub` provides a solution when the integration test requires an Azure Event Hub during the test run. A hub is created upon the upon the setup of the test fixture and is deleted again when the fixture is disposed.

> ✨ Only when the test fixture was responsible for creating the hub, will the hub be deleted upon the fixture's disposal. This follows the 'clean environment' testing principle that describes that after the test run, the same state should be achieved as before the test run.

```csharp
using Arcus.Testing;

ResourceIdentifier eventHubsNamespaceResourceId =
    EventHubsNamespaceResource.CreateResourceIdentifier("<subscription-id", "<resource-group>", "<namespace-name>");

await using var hub = await TemporaryEventHub.CreateIfNotExistsAsync(
    eventHubsNamespaceResourceId, consumerGroup: "$Default", "<event-hub-name>", logger);
```

> ⚡ Uses by default the [`DefaultAzureCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential) but other type of authentication mechanisms are supported with overloads that take in the `EventHubsNamespaceResource` directly:
> ```csharp
>  var credential = new DefaultAzureCredential();
>  var arm = new ArmClient(credential);
>  
>  EventHubsNamespaceResource eventHubsNamespace =
>      await arm.GetEventHubsNamespaceResource(eventHubNamespaceResourceId)
>               .GetAsync();
> ```

### Customization
The `TemporaryEventHub` allows testers to configure setup operations to manipulate the test fixture's behavior.

```csharp
using Arcus.Testing;

await TemporaryEventHub.CreateIfNotExistsAsync(..., options =>
{
    // Options related to when the test fixture is set up.
    // ---------------------------------------------------

    // Change the default hub-creation behavior.
    options.OnSetup.CreateHubWith((EventHubData hub) =>
    {
        hub.PartitionCount = 4;
    });
});
```

### Search for events
The `TemporaryEventHub` is equipped with an event filtering system that allows testers to search for events during the lifetime of the test fixture. This can be useful to verify the current state of a hub, or as a test assertion to verify EventHubs-related implementations.

```csharp
using Arcus.Testing;

await using TemporaryEventHub hub = ...

IEnumerable<PartitionEvent> events =
    await hub.Events

              // Get subset events currently on the hub.
             .Where(ev => ev.Data.Properties.ContainsKey("<my-key"))
             .Where(ev => ev.Data.ContentType == "application/json")

              // Get events only from a single partition.
             .FromPartition("<partition-id>", EventPosition.Earliest)

              // Configures the read options that will be associated with the search operation.
             .ReadWith((ReadEventOptions opt) =>
             {
                 opt.MaximumWaitTime = TimeSpan.FromSeconds(10);
                 opt.OwnerLevel = 10;
             })

             // Start searching for events.
             .ToListAsync();
```