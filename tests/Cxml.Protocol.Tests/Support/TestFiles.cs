namespace Cxml.Protocol.Tests.Support;

/// <summary>Locates the spec example documents copied next to the test assembly.</summary>
internal static class TestFiles
{
    public static string ExamplesDirectory => Path.Combine(AppContext.BaseDirectory, "Examples");

    public static IEnumerable<string> AllExamples() =>
        Directory.GetFiles(ExamplesDirectory, "*.xml").OrderBy(p => p, StringComparer.Ordinal);

    public static string ReadExample(string fileName) => File.ReadAllText(Path.Combine(ExamplesDirectory, fileName));

    public static IEnumerable<object[]> ExampleNames() =>
        AllExamples().Select(p => new object[] { Path.GetFileName(p) });
}
