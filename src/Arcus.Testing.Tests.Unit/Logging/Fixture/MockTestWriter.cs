using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging.Fixture;

public class MockTestWriter : TextWriter
{
    private readonly ICollection<string> _messages = new Collection<string>();

    public override Encoding Encoding { get; } = Encoding.UTF8;

    public override void WriteLine(string value)
    {
        _messages.Add(value);
    }

    public void VerifyWritten(params string[] messages)
    {
        Assert.All(messages, msg => Assert.Contains(_messages, m => m.Contains(msg)));
    }

    public void VerifyNotWritten(params string[] messages)
    {
        Assert.All(messages, msg => Assert.DoesNotContain(_messages, m => m.Contains(msg)));
    }
}