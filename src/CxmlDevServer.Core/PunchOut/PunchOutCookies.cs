using System.Globalization;

namespace CxmlDevServer.Core.PunchOut;

/// <summary>
/// The <c>SupplierPartAuxiliaryID</c> cookie format used for catalog items: <c>cxmldev:{catalogItemId}</c>.
/// It lets an edit or inspect request be mapped back to the catalog.
/// </summary>
public static class PunchOutCookies
{
    private const string Prefix = "cxmldev:";

    /// <summary>The cookie for a catalog item.</summary>
    public static string ForCatalogItem(int catalogItemId) => Prefix + catalogItemId.ToString(CultureInfo.InvariantCulture);

    /// <summary>Extracts the catalog item id from a cookie, if it is one of ours.</summary>
    public static bool TryGetCatalogItemId(string? auxiliaryId, out int catalogItemId)
    {
        catalogItemId = 0;
        var text = auxiliaryId?.Trim();
        return text is not null
               && text.StartsWith(Prefix, StringComparison.Ordinal)
               && int.TryParse(text.AsSpan(Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out catalogItemId);
    }
}
