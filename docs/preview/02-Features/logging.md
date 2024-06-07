# Logging
This page describes functionality related to logging in tests.

## xUnit test logging
The `Arcus.Testing.Logging.Xunit` library provides a `XunitTestLogger` type that's an implementation of the abstracted Microsoft [`Ilogger`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging)
inside the [xUnit](https://xunit.net/) test framework.

### Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Logging.Xunit
```

### Example
Log messages written to the `ILogger` instance will be written to the xUnit test output.

```csharp
using Arcus.Testing;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

public class TestClass
{
    private readonly ILogger _testLogger;

    public TestClass(ITestOutputHelper outputWriter)
    {
        _testLogger = new XunitTestLogger(outputWriter);
    }
}
```

In the same fashion there is a:
* [`XunitTestLogging`] extension to add a Serilog log sink for an `ITestOutputHelper` that delegates written log emits to the xUnit test output,
* [`AddXunitTestLogging`] extension to add a `ILoggerProvider` to a [Microsoft Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1) setup.

## NUnit test logging
The `Arcus.Testing.Logging.NUnit` library provides a `NUnitTestLogger` type that's an implementation of the abstracted Microsoft [`Ilogger`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging)
inside the [NUnit](https://nunit.org/) test framework.

### Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Logging.NUnit
```

### Example
Log messages written to the `ILogger` instance will be written to the `TestContext.Out/Error` in the NUnit test output.

```csharp
using Arcus.Testing;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

public class TestClass
{
    private readonly ILogger _testLogger;

    public TestClass()
    {
        _testLogger = new NUnitTestLogger(TestContext.Out, TestContext.Error);
    }
}
```

In the same fashion there is a:
* [`NUnitTestLogging`] extension to add a Serilog log sink for an `TestContext` that delegates written log emits to the NUnit test output,
* [`AddNUnitTestLogging`] extension to add a `ILoggerProvider` to a [Microsoft Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1) setup.

## MSTest test logging
The `Arcus.Testing.Logging.MSTest` library provides a `MSTestLogger` type that's an implementation of the abstracted Microsoft [`Ilogger`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging)
inside the [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest) test framework.

### Installation
The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Logging.MSTest
```

### Example
Log messages written to the `ILogger` instance will be written to the `TestContext` in the MSTest test output.

```csharp
using Arcus.Testing;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

public class TestClass
{
    private ILogger TestLogger => new MSTestLogger(TestContext);
    public TestContext TestContext { get; set; }
}
```

In the same fashion there is a:
* [`MSTestLogging`] extension to add a Serilog log sink for an `TestContext` that delegates written log emits to the NUnit test output,
* [`AddMSTestLogging`] extension to add a `ILoggerProvider` to a [Microsoft Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1) setup.

## In-memory test logging
The `Arcus.Testing.Logging.Core` library provides a `InMemoryLogger` and `InMemoryLogger<T>` which are  `ILogger` and `ILogger<T>` implementations respectively.
These types help in tracking logged messages and their metadata information like the level on which the message was logged or the related exception.

```csharp
using Arcus.Testing;
using Microsoft.Extensions.Logging;

ILogger logger = new InMemoryLogger();

logger.LogInformation("This is an informational message");

// Either get the message directly, or
IEnumerable<string> messages = logger.Messages;

// Use the full `LogEntry` object to retrieve the message.
IEnumerable<LogEntry> entries = logger.Entries;
LogEntry entry = entries.First();

// Level = Information
LogLevel level = entry.Level;
// Message = "This is a informational message"
string message = entry.Message;
```

Or, alternatively you can use the generic variant:

```csharp
using Arcus.Testing;
using Microsoft.Extensions.Logging;

ILogger<MyType> logger = new InMemoryLogger<MyType>();

logger.LogInformation("This is an informational message");

IEnumerable<string> messages = logger.Messages;
```

## Serilog in-memory log sink
The `Arcus.Testing.Logging.Core` library provides a `InMemoryLogSink` which is a [Serilog log sink](https://github.com/serilog/serilog/wiki/Configuration-Basics#sinks)
that collectes written log emits in-memory so the test infrastructure can assert on the actual rendered messages and possible properties available on the log emit.

```csharp
using Arcus.Testing;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

var logSink = new InMemoryLogSink();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(logSink)
    .CreateLogger();

logger.Information("This is an informational message");

IEnumerable<LogEvent> emits = logSink.CurrentLogEmits;
IEnumeratble<string> messages = logSink.CurrentLogMessages;
```
