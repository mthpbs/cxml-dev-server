using CxmlDevServer.Core.Domain;

namespace CxmlDevServer.Core.Options;

/// <summary>Settings of the simulator, bound from the <c>Cxml</c> configuration section.</summary>
public sealed class CxmlServerOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Cxml";

    /// <summary>Externally visible base URL (e.g. behind a proxy); when empty it is derived from the request.</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>How long a PunchOut session lives, in minutes.</summary>
    public int PunchOutSessionMinutes { get; set; } = 60;

    /// <summary>When true, documents from senders that match no trading partner are processed (with a warning).</summary>
    public bool AllowUnknownPartners { get; set; }

    /// <summary>The <c>UserAgent</c> of documents created by this server.</summary>
    public string UserAgent { get; set; } = "cXML Dev Server 1.0";

    /// <summary>Validation mode for unknown partners.</summary>
    public ValidationMode DefaultValidationMode { get; set; } = ValidationMode.Lenient;

    /// <summary>Supplier credential domain used in outbound headers when no supplier identity is stored.</summary>
    public string SupplierDomain { get; set; } = "DUNS";

    /// <summary>Supplier credential identity used in outbound headers when no supplier identity is stored.</summary>
    public string SupplierIdentity { get; set; } = "123456789";

    /// <summary>Currency used for empty carts and templates.</summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>When true the hosted service migrates and seeds the database on startup.</summary>
    public bool InitializeDatabaseOnStartup { get; set; } = true;

    /// <summary>
    /// When true (the default) an empty database is seeded with the demo trading partners (well-known shared
    /// secrets such as <c>devsecret</c>) and a sample response scenario. Set it to false for shared environments:
    /// supplier identities and the catalog are still seeded, but no demo partner or scenario is created.
    /// </summary>
    public bool SeedDemoPartners { get; set; } = true;

    /// <summary>Returns <see cref="PublicBaseUrl"/> or <paramref name="requestBaseUrl"/>, without a trailing slash.</summary>
    public string ResolveBaseUrl(string? requestBaseUrl) =>
        (string.IsNullOrWhiteSpace(PublicBaseUrl) ? requestBaseUrl ?? string.Empty : PublicBaseUrl).TrimEnd('/');
}
