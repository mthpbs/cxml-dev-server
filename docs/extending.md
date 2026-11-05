# Extending the server

The server is built so that a new cXML transaction touches as few places as possible. This guide walks
through the real extension seams with two examples:

1. an **inbound** transaction, `StatusUpdateRequest` (the buyer tells the supplier about the status of an
   earlier document), and
2. an **outbound** transaction, `ConfirmationRequest` (the supplier confirms an order), sent through
   `ICxmlOutboundClient`.

It then covers adding a UI page, an EF migration and, as a sketch, a buyer-simulator role. Read
[architecture.md](architecture.md) first for the pipeline.

All snippets use the real type names. The two walkthroughs were compiled and run against the test host
before they were written down.

> The `src/` projects compile with warnings as errors and require XML documentation (`///`) on public
> members. The snippets below leave the comments out for brevity; add them when you put the code in `src/`.

## The extension seams

| Seam | Where | Use it to |
|---|---|---|
| `ICxmlPayload` + `ICxmlPayloadParser` / `ICxmlPayloadWriter` | `Cxml.Protocol.Documents` | Teach the reader and writer a new payload element. `CxmlPayloadCodec<T>` is a ready-made delegate-based codec. |
| `ICxmlRequestHandler` | `CxmlDevServer.Core.Handlers` | Answer an inbound `Request` of one kind. |
| `AddCxmlTransaction<TPayload, TCodec, THandler>()` | `CxmlDevServer.Core.DependencyInjection` | Register payload, codec and handler in one line. Overloads: `<TPayload, TParser, TWriter, THandler>` and `<THandler>` (payload already known). |
| `IDocumentRule` / `DocumentRuleBase` + `AddDocumentRule<T>()` | `CxmlDevServer.Core.Rules` | Add a business rule. It shows up on the **Validation rules** page. |
| `ICxmlPipelineStep` + `AddPipelineStep<T>()` | `CxmlDevServer.Core.Pipeline` | Add behavior to every request, at a position given by `Order`. |
| `ICxmlOutboundClient` | `CxmlDevServer.Core.Messaging` | POST a document to a remote server and log the exchange. |

## Part 1: an inbound transaction, `StatusUpdateRequest`

The DTD (`cXML.dtd`) defines
`StatusUpdateRequest (DocumentReference?, Status, (%cxml.statuses;)?, Extrinsic*)`. Without any work the
server answers one with `450 Not Implemented`: the reader keeps unknown payloads as `UnknownPayload` and
the dispatcher finds no handler. We will make it a first-class transaction.

### 1. The payload model

A payload is a class implementing `ICxmlPayload`. The built-ins live in `Cxml.Protocol/Documents/<Area>/`;
put this in `src/Cxml.Protocol/Documents/StatusUpdates/StatusUpdateRequest.cs`.

```csharp
using Cxml.Protocol.Documents;

namespace Cxml.Protocol.Documents.StatusUpdates;

public sealed class StatusUpdateRequest : ICxmlPayload
{
    public const string Name = "StatusUpdateRequest";

    public string ElementName => Name;

    public string? DocumentPayloadId { get; set; }

    public int Code { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? Detail { get; set; }
}
```

### 2. Parser and writer

`ICxmlPayloadParser` turns the payload element into the model, `ICxmlPayloadWriter` does the reverse. One
class can do both. **Child elements must be written in DTD order**, otherwise the document is not valid.

```csharp
using System.Xml.Linq;
using Cxml.Protocol.Documents;

namespace Cxml.Protocol.Documents.StatusUpdates;

public sealed class StatusUpdateCodec : ICxmlPayloadParser, ICxmlPayloadWriter
{
    public string ElementName => StatusUpdateRequest.Name;

    public ICxmlPayload Parse(XElement element)
    {
        var status = element.Element("Status");
        return new StatusUpdateRequest
        {
            DocumentPayloadId = element.Element("DocumentReference")?.Attribute("payloadID")?.Value,
            Code = int.TryParse(status?.Attribute("code")?.Value, out var code) ? code : 0,
            Text = status?.Attribute("text")?.Value ?? string.Empty,
            Detail = string.IsNullOrWhiteSpace(status?.Value) ? null : status.Value.Trim(),
        };
    }

    public XElement Write(ICxmlPayload payload)
    {
        var update = (StatusUpdateRequest)payload;

        // Child order follows the DTD: (DocumentReference?, Status, ...).
        var element = new XElement(StatusUpdateRequest.Name);
        if (update.DocumentPayloadId is not null)
        {
            element.Add(new XElement("DocumentReference", new XAttribute("payloadID", update.DocumentPayloadId)));
        }

        element.Add(new XElement(
            "Status", new XAttribute("code", update.Code), new XAttribute("text", update.Text), update.Detail));
        return element;
    }
}
```

Prefer less ceremony? `CxmlPayloadCodec<T>` wraps two delegates (this is how the built-in payloads are
declared). Its constructor takes arguments, so register an instance instead of a type:

```csharp
var codec = new CxmlPayloadCodec<StatusUpdateRequest>(
    StatusUpdateRequest.Name,
    element => new StatusUpdateRequest { /* read the element */ },
    update => new XElement(StatusUpdateRequest.Name /* , children in DTD order */));
services.AddSingleton<ICxmlPayloadParser>(codec);
services.AddSingleton<ICxmlPayloadWriter>(codec);
services.AddCxmlTransaction<StatusUpdateHandler>();
```

### 3. The handler

A handler answers one request element name. It runs after parsing, DTD validation, authentication,
scenarios and rules, so it can concentrate on the business logic. Put it in
`src/CxmlDevServer.Core/Handlers/StatusUpdateHandler.cs`.

```csharp
using Cxml.Protocol;
using Cxml.Protocol.Documents.StatusUpdates;
using Cxml.Protocol.Validation;
using CxmlDevServer.Core.Abstractions;
using CxmlDevServer.Core.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace CxmlDevServer.Core.Handlers;

public sealed class StatusUpdateHandler : ICxmlRequestHandler
{
    public const string UnknownDocumentCode = "STATUS-UNKNOWN-DOCUMENT";

    private readonly ICxmlDbContext _db;

    public StatusUpdateHandler(ICxmlDbContext db) => _db = db;

    public string RequestName => StatusUpdateRequest.Name;

    public async Task<CxmlHandlerResult> HandleAsync(CxmlInboundContext context)
    {
        var request = (StatusUpdateRequest)context.Envelope!.Request!.Payload!;
        if (string.IsNullOrWhiteSpace(request.DocumentPayloadId))
        {
            return CxmlHandlerResult.Fail(CxmlStatusCodes.BadRequest, "DocumentReference@payloadID is required.");
        }

        // Link this update to the document it talks about, so the message log shows both together.
        var referenced = await _db.MessageLogs.AsNoTracking()
            .Where(l => l.PayloadId == request.DocumentPayloadId)
            .Select(l => l.CorrelationId)
            .FirstOrDefaultAsync(context.CancellationToken);

        var issues = new List<ValidationIssue>();
        if (referenced is null)
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Warning,
                UnknownDocumentCode,
                $"No logged document has payloadID '{request.DocumentPayloadId}'.",
                Source: ValidationSource.Rule));
        }

        return CxmlHandlerResult.Ok(correlationId: referenced, issues: issues);
    }
}
```

Handler conventions:

- Handlers are **scoped**: constructor-inject `ICxmlDbContext`, `TimeProvider` and other scoped services. The
  request context offers `Envelope`, `Partner` (the authenticated trading partner, or null for allowed unknown
  senders), `EffectiveValidationMode`, `BaseUrl` (for links back to this server), `LogEntry` and
  `CancellationToken`.
- Return `CxmlHandlerResult.Ok(payload, detail, correlationId, issues)` or `CxmlHandlerResult.Fail(code, detail, ...)`.
  A response payload (such as a `ProfileResponse`) is attached only for 2xx.
- `issues` are stored on the message log and shown in the inspector without changing the status. Use them for
  warnings; in `Strict` mode (`context.EffectiveValidationMode`) turn the same condition into a `Fail`, as
  `OrderRequestHandler` does.
- Throwing is allowed: `FinalizeStep` answers `500`, stores the exception in the log and discards pending
  database changes.

### 4. A business rule

Rules check what the DTD cannot. Derive from `DocumentRuleBase`, which provides `Error(...)`, `Warning(...)` and
`PayloadOf<T>(context)`.

```csharp
using Cxml.Protocol;
using Cxml.Protocol.Documents.StatusUpdates;
using Cxml.Protocol.Validation;

namespace CxmlDevServer.Core.Rules;

public sealed class StatusUpdateCodeRule : DocumentRuleBase
{
    public override string Code => "RULE-STATUSUPDATE-CODE";

    public override string Description => "StatusUpdateRequest/Status@code should be a known cXML status code.";

    public override bool AppliesTo(CxmlRuleContext context) => PayloadOf<StatusUpdateRequest>(context) is not null;

    public override IEnumerable<ValidationIssue> Evaluate(CxmlRuleContext context)
    {
        var update = PayloadOf<StatusUpdateRequest>(context)!;
        if (!CxmlStatusCodes.KnownCodes.Contains(update.Code))
        {
            yield return Warning($"Status code {update.Code} is not one of the cXML status codes.");
        }
    }
}
```

A warning is recorded in every mode but never rejects the request; an `Error(...)` rejects it with `406` for
partners in Strict mode. Keep rule codes unique and prefixed `RULE-`.

### 5. A pipeline step (optional)

A step sees every request. This one refuses production traffic, a plausible safeguard for a shared
development server:

```csharp
using Cxml.Protocol;
using CxmlDevServer.Core.Pipeline;

namespace CxmlDevServer.Core.Pipeline.Steps;

public sealed class BlockProductionTrafficStep : ICxmlPipelineStep
{
    // Runs right after authentication, before scenarios and rules.
    public int Order => CxmlPipelineOrder.Authenticate + 1;

    public Task InvokeAsync(CxmlInboundContext context, Func<Task> next)
    {
        var mode = context.Envelope?.Request?.DeploymentMode;
        if (string.Equals(mode, CxmlVersion.DeploymentProduction, StringComparison.OrdinalIgnoreCase))
        {
            context.Fail(CxmlStatusCodes.Forbidden, "This development server only accepts deploymentMode=\"test\".");
            return Task.CompletedTask; // short-circuit: do not call next
        }

        return next();
    }
}
```

Steps are scoped, so they can take `ICxmlDbContext` and friends in their constructor. Setting
`context.Response` (what `Fail` does) ends the chain; the log entry is still written by `FinalizeStep`. The
built-in orders are in `CxmlPipelineOrder` (Finalize 0, Capture 100, Parse 200, DtdValidate 300,
Authenticate 400, Scenario 500, RuleValidate 600, Dispatch 700); any integer is allowed.

### 6. Register everything

In `src/CxmlDevServer.Web/Program.cs`, right after `AddCxmlDevServerCore(...)`:

```csharp
using Cxml.Protocol.Documents.StatusUpdates;
using CxmlDevServer.Core.DependencyInjection;
using CxmlDevServer.Core.Handlers;
using CxmlDevServer.Core.Pipeline.Steps;
using CxmlDevServer.Core.Rules;

builder.Services
    .AddCxmlTransaction<StatusUpdateRequest, StatusUpdateCodec, StatusUpdateHandler>()
    .AddDocumentRule<StatusUpdateCodeRule>()
    .AddPipelineStep<BlockProductionTrafficStep>();
```

That is all:

- `AddCxmlTransaction<TPayload, TCodec, THandler>()` registers the codec as parser **and** writer (the payload
  registry picks both up when it is built) and the handler. `TPayload` documents the transaction and
  constrains the type arguments.
- `ProfileRequestHandler` lists every registered handler, so a `ProfileRequest` now advertises
  `StatusUpdateRequest` with this server's `/cxml` URL.
- The new rule appears on the **Validation rules** page.
- Built-in transactions that should be known to a bare `CxmlReader`/`CxmlWriter` (outside DI) are additionally
  added to `CxmlPayloadRegistry.CreateDefault()` with an `internal static readonly` codec, as
  `OrderRequest` does. For a transaction that only exists inside this server, DI registration is enough.

Try it with the **Message sender** (paste the document, choose the Demo Buyer credentials):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE cXML SYSTEM "http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd">
<cXML payloadID="status-1@example" timestamp="2026-01-01T10:00:00+00:00" xml:lang="en-US">
  <Header>
    <From><Credential domain="NetworkId"><Identity>AN01000000001</Identity></Credential></From>
    <To><Credential domain="DUNS"><Identity>123456789</Identity></Credential></To>
    <Sender>
      <Credential domain="NetworkId">
        <Identity>AN01000000001</Identity>
        <SharedSecret>devsecret</SharedSecret>
      </Credential>
      <UserAgent>Test Buyer 1.0</UserAgent>
    </Sender>
  </Header>
  <Request deploymentMode="test">
    <StatusUpdateRequest>
      <DocumentReference payloadID="32232995@ariba.acme.com"/>
      <Status code="200" text="OK">Received by the buyer</Status>
    </StatusUpdateRequest>
  </Request>
</cXML>
```

### 7. Test it

The Core test project has a `TestApp` that hosts the whole stack on in-memory SQLite, seeded like a fresh
install. `ExtensibilityTests` shows the pattern, pass the registrations as `configureServices`:

```csharp
[Fact]
public async Task Status_update_is_accepted_and_correlated()
{
    await using var app = await TestApp.CreateAsync(configureServices: s => s
        .AddCxmlTransaction<StatusUpdateRequest, StatusUpdateCodec, StatusUpdateHandler>()
        .AddDocumentRule<StatusUpdateCodeRule>());

    var xml = Docs.Envelope(Docs.Request("""
        <StatusUpdateRequest>
          <DocumentReference payloadID="unknown"/>
          <Status code="299" text="Odd">hello</Status>
        </StatusUpdateRequest>
        """));

    var result = await app.PostAsync(xml);          // also asserts the response is DTD-valid

    Assert.Equal(200, TestApp.StatusOf(result).Code);
    var log = await app.GetLogAsync(result.LogId!.Value);
    Assert.Contains(log.Issues, i => i.Code == "STATUS-UNKNOWN-DOCUMENT");
    Assert.Contains(log.Issues, i => i.Code == "RULE-STATUSUPDATE-CODE");
}
```

## Part 2: an outbound transaction, `ConfirmationRequest`

Outbound means the server is the sender: it builds an envelope, posts it to the buyer's URL and logs the
exchange. `ICxmlOutboundClient.SendAsync(url, xml)` takes the **document text** and returns a
`CxmlOutboundResult` (HTTP status, parsed cXML status, the log entry id, or an `Error` when there was no
response). It records one `CxmlMessageLog` entry holding request and response (direction Outbound, channel
`ManualSend`) and correlates it to the partner, the order or the PunchOut session when it can.

**Note on DTDs.** `ConfirmationRequest` (like `ShipNoticeRequest`) is defined in `Fulfill.dtd`, not in
`cXML.dtd`. The DOCTYPE of the document must therefore name `Fulfill.dtd`, otherwise a validating receiver
(and this server's own validator) rejects it. `CxmlWriter` takes the DTD URL from `CxmlWriterOptions.DtdUrl`.

### 1. Model and writer

Only the writer is needed for a purely outbound payload (no parser: should a buyer send a
`ConfirmationRequest` to this server, it is still answered `450`). Child order follows the DTD:
`ConfirmationRequest (ConfirmationHeader, OrderReference, ...)`; `noticeDate` and `type` are required.

```csharp
using System.Xml.Linq;
using Cxml.Protocol;
using Cxml.Protocol.Documents;

public sealed class ConfirmationRequest : ICxmlPayload
{
    public const string Name = "ConfirmationRequest";

    public string ElementName => Name;

    public string OrderId { get; set; } = string.Empty;

    public DateTimeOffset? OrderDate { get; set; }

    public string OrderPayloadId { get; set; } = string.Empty;

    public string Type { get; set; } = "accept";   // accept, reject, detail, backordered, ...

    public DateTimeOffset NoticeDate { get; set; }

    public string? ConfirmId { get; set; }
}

public sealed class ConfirmationWriter : ICxmlPayloadWriter
{
    public string ElementName => ConfirmationRequest.Name;

    public XElement Write(ICxmlPayload payload)
    {
        var c = (ConfirmationRequest)payload;
        var header = new XElement(
            "ConfirmationHeader",
            new XAttribute("type", c.Type),
            new XAttribute("noticeDate", CxmlTimestamp.Format(c.NoticeDate)));
        if (c.ConfirmId is not null)
        {
            header.Add(new XAttribute("confirmID", c.ConfirmId));
        }

        var reference = new XElement("OrderReference", new XAttribute("orderID", c.OrderId));
        if (c.OrderDate is { } date)
        {
            reference.Add(new XAttribute("orderDate", CxmlTimestamp.Format(date)));
        }

        reference.Add(new XElement("DocumentReference", new XAttribute("payloadID", c.OrderPayloadId)));
        return new XElement(ConfirmationRequest.Name, header, reference);
    }
}
```

Register the writer (no handler, no parser):

```csharp
builder.Services.AddSingleton<ICxmlPayloadWriter, ConfirmationWriter>();
builder.Services.AddScoped<ConfirmationSender>();
```

### 2. The sender service

It builds the envelope with the types from `Cxml.Protocol.Envelope`, uses a `CxmlWriter` whose DOCTYPE names
`Fulfill.dtd`, and hands the text to `ICxmlOutboundClient`:

```csharp
using Cxml.Protocol;
using Cxml.Protocol.Documents;
using Cxml.Protocol.Envelope;
using Cxml.Protocol.Writing;
using CxmlDevServer.Core.Common;
using CxmlDevServer.Core.Domain;
using CxmlDevServer.Core.Messaging;

public sealed class ConfirmationSender
{
    private readonly CxmlWriter _writer;
    private readonly ICxmlOutboundClient _client;
    private readonly SupplierIdentityService _supplier;
    private readonly TimeProvider _time;

    public ConfirmationSender(
        CxmlPayloadRegistry registry, ICxmlOutboundClient client, SupplierIdentityService supplier, TimeProvider time)
    {
        // ConfirmationRequest is defined in Fulfill.dtd, so the DOCTYPE must name that file.
        _writer = new CxmlWriter(registry, new CxmlWriterOptions { DtdUrl = CxmlVersion.DtdUrl(fileName: "Fulfill.dtd") });
        _client = client;
        _supplier = supplier;
        _time = time;
    }

    public async Task<CxmlOutboundResult> SendAsync(
        PurchaseOrder order, TradingPartner buyer, string buyerUrl, CancellationToken cancellationToken = default)
    {
        var me = await _supplier.GetDefaultAsync(cancellationToken);
        var now = _time.GetLocalNow();
        var envelope = new CxmlEnvelope
        {
            PayloadId = PayloadIdGenerator.Generate(),
            Timestamp = now,
            Lang = "en-US",
            Header = new CxmlHeader
            {
                From = new Party { Credentials = [new Credential { Domain = me.Domain, Identity = me.Identity }] },
                To = new Party { Credentials = [new Credential { Domain = buyer.FromDomain, Identity = buyer.FromIdentity }] },
                Sender = new SenderParty
                {
                    // Add SharedSecret = "..." here if the buyer authenticates the supplier.
                    Credentials = [new Credential { Domain = me.Domain, Identity = me.Identity }],
                    UserAgent = "cXML Dev Server",
                },
            },
            Request = new CxmlRequest
            {
                DeploymentMode = order.DeploymentMode,
                Payload = new ConfirmationRequest
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    OrderPayloadId = order.PayloadId,
                    NoticeDate = now,
                },
            },
        };

        // The outbound client posts the text and logs request and response.
        return await _client.SendAsync(buyerUrl, _writer.Write(envelope), cancellationToken);
    }
}
```

`CxmlPayloadRegistry` is a singleton in DI; it already contains every registered writer. The result's
`CxmlStatusCode` tells you what the buyer answered (`200` for a processed confirmation); `Succeeded` only says
that an HTTP response arrived. The exchange is visible in the **Message log**.

Test it without a real buyer by aiming it at this server's own `/cxml` (answered `450` until you add a
handler) or, in a unit test, at an unreachable URL and reading the logged request:
`TestApp.Validate(log.RawXml)` verifies that the document is valid against `Fulfill.dtd`. To expose it in the
UI, add a button on `OrderDetail.razor` that calls `ConfirmationSender` (see the next section).

## Adding a UI page

The UI is Blazor Server with Bootstrap and `QuickGrid`. A page is a Razor component in
`src/CxmlDevServer.Web/Components/Pages/`; `Routes.razor` discovers it by its `@page` directive.

1. Create `Pages/Statuses.razor`:

   ```razor
   @page "/statuses"
   @inject UiData Data

   <PageHeader Title="Status updates" Subtitle="StatusUpdateRequest documents received." />

   @if (_rows is null)
   {
       <p class="text-body-secondary">Loading...</p>
   }
   else
   {
       <div class="card table-card">
           <div class="table-responsive-card">
               <table class="table table-hover">
                   <thead><tr><th>Time (UTC)</th><th>Partner</th><th>Status</th></tr></thead>
                   <tbody>
                   @foreach (var m in _rows)
                   {
                       <tr>
                           <td><a href="messages/@m.Id">@Fmt.Time(m.CreatedAt)</a></td>
                           <td>@(m.Partner?.Name ?? "-")</td>
                           <td><StatusBadge Code="@(m.CxmlStatusCode ?? m.HttpStatus)" /></td>
                       </tr>
                   }
                   </tbody>
               </table>
           </div>
       </div>
   }

   @code {
       private List<CxmlMessageLog>? _rows;

       protected override async Task OnInitializedAsync() =>
           _rows = await Data.QueryAsync(db => db.MessageLogs.AsNoTracking()
               .Include(m => m.Partner)
               .Where(m => m.DocumentType == "StatusUpdateRequest")
               .OrderByDescending(m => m.Id).Take(100).ToListAsync());
   }
   ```

   `UiData` (singleton) runs each query on its own `ICxmlDbContext`, which is what Blazor Server components
   need (a component lives longer than a request, so it must not hold a scoped context). Shared building
   blocks are in `Components/Shared` (`PageHeader`, `StatusBadge`, `EmptyState`, `CopyField`, `XmlViewer`,
   `ConfirmDeleteButton`, ...) and formatting helpers in `Services/Fmt.cs`. `_Imports.razor` already imports
   the domain namespace and `Microsoft.EntityFrameworkCore`. For long lists use `QuickGrid` with an
   `ItemsProvider` as `Orders.razor` does.

2. Link it in `Components/Layout/NavMenu.razor`:

   ```razor
   <NavLink class="nav-link" href="statuses"><Icon Name="mail" /> Status updates</NavLink>
   ```

   `Icon` takes a name from `Components/Shared/Icon.razor`.

3. Add it to the smoke test list in `tests/CxmlDevServer.Web.Tests/PageSmokeTests.cs` so a broken page fails
   the build.

For work that is not a page (an endpoint), add a `Map...Endpoints` extension in `Web/Endpoints` and call it
from `Program.cs`, like `CxmlEndpoints`.

## Adding an EF Core migration

Persistence is in `CxmlDevServer.Infrastructure`; entities are plain classes in `CxmlDevServer.Core/Domain`.

1. **Entity.** Add the class to `src/CxmlDevServer.Core/Domain/`. Implement `IAuditedEntity` to get
   `CreatedAt`/`UpdatedAt` stamped automatically.
2. **Context.** Add a `DbSet<T>` to the `ICxmlDbContext` interface (Core) and to `CxmlDevDbContext`
   (Infrastructure).
3. **Mapping.** Add an `IEntityTypeConfiguration<T>` in
   `src/CxmlDevServer.Infrastructure/Persistence/Configurations/`; `OnModelCreating` applies every
   configuration in the assembly. `DateTimeOffset` is stored as sortable UTC text and `decimal` as double (SQLite
   has no native types), through conventions in `CxmlDevDbContext`; enums are mapped with
   `HasConversion<string>()`.
4. **Migration.** From the repository root:

   ```bash
   dotnet tool restore
   dotnet ef migrations add AddMyEntity --project src/CxmlDevServer.Infrastructure
   ```

   The tool version is pinned in `.config/dotnet-tools.json`. The design-time factory
   (`CxmlDevDbContextDesignTimeFactory`) creates the context, so no startup project is needed. The files
   land in `src/CxmlDevServer.Infrastructure/Migrations`; review them and commit them.
5. **Apply.** Migrations run at startup (`Cxml:InitializeDatabaseOnStartup`, default `true`). To apply
   manually: `dotnet ef database update --project src/CxmlDevServer.Infrastructure`.
6. **Seed data** (optional): extend `SeedData` and `DatabaseInitializer`. Seeding happens only into an empty
   database.

## A buyer-simulator role (sketch)

The server is a *supplier* simulator today. A *buyer* role (send `PunchOutSetupRequest` and `OrderRequest`
to a real supplier, receive `ConfirmationRequest` and `ShipNoticeRequest`) can reuse most parts. This is a
design sketch, not something that exists yet:

- **Sending.** `CxmlWriter` builds envelopes for any registered payload and `ICxmlOutboundClient` posts and
  logs them (the **Message sender** already does exactly this for hand-edited documents).
  `MessageTemplateService` renders request templates from a trading partner. A `BuyerClient` service would
  build `PunchOutSetupRequest`/`OrderRequest` envelopes, send them to a supplier URL and read the typed
  response from `CxmlOutboundResult.ResponseEnvelope` (for example the `StartPage` URL of a
  `PunchOutSetupResponse`).
- **Receiving the cart.** `PunchOutReturnReceiver` and `/test-buyer/punchout-return` already are the buyer
  side of the `BrowserFormPost`.
- **Receiving requests.** `ConfirmationRequest` and `ShipNoticeRequest` arrive as ordinary requests, so
  they plug in as in Part 1: payload codec (parser) + `ICxmlRequestHandler`, registered with
  `AddCxmlTransaction`, with `Fulfill.dtd` in the DOCTYPE. In the buyer role the roles in the header are
  mirrored (the sender is the supplier, `To` is the buyer), so `AuthenticateStep` needs a role-aware check
  of `To` (supplier identities today); a `Role` on `TradingPartner` (a migration) would drive that, and
  possibly a second endpoint such as `/buyer/cxml` with its own step set.
- **Data.** A new entity (for example an outbound purchase order) via the migration recipe above, plus UI
  pages.

## Checklist for a new transaction

1. Payload class + parser/writer (`Cxml.Protocol`), child elements in DTD order.
2. Handler (inbound) or sender service (outbound); the right DTD in the DOCTYPE (`cXML.dtd` or `Fulfill.dtd`,
   `InvoiceDetail.dtd`, ...).
3. `AddCxmlTransaction`, `AddDocumentRule`, `AddPipelineStep` in `Program.cs`.
4. A test through `TestApp.PostAsync`, which also asserts that the response is DTD-valid.
5. Optional: UI page, entity and migration, sender template (`MessageTemplates` and `TemplateKeys`).
