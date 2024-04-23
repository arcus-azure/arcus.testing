---
title: Logging
layout: default
---

# Logging

This page describes functionality related to logging in tests.
* [xUnit test logging](#xunit-test-logging)
* [In-memory test logging](#in-memory-test-logging)
* [Serilog in-memory log sink](#serilog-in-memory-log-sink)

## Installation

The following functionality is available when installing this package:

```shell
PM> Install-Package -Name Arcus.Testing.Logging
```

## xUnit Test Logging

The `Arcus.Testing.Logging` library provides a `XunitTestLogger` type that's an implementation of the abstracted Microsoft [`Ilogger`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging) 
inside the [xUnit](https://xunit.net/) test framework.

Log messages written to the `ILogger` instance will be written to the xUnit test output.

```csharp
using Arcus.Testing.Logging;
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

### xUnit Test Logging in .NET Core

During integration testing of hosts, one could find the need to add the log messages to the xUnit output for defect localization.
The `Arcus.Testing.Logging` library provides an extension to add this in a more dev-friendly way.

```csharp
using Arcus.Testing.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

public class TestClass
{
    private readonly ILogger _outputWriter;

    public TestClass(ITestOutputHelper outputWriter)
    {
        _outputWriter = outputWriter;
    }

    [Fact]
    public void TestMethod()
    {
        IHost host = new HostBuilder()
            .ConfigureLogging(loggingBuilder => loggingBuilder.AddXunitTestLogging(_outputWriter))
            .Build();
    }
}
```

## In-memory Test Logging

The `Arcus.Testing.Logging` library provides a `InMemoryLogger` and `InMemoryLogger<T>` which are [Microsoft Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1) `ILogger` and `ILogger<T>` implementations respectively.
These types help in tracking logged messages and their metadata information like the level on which the message was logged or the related exception.

```csharp
using Arcus.Testing.Logging;
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
using Arcus.Testing.Logging;
using Microsoft.Extensions.Logging;

ILogger<MyType> logger = new InMemoryLogger<MyType>();

logger.LogInformation("This is an informational message");

IEnumerable<string> messages = logger.Messages;
```

## Serilog in-memory log sink

The `Arcus.Testing.Logging` library provides a `InMemoryLogSink` which is a [Serilog log sink](https://github.com/serilog/serilog/wiki/Configuration-Basics#sinks) 
that collectes written log emits in-memory so the test infrastructure can assert on the actual rendered messages and possible properties available on the log emit.

```csharp
using Arcus.Testing.Logging;
using Serilog;
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