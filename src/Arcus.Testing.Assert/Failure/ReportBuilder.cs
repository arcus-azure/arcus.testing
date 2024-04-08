using System.Text;

namespace Arcus.Testing.Failure;

internal class ReportBuilder
{
    private readonly StringBuilder _report;

    private ReportBuilder(string methodName, string generalMessage)
    {
        _report = new StringBuilder();
        _report.AppendLine($"{methodName} failure: {generalMessage}");
    }

    internal static ReportBuilder ForMethod(string methodName, string generalMessage)
    {
        return new ReportBuilder(methodName, generalMessage);
    }

    internal ReportBuilder AppendLine(string message)
    {
        _report.AppendLine(message);
        return this;
    }

    internal ReportBuilder AppendInput(string input)
    {
        _report.AppendLine();
        _report.Append("Input:");
        _report.Append(input);

        return this;
    }

    internal ReportBuilder AppendDiff(string expected, string actual, int maxCharacters = 500)
    {
        string Trim(string txt) => txt.Length > maxCharacters ? txt[..maxCharacters] + "..." : txt;

        _report.AppendLine();
        _report.AppendLine("Expected:");
        _report.AppendLine(Trim(expected));
        _report.AppendLine();
        _report.AppendLine("Actual:");
        _report.AppendLine(Trim(actual));
        _report.AppendLine();

        return this;
    }

    public override string ToString()
    {
        return _report.ToString();
    }
}