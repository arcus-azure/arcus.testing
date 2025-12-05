using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    internal sealed class LogMessageBuilder
    {
        private readonly StringBuilder _builder = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessageBuilder"/> class.
        /// </summary>
        internal LogMessageBuilder(LogLevel level)
        {
            _builder.Append(DateTimeOffset.UtcNow.ToString("s", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(level).Append(' ');
        }

        internal LogMessageBuilder AddCategory(string categoryName)
        {
            if (categoryName != null)
            {
                _builder.Append('{').Append(categoryName).Append("} ");
            }

            return this;
        }

        internal LogMessageBuilder AddUserMessage(string message)
        {
            _builder.Append("> ").Append(message);
            return this;
        }

        internal void AddException(Exception exception)
        {
            if (exception is not null)
            {
                _builder.Append(": ").Append(exception);
            }
        }

        internal void AddScope(object scope)
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> properties)
            {
                foreach (KeyValuePair<string, object> pair in properties)
                {
                    _builder.AppendLine().Append("\t=> ").Append(pair.Key).Append(": ").Append(pair.Value);
                }
            }
            else if (scope != null)
            {
                _builder.AppendLine().Append("\t=> ").Append(scope);
            }
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
