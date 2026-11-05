using Cxml.Protocol.Envelope;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Core.Rules;

/// <summary>What a business rule can look at.</summary>
/// <param name="Envelope">The parsed document.</param>
/// <param name="RawXml">The document text.</param>
/// <param name="Partner">The authenticated partner, if any.</param>
public sealed record CxmlRuleContext(CxmlEnvelope Envelope, string RawXml, TradingPartner? Partner);

/// <summary>
/// A business rule that checks what the DTD cannot. Rules are small, independent and registered with
/// <c>AddDocumentRule&lt;T&gt;()</c>. Rules must not throw for malformed input; they report issues instead.
/// </summary>
public interface IDocumentRule
{
    /// <summary>Unique code (e.g. <c>RULE-TIMESTAMP-FORMAT</c>), also the code of the issues it reports.</summary>
    string Code { get; }

    /// <summary>One-line description of what the rule checks.</summary>
    string Description { get; }

    /// <summary>True when the rule is relevant for the document.</summary>
    bool AppliesTo(CxmlRuleContext context);

    /// <summary>Evaluates the document.</summary>
    IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context);
}
