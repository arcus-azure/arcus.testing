using Microsoft.Extensions.Logging;

namespace Arcus.Testing.Logging
{
    /// <summary>
    /// Spy (stub) <see cref="ILogger{TCategoryName}"/> implementation to track the logged messages in-memory.
    /// </summary>
    /// <typeparam name="T">The type who's name is used for the logger category name.</typeparam>
    public class InMemoryLogger<T> : InMemoryLogger, ILogger<T>
    {
    }
}
