---
sidebar_label: Core infrastructure
---

# Test core infrastructure
The `Arcus.Testing.Core` package provides general testing infrastructure that is independent of technology, SUT (system-under-test) or testing framework.
The features provided in this package are very often used and/or required for a functional integration/system test suite.

## Installation
The following infrastructure is available when installing this package:

```powershell
PM> Install-Package -Name Arcus.Testing.Core
```

## Test configuration
The `TestConfig` (implements Microsoft's [`IConfiguration`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration)) provides a solution on retrieving application configuration values during testing. Integration/system tests often require values that are only known at runtime. These values can be injected into an `appsettings.json` in your test project, but without a good configuration setup, retrieving and using those values can often be obscure (what with missing/blank values, for example?).

The default `TestConfig` uses the `appsettings.json` as main file where the tokens could be set, for example:
```json
{
    "MyProject": {
      "MyEndpoint": "#{Published.Endpoint}#"
    }
}
```

> ⚠️ Make sure that you have such an `appsettings.json` file in your project and that this is copied to the test project's output. 

The default local alternative is called `appsettings.local.json` (for local integration/system testing) could include the endpoint to a locally run endpoint:
```json
{
    "MyProject": {
      "MyEndpoint": "http://localhost:8081/"
    }
}
```

The `appsettings.json` will have a token replacement when running the tests remotely, the `appsettings.local.json` will have the values for locally running the tests (⚠️ **should be not checked-in into source control!**).

A test requires only a single `TestConfig` to retrieve the values.
```csharp
// Default test configuration, uses 'appsettings.json' as main and 'appsettings.local.json' as single local alternative.
var config = TestConfig.Create();

string endpoint = config["MyProject:MyEndpoint"];

string _ = config["Unknown:Value"];
// Failure:
//  Cannot find any non-blank test configuration value for the key: 'Unknown:Value', 
//  please make sure that this key is specified in your (local or remote) 'appsettings.json' file 
//  and it is copied to the build output in your .csproj/.fsproj project file: <CopyToOutputDirectory>Always/CopyToOutputDirectory>
```

### Customization

The `TestConfig` can be customized with additional local alternatives (like for different environment testing) and with a different main JSON file.

```csharp
var config = TestConfig.Create(options =>
{
    // Overrides 'appsettings.json' -> 'configuration.json'
    options.UseMainJsonFile("configuration.json");

    // Adds 'configuration.Dev.json' to local alternatives [ 'appsettings.local.json' ]
    options.AddOptionalJsonFile("configuration.Dev.json");
});
```

It can also be used as a base for your custom configuration, by inheriting from the `TestConfig`:

```csharp
public class MyTestConfig : TestConfig
{
    // Uses default 'appsettings.json' and 'appsettings.local.json' as local alternative.
    // Same as doing `TestConfig.Create()`
    public MyTestConfig()

    public MyTestConfig() : base(options => options.UseMainJsonFile("configuration.json")) { }
}
```

💡 The added benefit from having your own instance of the test configuration, is that you are free to add project-specific properties and methods, without exposing Arcus functionality directly to your tests.

## Polling
Writing integration tests interacts by definition with external resources. These resources might not always be available at the time the test needs them. Because of this, polling until the target resource is available is a common practice. An example is polling for a health endpoint until the application response 'healthy'.

It's important to understand that polling is different from delaying/waiting. The fundamental difference is that polling will stop when the target resource is available, while delaying/waiting will always happen, regardless of the state of the resource.

The `Poll` provides an easy way to describe these polling operations in a fluent manner. Different options are available for both polling until an interaction stops throwing exceptions or when a result is returning the expected value.

```csharp
using Arcus.Testing;

// Poll until file is available - stops throwing `FileNotFoundException`:
// When no exception is specified, all are considered failures.
string txt = 
    await Poll.UntilAvailableAsync<FileNotFoundException>(
        () => File.ReadAllTextAsync("..."));

// Poll until HTTP status code is `200 OK`:
// When no 'until' filters are specified, all results are considered valid. 
HttpResponseMessage response = 
    await Poll.Target(() => HttpClient.GetAsync("..."))
              .Until(resp => resp.StatusCode == HttpStatusCode.OK)
              .StartAsync();
```

> 💡 The returned `Poll` model implements `GetAwaiter`, which means that the `StartAsync` is optional.

### Customization
The `Poll` model can be customized with several options to override existing functionality or to manipulate the polling operations to your needs.

```csharp
// Set options directly.
await Poll.UntilAvailableAsync(..., options =>
{
    // Sets the interval between each poll operation (default: 1 second).
    options.Interval = TimeSpan.FromMilliseconds(500);

    // Sets the time frame in which the polling operation has to succeed (default: 30 seconds).
    options.Timeout = TimeSpan.FromSeconds(5);

    // Sets the message that describes the failure of the polling operation.
    options.FailureMessage = "my polling operation description that gives the test failure more context";
});

// Set options fluently.
await Poll.Target(...)
          .Until(...)
          .Every(TimeSpan.FromMilliseconds(500))
          .Timeout(TimeSpan.FromSeconds(5))
          .FailWith("my polling operation description that gives the test failure more context")
          .StartAsync();
```

> 💡 Try to come up with a sweet spot that does not wait too long for the target resource, but takes enough margin to be run on any environment, in all conditions.

## Resource directory
The `ResourceDirectory` provides a solution to retrieving local files during the test run. It points by default to the root output directory where the test suite is running, and from there any sub-directory can be navigated to in a test-friendly manner. Each time a directory or a file does not exists, an IO exception will be thrown with a clear message on what is missing on disk.

```csharp
using Arcus.Testing;

// Path: /bin/net8.0/
ResourceDirectory root = ResourceDirectory.CurrentDirectory;

string txt = root.ReadFileTextByName("file.txt");
byte[] img = root.ReadFileBytesByName("file.png");

// Path: /bin/net8.0/resources
ResourceDirectory sub = root.WithSubDirectory("resources");

string txt = sub.ReadFileTextByName("file.txt");
byte[] img = sub.ReadFileBytesByPattern("*.png");


// FileNotFoundException: 
//    Cannot retrieve 'file.txt' file contents in test resource directory 'resources' because it does not exists,
//    make sure that the test resource files are always copied to the output before loading their contents.
//    File path: /bin/net8.0/resources/file.txt
//    Resource directory: /bin/net8.0/resources
```

## Temporary environment variable
The `TemporaryEnvironmentVariable` provides a solution when the test needs to set certain environment information on the hosting system itself. This is fairly common when testing locally and spinning up the application on your own system. It can also be used for authentication, like managed identity connections. The test fixture will temporarily set or override an environment variable and remove or revert it upon disposal.

> ⚡ By using one of the exposed methods, you can specify if you want the original/new environment variables to be written to the test output. With secrets, only the names will be written.

```csharp
using Arcus.Testing;

// Set a public known application value.
using var tenantId = TemporaryEnvironmentVariable.SetIfNotExists("AZURE_TENANT_ID", "<tenant-id>");
// > [Test:Setup] Set new environment variable 'AZURE_TENANT_ID' with '<tenant-id>'

// Set private application secret.
using var clientSecret = TemporaryEnvironmentVariable.SetSecretIfNotExits("AZURE_CLIENT_SECRET", "<client-secret>");
// > [Test:Setup] Set new secret environment variable 'AZURE_CLIENT_SECRET'

```

## Disposable collection
The `DisposableCollection` provides a solution for when multiple temporary/disposable test fixtures need to be teared down independently from each other, meaning: when one test fixture fails, it should not stop another fixture from tearing down. Multiple synchronous/asynchronous test fixtures can be added to the collection. Upon disposing the collection itself, it will try to dispose each registered test fixture. When one or more failures occur, it will collect them and throw an `AggregateException`.

```csharp
using Arcus.Testing;

await using var disposables = new DisposableCollection(logger);

disposables.Add(temporaryEnvironmentVariable);
disposables.Add(temporaryStorageState);

// Dispose:
//   - removing temporary environment variable
//   - reverting storage state
```

### Customization

The teardown of each test fixture will be retried, in case of a delay during network interaction, for example.

```csharp
await using var disposables = new DisposableCollection(logger);

// The amount of times a failed test fixture's disposal should be retried.
disposables.Options.RetryCount = 5;

// The time interval between each failed test fixture's disposal retry.
disposables.Options.RetryInterval = TimeSpan.FromSeconds(5);
```