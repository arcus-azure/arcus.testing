# Core testing functionality

The `Arcus.Testing.Core` package provides general testing functionality that is independent of technology, SUT (system-under-test) testing framework.
The features provided in this package are very often used and/or required for a functional integration/system test suite.

## Installation

The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Core
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
byte[] img = sub.ReadFileBytesByName("file.png");

// FileNotFoundException: 
//    Cannot retrieve 'file.txt' file contents in test resource directory 'resources' because it does not exists,
//    make sure that the test resource files are always copied to the output before loading their contents.
//    File path: /bin/net8.0/resources/file.txt
//    Resource directory: /bin/net8.0/resources
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