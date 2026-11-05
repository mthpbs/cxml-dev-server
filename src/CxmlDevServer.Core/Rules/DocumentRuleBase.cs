using Cxml.Protocol.Documents;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

/// <summary>Base class for rules: provides issue factories and payload-type helpers.</summary>
public abstract class DocumentRuleBase : IDocumentRule
{
    /// <inheritdoc />
    public abstract string Code { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual bool AppliesTo(CxmlRuleContext context) => true;

    /// <inheritdoc />
    public abstract IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context);

    /// <summary>Creates an error issue with this rule's code.</summary>
    protected ValidationIssue Error(string message, int? line = null, int? position = null) =>
        new(ValidationSeverity.Error, Code, message, line, position, ValidationSource.Rule);

    /// <summary>Creates a warning issue with this rule's code.</summary>
    protected ValidationIssue Warning(string message, int? line = null, int? position = null) =>
        new(ValidationSeverity.Warning, Code, message, line, position, ValidationSource.Rule);

    /// <summary>The request or message payload of type <typeparamref name="T"/>, if the document has one.</summary>
    protected static T? PayloadOf<T>(CxmlRuleContext context) where T : class, ICxmlPayload =>
        context.Envelope.Payload as T;
}
