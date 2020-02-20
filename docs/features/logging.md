---
title: Logging
layout: default
---

# Logging

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