using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;
using Cxml.Protocol;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Common;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.PunchOut;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CxmlOptions = CxmlDevServer.Core.Options.CxmlServerOptions;

namespace CxmlDevServer.Core.Templates;

/// <summary>Default <see cref="IMessageTemplateService"/>.</summary>
public sealed partial class MessageTemplateService : IMessageTemplateService
{
    private static readonly IReadOnlyList<MessageTemplate> BuiltIn =
    [
        new(TemplateKeys.PunchOutSetupCreate, "PunchOutSetupRequest (create)", "Starts a new PunchOut shopping session.", true),
        new(TemplateKeys.PunchOutSetupEdit, "PunchOutSetupRequest (edit)", "Reopens a cart with two catalog items for editing.", true),
        new(TemplateKeys.PunchOutSetupInspect, "PunchOutSetupRequest (inspect)", "Opens a read-only view of two catalog items.", true),
        new(TemplateKeys.OrderRequest, "OrderRequest", "A new order with three lines, modelled on the specification example."),
        new(TemplateKeys.ProfileRequest, "ProfileRequest", "Asks which transactions the server supports."),
    ];

    private readonly ICxmlDbContext _db;
    private readonly SupplierIdentityService _supplierIdentities;
    private readonly CxmlOptions _options;
    private readonly TimeProvider _time;

    /// <summary>Creates the service.</summary>
    public MessageTemplateService(
        ICxmlDbContext db, SupplierIdentityService supplierIdentities, IOptions<CxmlOptions> options, TimeProvider time)
    {
        _db = db;
        _supplierIdentities = supplierIdentities;
        _options = options.Value;
        _time = time;
    }

    /// <inheritdoc />
    public IReadOnlyList<MessageTemplate> Templates => BuiltIn;

    /// <inheritdoc />
    public async Task<string> RenderAsync(
        string templateKey, TradingPartner? partner, string targetBaseUrl, CancellationToken cancellationToken = default)
    {
        var template = templateKey switch
        {
            TemplateKeys.PunchOutSetupCreate => MessageTemplates.PunchOutSetupCreate,
            TemplateKeys.PunchOutSetupEdit => MessageTemplates.PunchOutSetupEdit,
            TemplateKeys.PunchOutSetupInspect => MessageTemplates.PunchOutSetupInspect,
            TemplateKeys.OrderRequest => MessageTemplates.OrderRequest,
            TemplateKeys.ProfileRequest => MessageTemplates.ProfileRequest,
            _ => throw new ArgumentException($"Unknown template '{templateKey}'.", nameof(templateKey)),
        };

        var supplier = await _supplierIdentities.GetDefaultAsync(cancellationToken);
        var now = _time.GetLocalNow();
        var unique = Guid.NewGuid().ToString("N")[..10];
        var useOverride = !string.IsNullOrWhiteSpace(partner?.ToDomain) && !string.IsNullOrWhiteSpace(partner.ToIdentity);

        var values = new Dictionary<string, string>
        {
            ["payloadId"] = PayloadIdGenerator.Generate(),
            ["timestamp"] = CxmlTimestamp.Format(now),
            ["fromDomain"] = partner?.FromDomain ?? "NetworkId",
            ["fromIdentity"] = partner?.FromIdentity ?? "AN01000000001",
            ["senderDomain"] = partner?.SenderDomain ?? "NetworkId",
            ["senderIdentity"] = partner?.SenderIdentity ?? "AN01000000001",
            ["sharedSecret"] = partner is null ? "devsecret" : partner.SharedSecret ?? string.Empty,
            ["toDomain"] = useOverride ? partner!.ToDomain! : supplier.Domain,
            ["toIdentity"] = useOverride ? partner!.ToIdentity! : supplier.Identity,
            ["userAgent"] = "cXML Dev Server Message Sender",
            ["buyerCookie"] = "bc-" + unique,
            ["orderId"] = $"PO-{now:yyyyMMdd}-{unique}",
            ["deliveryDate"] = now.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["browserFormPostUrl"] = _options.ResolveBaseUrl(targetBaseUrl) + "/test-buyer/punchout-return",
        };
        await AddCatalogItemsAsync(values, cancellationToken);

        return PlaceholderRegex().Replace(template, m =>
            values.TryGetValue(m.Groups[1].Value, out var value) ? SecurityElement.Escape(value) ?? string.Empty : m.Value);
    }

    private async Task AddCatalogItemsAsync(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        var items = await _db.CatalogItems.AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        for (var n = 1; n <= 2; n++)
        {
            var item = items.Count >= n ? items[n - 1] : null;
            values[$"item{n}PartId"] = item?.SupplierPartId ?? $"SAMPLE-{n}";
            values[$"item{n}Aux"] = item is null ? string.Empty : PunchOutCookies.ForCatalogItem(item.Id);
            values[$"item{n}Name"] = item?.ShortName ?? $"Sample item {n}";
            values[$"item{n}Price"] = (item?.UnitPrice ?? 10m * n).ToString("0.00", CultureInfo.InvariantCulture);
            values[$"item{n}Currency"] = item?.Currency ?? _options.DefaultCurrency;
            values[$"item{n}Uom"] = item?.UnitOfMeasure ?? "EA";
            values[$"item{n}Unspsc"] = item?.Unspsc ?? "43211503";
        }
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();
}
