# Migrate your test suite from Arcus.Testing v1 to v2
This guide will walk you through the process of migrating your test suite from the Arcus.Testing v1 to the new major v2 release.

## General
* 🗑️ .NET 6 support is removed
Starting from v3, all `Arcus.Testing.*` packages solely support .NET 8 and stop supporting .NET 6.

## 📦 Arcus.Testing.Logging.*
### 👋 Arcus.Testing.Logging.Core is archived
Starting from v3, the `Arcus.Testing.Logging.Core` package of the set of logging packages is being archived and is not included anymore as a transient reference in any of the testing framework-specific packages.

This means that the following types are not included anymore when you install v3 via a testing framework package (xUnit, NUnit, MSTest):
* `InMemoryLogger`
* `InMemoryLogger<>`
* `CustomLoggerProvider`
* `InMemoryLogSink`
* `LogEntry`

This also means that the following packages will not be transiently available anymore:
* **Serilog**

## 📦 Arcus.Testing.Storage.Blob
### `BlobNameFilter` ➡️ `Func<BlobItem, bool>`
Previous versions had a dedicated type called `BlobNameFilter` to filter out certain Azure Blob items subject for deletion during the setup/teardown of the container. The type has been removed in v3 in favor of a built-in delegation.

```diff
TemporaryBlobContainer.CreateIfNotExistsAsync(..., options =>
{
    options.OnSetup/Teardown.CleanMatchingBlobs(
-        BlobNameFilter.NameContains("test"),
-        BlobNameFilter.NameStartsWith("temp-)
+        (BlobItem blob) => blob.Name.Contains("test"),
+        (BlobItem blob) => blob.Name.StartsWith("temp-")
    );
});
```

### `TemporaryBlobFileOptions` ➡️ `TemporaryBlobContainerOptions`
Previous versions had additional options on the `TemporaryBlobFile` test fixture to override/use Azure Blob files. This and the entire options on this fixture has been removed in v3 - as it is already implicitly available on the `TemporaryBlobContainerOptions`.

1. When you want to 'use an existing blob file instead of overriding it + remove it nonetheless afterwards' you can use either `.CleanAllBlobs` or `.CleanMatchingBlobs` on the container options:
    ```diff
    - TemporaryBlobFile.UploadIfNotExistsAsync(..., options =>
    - {
    -     options.OnSetup.UseExistingBlob();
    -     options.OnTeardown.DeleteExistingBlob();
    - });
    + TemporaryBlobContainer.CreateIfNotExistsAsync(..., options =>
    + {
    +     options.OnSetup.LeaveAllBlobs();
    +     options.OnTeardown.CleanAllBlobs();
    + })
    ```
2. When you want to 'override an existing blob + revert it afterwards' you can use `.UpsertFileAsync` on the container:
    ```diff
    - TemporaryBlobFile.UploadIfExistsAsync(..., options =>
    - {
    -     options.OnSetup.OverrideExistingBlob();
    -     options.OnTeardown.DeleteCreatedBlob();
    - });
    + await using TemporaryBlobContainer container = ...
    + await container.UpsertBlobFileAsync(...);
    ```

## 📦 Arcus.Testing.Storage.Cosmos
### `Newtonsoft.Json` ➡️ `System.Text.Json`
Starting from v2, the `Newtonsoft.Json` package is not included by default anymore, since Microsoft has removed this from its transient dependencies (See also [this GitHub issue](https://github.com/Azure/azure-cosmos-dotnet-v3/issues/4900)).

Before v1, we depended on a transient package to provide NoSQL item filters for setup and teardown operations. In v2, the provided item differs slightly, as it now builds on top of the built-in System.Text.Json package.

```diff
TemporaryNoSqlContainer.CreateIfNotExistsAsync(..., options =>
{
    options.OnSetup.CleanMatchingItems((NoSqlItem item) =>
    {
-        return item["JsonProperty"].Value<string>() == "JsonValue";
+        return item.Content["JsonProperty"].GetValue<string>() == "JsonValue";
    });
});
```

### `NoSqlItemFilter` ➡️ `Func<NoSqlItem, bool>`
Previous versions had a dedicated type called `NoSqlItemFilter` to filter out certain Azure Cosmos NoSql items subject for deletion during the setup/teardown of the container. The type has been removed in v2 in favor of a built-in delegation.

```diff
TemporaryNoSqlContainer.CreateIfNoExistsAsync(..., options =>
{
    options.OnSetup/Teardown.CleanMatchingItems(
-        NoSqlItemFilter.PartitionKeyEqual(new PartitionKey("test")),
+        (NoSqlItem item) => item.PartitionKey.Equals(new PartitionKey("test")));

    options.OnSetup/Teardown.CleanMatchingItems(
-        NoSqlItemFilter.Where<Shipment>(s => s.BoatName == "The Alice")
+        (Shipment s) => s.BoatName == "The Alice"
    );
});
```

## 📦 Arcus.Testing.Storage.Table
### `TableEntityFilter` ➡️ `Func<TableEntity, bool>`
Previous versions had a dedicated type called `TableEntityFilter` to filter out certain Azure Table entities subject for deletion during the setup/teardown of the table. The type has been removed in v2 in favor of a built-in delegation.

```diff
TemporaryTable.CreateIfNotExistsAsync(..., options =>
{
    options.OnSetup/Teardown.CleanMatchingEntities(
-        TableEntityFilter.PartitionKeyEqual("test"),
-        TableEntityFilter.EntityEqual((TableEntity entity) => entity["Version"] == "v1.0.0-beta")
+        (TableEntity entity) => entity.PartitionKey == "test",
+        (TableEntity entity) => entity["Version"] == "v1.0.0-beta"
    );
});
```