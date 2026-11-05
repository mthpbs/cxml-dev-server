namespace Cxml.Protocol.Documents.Orders;

/// <summary>The <c>OrderRequestHeader@type</c> value.</summary>
public enum OrderRequestType
{
    /// <summary>A new order (the DTD default).</summary>
    New,
    /// <summary>A new version of an existing order.</summary>
    Update,
    /// <summary>Cancel an existing order.</summary>
    Delete,
}

/// <summary>Conversions for <see cref="OrderRequestType"/>.</summary>
public static class OrderRequestTypeExtensions
{
    /// <summary>The lowercase attribute value.</summary>
    public static string ToCxml(this OrderRequestType type) => type switch
    {
        OrderRequestType.New => "new",
        OrderRequestType.Update => "update",
        OrderRequestType.Delete => "delete",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    /// <summary>Parses an attribute value; missing or unrecognised text yields <see cref="OrderRequestType.New"/>.</summary>
    public static OrderRequestType Parse(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "update" => OrderRequestType.Update,
        "delete" => OrderRequestType.Delete,
        _ => OrderRequestType.New,
    };
}
