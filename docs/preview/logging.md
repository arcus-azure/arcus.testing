---
title: Logging
layout: default
---

# Logging

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
public class TestClass
{
    private readonly ILogger _testLogger;

    public TestClass(ITestOutputHelper outputWriter)
    {
        _testLogger = new XunitTestLogger(outputWriter);
    }
}
```

## Spy (stub) Test Logging

The `Arcus.Testing.Logging` library provides a `SpyLogger` and `SpyLogger<T>` which are [Microsoft Logging]() `ILogger` and `ILogger<T>` implementations respectively.
These types help in tracking logged messages and their metadata information like the level on which the message was logged or the related exception.

```csharp
ILogger logger = new SpyLogger();

logger.LogInformation("This is a informational message");

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
ILogger<MyType> logger = new SpyLogger<MyType>();

logger.LogInformation("This is a informational message");

IEnumerable<string> messages = logger.Messages;
```