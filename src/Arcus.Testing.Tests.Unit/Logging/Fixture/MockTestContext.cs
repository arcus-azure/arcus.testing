using System;
using System.Collections;
using System.Collections.ObjectModel;
using Assert = Xunit.Assert;
using TestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

namespace Arcus.Testing.Tests.Unit.Logging.Fixture
{
    internal class MockTestContext : TestContext
    {
        private readonly Collection<string> _messages = new Collection<string>();

        public override IDictionary Properties { get; }

        public override void AddResultFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void Write(string message)
        {
            _messages.Add(message);
        }

        public override void Write(string format, params object[] args)
        {
            _messages.Add(string.Format(format, args));
        }

        public override void WriteLine(string message)
        {
            _messages.Add(message);
        }

        public override void WriteLine(string format, params object[] args)
        {
            _messages.Add(string.Format(format, args));
        }

        public override void DisplayMessage(MessageLevel messageLevel, string message)
        {
        }

        public void VerifyWritten(string expected)
        {
            Xunit.Assert.Contains(_messages, m => m.Contains(expected));
        }
    }
}
