using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Cxml.Protocol.Validation;

/// <summary>
/// An <see cref="XmlResolver"/> that serves cXML DTDs from assembly resources and refuses everything else,
/// so validation never touches the network or file system.
/// </summary>
/// <remarks>
/// <c>http(s)://xml.cxml.org/schemas/cXML/{version}/{file}.dtd</c> is matched case-insensitively. A version
/// that is not embedded falls back to <see cref="CxmlVersion.Current"/> and is recorded in
/// <see cref="Warnings"/>.
/// </remarks>
public sealed partial class EmbeddedDtdResolver : XmlResolver
{
    private const string ResourcePrefix = "Cxml.Protocol.Dtds.";

    private static readonly Assembly ResourceAssembly = typeof(EmbeddedDtdResolver).Assembly;

    private readonly List<string> _warnings = [];

    /// <summary>Warnings produced while resolving (e.g. version fallbacks).</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>The file names of all embedded DTDs for the current version.</summary>
    public static IReadOnlyList<string> EmbeddedFileNames { get; } = ResourceAssembly
        .GetManifestResourceNames()
        .Where(n => n.StartsWith(ResourcePrefix + CxmlVersion.Current + ".", StringComparison.Ordinal))
        .Select(n => n[(ResourcePrefix.Length + CxmlVersion.Current.Length + 1)..])
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <inheritdoc />
    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        var match = DtdUrlRegex().Match(absoluteUri.AbsoluteUri);
        if (!match.Success)
        {
            throw new XmlException($"External resource '{absoluteUri}' is not permitted; only cXML DTDs on {CxmlVersion.DtdHost} are resolved (from embedded copies).");
        }

        var version = match.Groups["ver"].Value;
        var file = match.Groups["file"].Value;
        var stream = Open(version, file);
        if (stream is null && !string.Equals(version, CxmlVersion.Current, StringComparison.OrdinalIgnoreCase))
        {
            _warnings.Add($"cXML DTD version '{version}' is not available offline; validated against {CxmlVersion.Current} instead.");
            stream = Open(CxmlVersion.Current, file);
        }

        return stream is null
            ? throw new XmlException($"Unknown cXML DTD '{file}'. Available: {string.Join(", ", EmbeddedFileNames)}.")
            : InlineNonEmptyAnyEntities(stream);
    }

    /// <summary>
    /// Works around a .NET DTD parser limitation. The cXML DTDs declare extension hooks such as
    /// <c>&lt;!ENTITY % Object.ANY '|xades:QualifyingProperties|...'&gt;</c> and use them inside mixed-content
    /// groups (<c>(#PCDATA|ds:Manifest %Object.ANY;)*</c>). .NET rejects the non-empty replacement with
    /// "must nest properly within markup declarations" and then fails every document. Inlining the
    /// replacement text yields the identical content model.
    /// </summary>
    private static MemoryStream InlineNonEmptyAnyEntities(Stream source)
    {
        using var reader = new StreamReader(source, Encoding.UTF8);

        // Comments are blanked (keeping line breaks) so commented-out declarations are not picked up.
        var text = CommentRegex().Replace(reader.ReadToEnd(), m => new string('\n', m.Value.Count(c => c == '\n')));
        foreach (Match declaration in NonEmptyAnyEntityRegex().Matches(text).ToList())
        {
            var name = declaration.Groups["name"].Value;
            var value = declaration.Groups["value"].Value;
            text = text.Replace($"%{name};", value, StringComparison.Ordinal);
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    private static Stream? Open(string version, string file)
    {
        var wanted = $"{ResourcePrefix}{CxmlVersion.Current}.{file}";
        var name = version.Equals(CxmlVersion.Current, StringComparison.OrdinalIgnoreCase)
            ? ResourceAssembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.Equals(wanted, StringComparison.OrdinalIgnoreCase))
            : null;
        return name is null ? null : ResourceAssembly.GetManifestResourceStream(name);
    }

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"<!ENTITY\s+%\s+(?<name>[\w.]+\.ANY)\s+'(?<value>[^']+)'\s*>", RegexOptions.CultureInvariant)]
    private static partial Regex NonEmptyAnyEntityRegex();

    [GeneratedRegex(@"^https?://xml\.cxml\.org/schemas/cXML/(?<ver>[^/]+)/(?<file>[^/]+\.dtd)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DtdUrlRegex();
}
