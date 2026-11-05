using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace Cxml.Protocol.Validation;

/// <summary>
/// Validates cXML documents against the cXML DTD using the DTDs embedded in this assembly.
/// The network is never used.
/// </summary>
/// <remarks>
/// The DTD to use is taken from the document's DOCTYPE (<c>cXML.dtd</c>, <c>Fulfill.dtd</c>, ...). A
/// document without a DOCTYPE is validated against <c>cXML.dtd</c> and a warning is reported. When a
/// DOCTYPE is injected it is placed on the first line without adding line breaks, so reported line numbers
/// match the original text.
/// </remarks>
public sealed partial class CxmlDtdValidator
{
    /// <summary>Code of the issue reported for errors found by the DTD.</summary>
    public const string DtdErrorCode = "CXML-DTD";

    /// <summary>Code of the warning reported when the document has no DOCTYPE.</summary>
    public const string NoDoctypeCode = "CXML-DTD-NO-DOCTYPE";

    /// <summary>Code of the warning reported when the DTD version is unknown and 1.2.071 was used.</summary>
    public const string VersionFallbackCode = "CXML-DTD-VERSION-FALLBACK";

    /// <summary>Code of the error reported when the document is not well-formed XML.</summary>
    public const string MalformedCode = "CXML-XML-MALFORMED";

    // The embedded DTDs expand to a few hundred thousand characters; internal subsets are rejected up front.
    /// <summary>Code of the error reported when the document declares an internal DTD subset.</summary>
    public const string InternalSubsetCode = "CXML-DTD-INTERNAL-SUBSET";

    private const int MaxCharactersFromEntities = 5_000_000;
    private const long MaxCharactersInDocument = 50_000_000;

    /// <summary>Validates a document held in memory.</summary>
    public ValidationResult Validate(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var issues = new List<ValidationIssue>();

        if (!TryInspectProlog(xml, issues, out var hasDoctype))
        {
            return new ValidationResult(issues);
        }

        var text = xml;
        var injectedLength = 0;
        if (!hasDoctype)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning, NoDoctypeCode,
                "Document has no DOCTYPE; validated against the default cXML.dtd.",
                Source: ValidationSource.Dtd));
            (text, injectedLength) = InjectDoctype(xml);
        }

        var resolver = new EmbeddedDtdResolver();
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            ValidationType = ValidationType.DTD,
            XmlResolver = resolver,
            MaxCharactersFromEntities = MaxCharactersFromEntities,
            MaxCharactersInDocument = MaxCharactersInDocument,
        };

        void OnValidation(object? sender, ValidationEventArgs e) =>
            issues.Add(ToIssue(e, injectedLength, hasDoctype));
        settings.ValidationEventHandler += OnValidation;

        try
        {
            using var reader = XmlReader.Create(new StringReader(text), settings);
            while (reader.Read())
            {
                // Reading drives validation; events are collected by the handler.
            }
        }
        catch (XmlException ex)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error, MalformedCode, ex.Message,
                ex.LineNumber > 0 ? ex.LineNumber : null,
                ex.LineNumber > 0 ? AdjustPosition(ex.LineNumber, ex.LinePosition, injectedLength) : null,
                ValidationSource.Parse));
        }

        foreach (var warning in resolver.Warnings)
        {
            issues.Add(new ValidationIssue(ValidationSeverity.Warning, VersionFallbackCode, warning, Source: ValidationSource.Dtd));
        }

        return new ValidationResult(issues);
    }

    /// <summary>Validates a document from UTF-8 (or BOM/declaration-detected) bytes.</summary>
    public ValidationResult Validate(byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        using var reader = new StreamReader(new MemoryStream(xml), detectEncodingFromByteOrderMarks: true);
        return Validate(reader.ReadToEnd());
    }

    /// <summary>
    /// Checks well-formedness (reading the whole document) and whether a DOCTYPE is present, without validating.
    /// Returns <c>false</c> (after recording a Parse error) when the text is not well-formed.
    /// </summary>
    private static bool TryInspectProlog(string xml, List<ValidationIssue> issues, out bool hasDoctype)
    {
        hasDoctype = false;
        // DtdProcessing.Parse with no resolver reports the DOCTYPE node without loading anything external.
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = null,
            MaxCharactersFromEntities = 1_000,
        };
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.DocumentType)
                {
                    hasDoctype = true;
                    if (!string.IsNullOrWhiteSpace(reader.Value))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error, InternalSubsetCode,
                            "An internal DTD subset is not permitted in cXML documents.",
                            (reader as IXmlLineInfo)?.LineNumber, (reader as IXmlLineInfo)?.LinePosition,
                            ValidationSource.Parse));
                        return false;
                    }
                }
            }

            return true;
        }
        catch (XmlException ex)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error, MalformedCode, ex.Message,
                ex.LineNumber > 0 ? ex.LineNumber : null,
                ex.LineNumber > 0 ? ex.LinePosition : null,
                ValidationSource.Parse));
            return false;
        }
    }

    private static (string Text, int InjectedLength) InjectDoctype(string xml)
    {
        var doctype = $"<!DOCTYPE cXML SYSTEM \"{CxmlVersion.DtdUrl()}\">";
        var declaration = XmlDeclarationRegex().Match(xml);
        var insertAt = declaration.Success ? declaration.Length : 0;
        return (xml.Insert(insertAt, doctype), doctype.Length);
    }

    private static ValidationIssue ToIssue(ValidationEventArgs e, int injectedLength, bool hasDoctype)
    {
        var severity = e.Severity == XmlSeverityType.Error ? ValidationSeverity.Error : ValidationSeverity.Warning;
        var line = e.Exception?.LineNumber is > 0 ? e.Exception.LineNumber : (int?)null;
        var position = line is { } l ? AdjustPosition(l, e.Exception!.LinePosition, hasDoctype ? 0 : injectedLength) : (int?)null;
        return new ValidationIssue(severity, DtdErrorCode, e.Message, line, position, ValidationSource.Dtd);
    }

    /// <summary>Removes the injected DOCTYPE length from column numbers on the first line.</summary>
    private static int AdjustPosition(int line, int position, int injectedLength) =>
        line == 1 && injectedLength > 0 ? Math.Max(1, position - injectedLength) : position;

    [GeneratedRegex(@"^\s*<\?xml[^>]*\?>", RegexOptions.CultureInvariant)]
    private static partial Regex XmlDeclarationRegex();
}
