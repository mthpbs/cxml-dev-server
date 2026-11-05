namespace CxmlDevServer.Core.Pipeline;

/// <summary>
/// A middleware-style step of the inbound pipeline. A step does its work and calls <c>next</c> to continue, or
/// short-circuits by setting <see cref="CxmlInboundContext.Response"/> (or <see cref="CxmlInboundContext.Abort"/>)
/// and not calling <c>next</c>. Steps run in ascending <see cref="Order"/>; register custom steps with
/// <c>AddPipelineStep&lt;T&gt;()</c>.
/// </summary>
public interface ICxmlPipelineStep
{
    /// <summary>Sort key, see <see cref="CxmlPipelineOrder"/> for the built-in positions.</summary>
    int Order { get; }

    /// <summary>Runs the step.</summary>
    Task InvokeAsync(CxmlInboundContext context, Func<Task> next);
}

/// <summary>Positions of the built-in steps; custom steps can use values in between.</summary>
public static class CxmlPipelineOrder
{
    /// <summary>Wraps everything: serializes the response and persists the log, turns exceptions into 500.</summary>
    public const int Finalize = 0;

    /// <summary>Reads the body, starts timing, creates the log entry.</summary>
    public const int Capture = 100;

    /// <summary>Parses the XML; failure is answered with 406.</summary>
    public const int Parse = 200;

    /// <summary>DTD validation (issues are only recorded here).</summary>
    public const int DtdValidate = 300;

    /// <summary>Partner lookup, shared secret and To checks; failure is answered with 401 or 403.</summary>
    public const int Authenticate = 400;

    /// <summary>Response scenarios.</summary>
    public const int Scenario = 500;

    /// <summary>Business rules and enforcement of the Strict validation mode.</summary>
    public const int RuleValidate = 600;

    /// <summary>Hands the request to its <see cref="Handlers.ICxmlRequestHandler"/>.</summary>
    public const int Dispatch = 700;
}
