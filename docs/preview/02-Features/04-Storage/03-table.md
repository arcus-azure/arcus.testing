# Table storage
The `Arcus.Testing.Storage.Table` package provides test fixtures related to Azure Table storage. By using the common testing practice 'clean environment', it provides a temporary Table and Table entity.

## Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Storage.Table
```

## Temporary Table
The `TemporaryTable` provides a solution when the integration test requires a storage system (table) during the test run. An Azure Table is created upon the setup of the test fixture and is deleted again when the test fixture is disposed.

```csharp
using Arcus.Testing;

await using var table = await TemporaryTable.CreateIfNotExistsAsync(
    "<account-name", "<table-name>", logger);

// Interact with the table during the lifetime of the fixture.
TableClient client = table.Client;
```

> 🎖️ Overloads are available to fully control the authentication mechanism to Azure Table storage. By default, it uses the [`DefaultAzureCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential).

Adding entities to the table can also be done via the test fixture. It always make sure that any added entities will be removed afterwards upon disposal.

```csharp
using Arcus.Testing;

await using TemporaryTable table = ...

ITableEntity myEntity = ...
await table.AddEntityAsync(myEntity);
```

### Customization
The setup and teardown process of the temporary table is configurable. **By default, it always removes any resources that it was responsible for creating.**

```csharp
using Arcus.Testing;

await TemporaryTable.CreateIfNotExistsAsync(..., options =>
{
    // Options related to when the test fixture is set up.
    // ---------------------------------------------------

    // (Default) Leaves all Azure Table entities untouched that already existed,
    // upon the test fixture creation, when there was already an Azure Table available.
    options.OnSetup.LeaveAllEntities();

    // Delete all Azure Table entities upon the test fixture creation, 
    // when there was already an Azure Table available.
    options.OnSetup.CleanAllEntities();

    // Delete Azure Table entities that matches any of the configured filters, 
    // upon the test fixture creation, when there was already an Azure Table available.
    options.OnSetup.CleanMatchingEntities(
      TableEntityFilter.RowKeyEqual("<row-key>"),
      TableEntityFilter.PartitionKeyEqual("<partition-key>"),
      TableEntityFilter.EntityEqual((TableEntity entity) => entity["Key"] == "Value"));

    // Options related to when the test fixture is teared down.
    // --------------------------------------------------------

    // (Default for cleaning entities)
    // Delete Azure Table entities upon the test fixture disposal that were added by the fixture.
    options.OnTeardown.CleanCreatedEntities();

    // Delete all Azure Table entities upon the test fixture disposal, 
    // even if the test fixture didn't added them.
    options.OnTeardown.CleanAllEntities();

    // Delete additional Azure Table entities that matches any of the configured filters, 
    // upon the test fixture disposal.
    // ⚠️ Entities added by the test fixture itself will always be deleted.
    options.OnTeardown.CleanMatchingEntities(
      TableEntityFilter.RowKeyEqual("<row-key>"),
      TableEntityFilter.PartitionKeyEqual("<partition-key>"),
      TableEntityFilter.EntityEqual((TableEntity entity) => entity["Key"] == "Value"));
});

// `OnTeardown` is also still available after the temporary table is created:
await using TemporaryTable table = ...

table.OnTeardown.CleanAllEntities();
```

> 🎖️ The `TemporaryTable` will always remove any Azure Table entities that were added on the temporary table  itself with the `table.AddEntityAsync`. This follows the 'clean environment' testing principal that any test should not leave any state it created behind after the test has run.

## Temporary Table entity
The `TemporaryTableEntity` provides a solution when the integration test requires data (table) during the test run. An Azure Table entity is added upon the setup of the test fixture and is deleted again when the test fixture is disposed.

```csharp
using Arcus.Testing;

ITableEntity entity = ...

await using var entity = await TemporaryTableEntity.AddIfNotExistsAsync(
    "<account-name>", "<table-name>", entity, logger);
```

> 🎖️ Overloads are available to fully control the authentication mechanism to Azure Table storage. By default, it uses the [`DefaultAzureCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential).