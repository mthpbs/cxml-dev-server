using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Pipeline.Steps;

/// <summary>
/// Validates the document against the cXML DTD and records the issues. Whether errors are enforced depends on the
/// partner's validation mode, which is only known after authentication; see <see cref="RuleValidateStep"/>.
/// </summary>
public sealed class DtdValidateStep : ICxmlPipelineStep
{
    private readonly CxmlDtdValidator _validator;

    /// <summary>Creates the step.</summary>
    public DtdValidateStep(CxmlDtdValidator validator) => _validator = validator;

    /// <inheritdoc />
    public int Order => CxmlPipelineOrder.DtdValidate;

    /// <inheritdoc />
    public Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        context.AddIssues(_validator.Validate(context.RawText).Issues);
        return next();
    }
}
