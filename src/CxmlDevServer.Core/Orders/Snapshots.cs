using System.Text.Json;
using System.Text.Json.Serialization;
using Cxml.Protocol.Common;

namespace CxmlDevServer.Core.Orders;

/// <summary>A postal address as stored in JSON columns.</summary>
public sealed class AddressSnapshot
{
    /// <summary>Address name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The <c>addressID</c>.</summary>
    public string? AddressId { get; set; }

    /// <summary>Email.</summary>
    public string? Email { get; set; }

    /// <summary>Phone, formatted.</summary>
    public string? Phone { get; set; }

    /// <summary>Postal address name attribute.</summary>
    public string? PostalName { get; set; }

    /// <summary><c>DeliverTo</c> lines.</summary>
    public List<string> DeliverTo { get; set; } = [];

    /// <summary><c>Street</c> lines.</summary>
    public List<string> Street { get; set; } = [];

    /// <summary>City.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>State or region.</summary>
    public string? State { get; set; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Country name.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>ISO 3166 country code.</summary>
    public string IsoCountryCode { get; set; } = string.Empty;
}

/// <summary>A contact as stored in JSON columns.</summary>
public sealed class ContactSnapshot
{
    /// <summary>Contact role.</summary>
    public string? Role { get; set; }

    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email addresses.</summary>
    public List<string> Emails { get; set; } = [];

    /// <summary>Phone numbers, formatted.</summary>
    public List<string> Phones { get; set; } = [];
}

/// <summary>A comment as stored in JSON columns.</summary>
public sealed class CommentSnapshot
{
    /// <summary>The comment text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Language.</summary>
    public string? Lang { get; set; }

    /// <summary>Comment type.</summary>
    public string? Type { get; set; }
}

/// <summary>A name/value pair (extrinsic) as stored in JSON columns.</summary>
public sealed class NameValueSnapshot
{
    /// <summary>Creates an empty pair (for deserialization).</summary>
    public NameValueSnapshot() { }

    /// <summary>Creates a pair.</summary>
    public NameValueSnapshot(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Value.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>Payment data as stored in JSON columns; the card number is always masked.</summary>
public sealed class PaymentSnapshot
{
    /// <summary>Card number with all but the last four digits masked.</summary>
    public string? MaskedCardNumber { get; set; }

    /// <summary>Card expiration.</summary>
    public string? Expiration { get; set; }

    /// <summary>Card holder name.</summary>
    public string? CardHolder { get; set; }
}

/// <summary>An amount with a description (shipping, tax) as stored in JSON columns.</summary>
public sealed class AmountSnapshot
{
    /// <summary>Amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Currency.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Description text.</summary>
    public string? Description { get; set; }
}

/// <summary>A cart line as stored in <c>PunchOutSession.OriginalLinesJson</c>.</summary>
public sealed class CartLineSnapshot
{
    /// <summary>Catalog item id.</summary>
    public int? CatalogItemId { get; set; }

    /// <summary>Supplier part number.</summary>
    public string SupplierPartId { get; set; } = string.Empty;

    /// <summary>Auxiliary id (inner XML).</summary>
    public string? SupplierPartAuxiliaryId { get; set; }

    /// <summary>Short name.</summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>Description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Currency.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Unit of measure.</summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>UNSPSC.</summary>
    public string? Unspsc { get; set; }

    /// <summary>Classification domain; <c>null</c> means UNSPSC.</summary>
    public string? ClassificationDomain { get; set; }

    /// <summary>Manufacturer name.</summary>
    public string? ManufacturerName { get; set; }

    /// <summary>Manufacturer part number.</summary>
    public string? ManufacturerPartId { get; set; }

    /// <summary>Lead time in days.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>True for ad-hoc lines.</summary>
    public bool IsAdHoc { get; set; }
}

/// <summary>Converts protocol models to JSON-friendly snapshots and serializes them.</summary>
public static class Snapshots
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Serializes a snapshot to JSON; <c>null</c> stays <c>null</c>.</summary>
    public static string? ToJson<T>(T? value) where T : class => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);

    /// <summary>Serializes a list; an empty list becomes <c>null</c>.</summary>
    public static string? ListToJson<T>(IReadOnlyCollection<T> values) =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);

    /// <summary>Deserializes JSON produced by <see cref="ToJson{T}(T)"/> or <see cref="ListToJson{T}"/>; <c>null</c> on empty or invalid input.</summary>
    public static T? FromJson<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Deserializes a JSON list; empty on missing or invalid input.</summary>
    public static List<T> ListFromJson<T>(string? json) => FromJson<List<T>>(json) ?? [];

    /// <summary>Snapshot of an address.</summary>
    public static AddressSnapshot From(Address address) => new()
    {
        Name = address.Name,
        AddressId = address.AddressId,
        Email = address.Email,
        Phone = address.Phone is { } p ? FormatPhone(p) : null,
        PostalName = address.PostalAddress?.Name,
        DeliverTo = address.PostalAddress?.DeliverTo ?? [],
        Street = address.PostalAddress?.Street ?? [],
        City = address.PostalAddress?.City ?? string.Empty,
        State = address.PostalAddress?.State,
        PostalCode = address.PostalAddress?.PostalCode,
        Country = address.PostalAddress?.Country ?? string.Empty,
        IsoCountryCode = address.PostalAddress?.IsoCountryCode ?? address.IsoCountryCode ?? string.Empty,
    };

    /// <summary>Snapshot of a contact.</summary>
    public static ContactSnapshot From(Contact contact) => new()
    {
        Role = contact.Role,
        Name = contact.Name,
        Emails = contact.Emails,
        Phones = contact.Phones.Select(FormatPhone).ToList(),
    };

    /// <summary>Snapshot of a comment.</summary>
    public static CommentSnapshot From(Comments comments) => new()
    {
        Text = comments.Text,
        Lang = comments.Lang,
        Type = comments.Type,
    };

    /// <summary>Snapshot of payment data with the card number masked.</summary>
    public static PaymentSnapshot From(Payment payment) => new()
    {
        MaskedCardNumber = payment.PCard is { } card ? MaskCardNumber(card.Number) : null,
        Expiration = payment.PCard?.Expiration,
        CardHolder = payment.PCard?.Name,
    };

    /// <summary>Snapshot of shipping costs.</summary>
    public static AmountSnapshot From(Shipping shipping) => new()
    {
        Amount = shipping.Cost.Amount,
        Currency = shipping.Cost.Currency,
        Description = shipping.Description.Text,
    };

    /// <summary>Snapshot of tax.</summary>
    public static AmountSnapshot From(Tax tax) => new()
    {
        Amount = tax.Amount.Amount,
        Currency = tax.Amount.Currency,
        Description = tax.Description.Text,
    };

    /// <summary>Snapshots of extrinsics.</summary>
    public static List<NameValueSnapshot> From(IEnumerable<Extrinsic> extrinsics) =>
        extrinsics.Select(e => new NameValueSnapshot(e.Name, e.Value)).ToList();

    /// <summary>Masks all but the last four characters of a card number.</summary>
    public static string MaskCardNumber(string number)
    {
        var digits = number.Trim();
        return digits.Length <= 4 ? new string('*', digits.Length) : new string('*', digits.Length - 4) + digits[^4..];
    }

    private static string FormatPhone(Phone phone) =>
        string.Join(' ', new[] { "+" + phone.CountryCode, phone.AreaOrCityCode, phone.Number, phone.Extension }
            .Where(s => !string.IsNullOrWhiteSpace(s) && s != "+"));
}
