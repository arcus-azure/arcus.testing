# Migrate your test suite from Arcus.Testing v1 to v2
This guide will walk you through the process of migrating your test suite from the Arcus.Testing v1 to the new major v2 release.

## `Arcus.Testing.Storage.Cosmos`
Starting from v2, the `Newtonsoft.Json` package is not included by default anymore, since Microsoft has removed this from its transient dependencies (See also [this GitHub issue](https://github.com/Azure/azure-cosmos-dotnet-v3/issues/4900)).

Before v1, we also dependent on this transient package when providing NoSql item filters for setup/teardown operations. Now, in v2, the provided item is slightly different as we now built on top of the built-in `System.Text.Json` package.

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