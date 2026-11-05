namespace Cxml.Protocol;

/// <summary>Constants describing the cXML version this library targets.</summary>
public static class CxmlVersion
{
    /// <summary>The cXML protocol version the embedded DTDs and the writer use.</summary>
    public const string Current = "1.2.071";

    /// <summary>Host name of the public cXML DTD location (matched case-insensitively).</summary>
    public const string DtdHost = "xml.cxml.org";

    /// <summary>Public identifier URL of the main cXML DTD for <paramref name="version"/>.</summary>
    public static string DtdUrl(string version = Current, string fileName = "cXML.dtd") =>
        $"http://{DtdHost}/schemas/cXML/{version}/{fileName}";

    /// <summary>The <c>deploymentMode</c> value for test traffic.</summary>
    public const string DeploymentTest = "test";

    /// <summary>The <c>deploymentMode</c> value for production traffic (the cXML default).</summary>
    public const string DeploymentProduction = "production";

    /// <summary>The MIME content type used for cXML responses.</summary>
    public const string ContentType = "text/xml; charset=\"UTF-8\"";
}
