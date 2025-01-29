using System;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    /// <summary>
    /// Spy (stub) <see cref="ILogger{TCategoryName}"/> implementation to track the logged messages in-memory.
    /// </summary>
    /// <typeparam name="T">The type who's name is used for the logger category name.</typeparam>
#pragma warning disable S1133
    [Obsolete("Will be removed in v2.0, use the specific logging packages (Xunit, NUnit, MSTest) instead")]
#pragma warning restore
    public class InMemoryLogger<T> : InMemoryLogger, ILogger<T>
    {
    }
}
