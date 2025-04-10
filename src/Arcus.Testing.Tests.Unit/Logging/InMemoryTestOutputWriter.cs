using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Unit.Logging
{
    extern alias ArcusXunitV3;
    extern alias XunitV3;

    public class InMemoryTestOutputWriter : ITestOutputHelper, XunitV3::Xunit.ITestOutputHelper
    {
        private readonly Collection<string> _contents = new();

        public IEnumerable<string> Contents => _contents;

        public void Write(string message)
        {
            _contents.Add(message);
        }

        public void Write(string format, params object[] args)
        {
            _contents.Add(string.Format(format, args));
        }

        public void WriteLine(string message)
        {
            _contents.Add(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _contents.Add(string.Format(format, args));
        }

        public string Output => string.Join(Environment.NewLine, _contents);
    }
}