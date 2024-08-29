# CosmosDb storage
The `Arcus.Testing.Storage.CosmosDb` package provides test fixtures to Azure CosmosDb storage. By using the common test practices 'clean environment', it provides a temporary collections and documents.

## Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Storage.CosmosDb
```

## MongoDb
### Temporary MongoDb collection
The `TemporaryMongoDbCollection` provides a solution when the integration tes requires a storage system (collection) during the test run. An MongoDb collection is created upon setup of the test fixture and is deleted again when the test fixture disposed.

> ⚡ Whether or not the test fixture should use an existing collection is configurable.

```csharp
using Arcus.Testing;

ResourceIdentifier cosmosDbAccountResourceId = ...

await using var collection = await TemporaryMongoDbCollection.CreateIfNotExistsAsync(
    cosmosDbAccountResourceId,
    "<database-name>",
    "<collection-name>",
    logger);

// Interact with the collection during the lifetime of the fixture.
IMongoCollection<Shipment> client = collection.GetCollectionClient<Shipment>();
```

> 💡 Overloads are available that takes in the `IMongoDatabase` for custom authentication mechanism.

Inserting documents in the collection can also be done via the test fixture. It always make sure that any inserted documents are removed afterwards.

```csharp
using Arcus.Testing;

await using TemporaryMongoDbCollection collection = ...

await collection.InsertDocumentAsync(new Shipment { BoatName = "The Alice"  });
```

#### Customization
The setup and teardown process of the temporary collection is configurable. **By default, it always removes any resources that it was responsible for creating.**

```csharp
using Arcus.Testing;

await TemporaryMongoDbCollection.CreateIfNotExistsAsync(..., options =>
{
    // Options related to when the test fixture is set up.
    // ---------------------------------------------------

    // (Default) Leaves all existing MongoDb documents untouched when the test fixture is created.
    options.OnSetup.LeaveAllDocuments();

    // Delete all MongoDb documents that already existed before the test fixture creation, when there was already a MongoDb collection available.
    options.OnSetup.CleanAllDocuments();

    // Delete all MongoDb documents that matches any of the configured filters,
    // upon the test fixture creation, when there was already a MongoDb collection available.
    options.OnSetup.CleanMatchingDocuments(
        Builders<Shipment>.Filter.Eq(s => s.BoatName, "The Alice"));
    
    options.OnSetup.CleanMatchingDocuments((Shipment s) => s.BoatName == "The Alice");

    // Options related to when the test fixture is teared down.
    // --------------------------------------------------------

    // (Default) Delete all MongoDb documents that were inserted by the test fixture.
    options.OnTeardown.CleanCreatedDocuments();

    // Delete all MongoDb documents upon the test fixture disposal,
    // even if the test fixture didn't inserted them.
    options.OnTeardown.CleanAllDocuments();

    // Delete all MongoDb documents tht matches any of the configured filters,
    // upon the test fixture disposal, even if the test fixture didn't inserted them.
    // ⚠️ MongoDb documents inserted by the test fixture will always be deleted, regardless of the configured filters.
    options.OnTeardown.CleanMatchingDocuments(
      Builders<Shipment>.Filter.Eq(s => s.BoatName, "The Alice"));

    options.OnTeardown.CleanMatchingDocuments((Shipment s) => s.BoatName == "The Alice");
});
```

> 🎖️ The `TemporaryMongoDbCollection` will always remove any MongoDb documents that were uploaded on the temporary collection itself with the `collection.InsertDocumentAsync`. This follows the 'clean environment' testing principal that any test should not leave any state it created behind after the test has run.

### Temporary MongoDb document
The `TemporaryMongoDbDocument` provides a solution when the integration test requires a document during the test run. A MongoDb document is created upon the setup of the test fixture and is deleted again when the test fixture is disposed.

When the document already existed (already a document with a same configured document ID), then the test fixture will revert to the original content of the document upon the test fixture disposal.

> ⚡ Whether or not the test fixture should use an existing document is set by using n existing document ID (`_id` by default).

```csharp
using Arcus.Testing;

ResourceIdentifier cosmosDbAccountResourceId = ...

await using var document = await TemporaryMongoDbDocument.InsertIfNotExistsAsync(
    cosmosDbAccountResourceId,
    "<database-name>",
    "<collection-name>",
    new Shipment { BoatName = "The Alice" },
    logger);

string documentId = document.Id;
```

> ⚠️ Currently, only `ObjectId` is supported as the document ID. 