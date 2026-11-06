# Connecting your procurement system

This guide shows how to point a buyer system (SAP Ariba, Coupa or a custom application) at the cXML Dev
Server, how to add your own trading partner, how to provoke error conditions, and what to check when
something is rejected.

- [What the server expects](#what-the-server-expects)
- [Generic setup](#generic-setup)
- [Ariba-style setup](#ariba-style-setup)
- [Coupa-style setup](#coupa-style-setup)
- [Create your own trading partner](#create-your-own-trading-partner)
- [PunchOut return encodings](#punchout-return-encodings)
- [Testing error handling with scenarios](#testing-error-handling-with-scenarios)
- [Troubleshooting](#troubleshooting)

> Field names in procurement products differ between versions and tenants. The tables below use the
> typical labels; use the "cXML element" column to find the right field in your product.

## What the server expects

One URL does everything: `POST {base}/cxml` receives `PunchOutSetupRequest`, `OrderRequest` and
`ProfileRequest` (`{base}` is for example `http://localhost:5058`, or `http://<host>:8080` in Docker). The
dashboard shows the exact URL to copy.

The server identifies a request by the `Header` of the cXML document:

```xml
<Header>
  <From>   <!-- the buyer organisation -->
    <Credential domain="NetworkId"><Identity>AN01000000001</Identity></Credential>
  </From>
  <To>     <!-- the supplier: this server -->
    <Credential domain="DUNS"><Identity>123456789</Identity></Credential>
  </To>
  <Sender> <!-- who transmits the document, plus the proof -->
    <Credential domain="NetworkId">
      <Identity>AN01000000001</Identity>
      <SharedSecret>devsecret</SharedSecret>
    </Credential>
    <UserAgent>My Procurement System 1.0</UserAgent>
  </Sender>
</Header>
```

| cXML element | Must match | Seeded value (Demo Buyer) |
|---|---|---|
| `Sender/Credential` domain and `Identity` | the trading partner's *Sender domain* / *Sender identity* | `NetworkId` / `AN01000000001` |
| `Sender/Credential/SharedSecret` | the partner's *Shared secret* (unless authentication is "None") | `devsecret` |
| `From/Credential` | the partner's *From domain* / *From identity* (used to pick the partner when several share a sender) | `NetworkId` / `AN01000000001` |
| `To/Credential` | a *Supplier identity*, or the partner's optional *Expected To* | `DUNS` / `123456789` (also `DUNS` / `942888711`) |
| `Request@deploymentMode` | the partner's *Deployment mode* if set (a mismatch is a warning) | `test` or `production` |

The "Seeded value" column describes the demo partner. In the deployed environments (Docker Compose, Azure) the
demo partners are seeded by default too; on a shared or internet-reachable host switch them off
(`Cxml:SeedDemoPartners=false`) and create your own partner with its own secret first; see the [new-environment checklist](deployment/configuration.md#new-environment-checklist).

Matching is case-insensitive. If the same buyer system sends several credentials in `From` (a marketplace
and the member organisation), any one of them can match.

## Generic setup

In the supplier / PunchOut catalog configuration of your system enter:

| Setting in your system | Value |
|---|---|
| PunchOut URL (setup request URL) | `http://<server>:<port>/cxml` |
| Order URL (OrderRequest URL) | `http://<server>:<port>/cxml` (the same) |
| Supplier (To) domain and identity | `DUNS` and `123456789` |
| Buyer (From) domain and identity | `NetworkId` and `AN01000000001` |
| Sender domain and identity | `NetworkId` and `AN01000000001` |
| Shared secret | `devsecret` |
| Deployment mode | `test` (or `production`: the server accepts both) |
| cXML version | 1.2.071 is what the server validates against; a document naming another DTD version is still validated against 1.2.071 and gets a `CXML-DTD-VERSION-FALLBACK` warning |
| Content type | `text/xml` (or `application/xml`) |
| Return method for the cart | form post (`cXML-base64` preferred) |

Your system must be able to reach the server: if it runs in the cloud, expose the server through a tunnel or
reverse proxy and set `Cxml:PublicBaseUrl` (see [Troubleshooting](#start-page-url-is-wrong-behind-a-proxy-or-in-docker)). The ready-made
[deployments](deployment/README.md) already put HTTPS in front of the server; the URL to enter is then
`https://<your-host>/cxml`.
The reverse also matters: the browser that opens the StartPage must reach the server, and the server's
returned form must reach your system's `BrowserFormPost` URL *from the browser* (it is posted by the user's
browser, not by the server).

Quick connectivity check from the buyer's network: `GET http://<server>:<port>/cxml` must answer
`<Status code="200" text="OK">`. Then run a `ProfileRequest` or a PunchOut from your system and look at the
**Message log**: every request arrives there, accepted or not.

## Ariba-style setup

In an Ariba-style buying application the PunchOut catalog and the supplier connection ask for the values
below (SAP Ariba Buying and the Ariba Network use these concepts; names vary by version):

| Ariba-style field | Value |
|---|---|
| Punchout URL / Supplier URL | `http://<server>:<port>/cxml` |
| Supplier ID (domain `DUNS`) | `123456789` |
| Network ID / buyer ID (domain `NetworkId`) | `AN01000000001` |
| Shared secret (supplier password) | `devsecret` |
| Order routing | cXML (online), URL as above |
| PunchOut "Level" / method | cXML PunchOut with a form post, `cXML-base64` |

Ariba Network adds a marketplace hop: the `Sender` is the network (for example `AribaNetworkUserId` /
`sysadmin@ariba.com`) while `From` is the buying organisation (`admin@acme.com`). The seeded partner
**Acme (spec example)** models exactly that, with the credentials of the specification's
`Examples/0-OrderRequest.xml`:

| Partner | From | Sender | Shared secret | Return encoding |
|---|---|---|---|---|
| Acme (spec example) | `AribaNetworkUserId` / `admin@acme.com` | `AribaNetworkUserId` / `sysadmin@ariba.com` | `abracadabra` | `cXML-urlencoded` |

Use it to replay specification documents, or copy it for your own network credentials. The supplier
identity the example sends to is `DUNS` / `942888711`, which is seeded too.

## Coupa-style setup

A Coupa-style system takes the PunchOut site details in a form (Setup, Punchout Sites) and the order
transmission in the supplier record:

| Coupa-style field | Value |
|---|---|
| Punchout / cXML URL | `http://<server>:<port>/cxml` |
| Supplier domain and identity (cXML `To`) | `DUNS` / `123456789` |
| Customer domain and identity (cXML `From` and `Sender`) | `NetworkId` / `AN01000000001` (your own identity: create a partner for it) |
| Shared secret | `devsecret` |
| Order method | cXML, same URL |
| Return / checkout handling | form post to the Coupa return URL; encoding `cXML-base64` |

Coupa lets you choose the domain and identity it sends as its own, so the simplest path is to create a
trading partner that matches what your Coupa instance sends. Look at the first request in the **Message
log** (a rejected one still shows the header it carried) and copy its `From`, `Sender` and `To`.

## Create your own trading partner

1. Open **Trading partners**, **New**.
2. Fill in the credentials your system sends, exactly as they appear in the `Header`:
   *From domain/identity*, *Sender domain/identity*, *Shared secret*. If your system sends a different `To`
   than a supplier identity of this server, either add that identity under **Supplier identities** or enter it
   as *Expected To* on the partner.
3. Choose the behavior:

   | Setting | Meaning |
   |---|---|
   | Authentication | *Shared secret* (default) or *None (identify only)*: the partner is recognized by its credentials but no secret is checked. |
   | Validation mode | *Strict*: DTD and business-rule errors are rejected with 406 (and some order conflicts with 409/412). *Lenient*: accepted, issues recorded in the message log. *Off*: no validation. Start with Lenient and tighten to Strict once your documents are clean. |
   | Post encoding | `cXML-base64` or `cXML-urlencoded` for the PunchOut return form. |
   | operationAllowed | What the returned `PunchOutOrderMessage` announces: `create`, `inspect`, `edit` (or `source`). `edit` lets the buyer reopen the cart later. |
   | Deployment mode | Warn when `Request@deploymentMode` differs; *Accept both* by default. |
   | Active | An inactive partner is answered with 403. |

4. In **Message sender** choose a template and your partner: the credentials, a fresh `payloadID` and
   timestamp are filled in. Send it to this server to prove the configuration before trying your real system.

Unknown senders are answered with 401 unless `Cxml:AllowUnknownPartners` is `true`. That is handy for a
first look at an unknown system's documents (they are processed with a warning and the
`Cxml:DefaultValidationMode`), but a real partner is better because the log is then attributed to it.

## PunchOut return encodings

At checkout the server returns an HTML page with an auto-submitting form that posts to the buyer's
`BrowserFormPost/URL`. The form has **one hidden field**, chosen by the partner's *Post encoding*:

| Field name | Value | Notes |
|---|---|---|
| `cXML-base64` (default) | the whole `cXML` document, UTF-8, base64 | Preferred by the cXML specification; safe for any characters. Decode, then parse. |
| `cXML-urlencoded` | the whole document as a string, HTML-escaped for the attribute (`&amp;`, `&lt;`, `&gt;`, `&quot;`, `&#39;`) and with numeric character references (`&#233;`) for non-ASCII characters | The server does not URL-encode the value itself: the browser does that when it submits the form, and your server receives the original XML after normal form decoding. |

A buyer should look for `cXML-base64` first and fall back to `cXML-urlencoded`. The built-in test buyer
(`/test-buyer/punchout-return`) does the same and also accepts either. If your system only supports one of
them, set the partner accordingly. The page has a "Continue" button for browsers without JavaScript, and
the message sender can show the document before it is posted.

For `edit` and `inspect` sessions the setup request lists `ItemOut` elements; the server rebuilds the cart
from them and keeps the `SupplierPartAuxiliaryId` it issued as the "supplier cookie" so the same items come
back on the next edit. Cancelling a `create` returns an empty cart; cancelling an `edit` returns the
original items with `Status 204`.

## Testing error handling with scenarios

**Response scenarios** make the server fail in a controlled way, so you can verify your retry and error
handling without breaking anything. Create them under **Response scenarios**:

| Field | Use |
|---|---|
| Partner / Document type / Request contains | Narrow when it applies (for example only `OrderRequest`, or only requests containing `PO-1234`). Empty means any. |
| Priority | Higher wins when several scenarios match. |
| Behavior | *Respond with a status* (cXML status code, text, detail, optional HTTP status), *Delay only, then process normally*, *Answer with a malformed body*, *Never answer (timeout)*. |
| Delay (ms) | Applied before the behavior. |
| Remaining uses | Applies the scenario to the next N matching requests, then it switches itself off. Empty: unlimited. |

Scenarios run after authentication and before validation and handling, so a forced answer replaces the real
processing (nothing is persisted for it). The message log marks the requests a scenario answered.

Typical recipes:

| Test | Scenario |
|---|---|
| **Retry on transient failure**: the first 3 orders fail with 500, the 4th succeeds | Document type `OrderRequest`, Respond, cXML status `500`, *Remaining uses* `3`. (The seeded, disabled "Sample: fail the first 3 OrderRequests with 500" is exactly this: enable it.) |
| **Server busy**: temporary error | Respond with cXML status `560` ("Temporary server error"), remaining uses 2. |
| **No retry on permanent failure**: your system must not resend | Respond with cXML status `406` or `409`, remaining uses 1; verify that the **Message log** shows exactly one request. |
| **Transport-level failure**: HTTP 503 with a body | Respond, cXML status `500`, *HTTP status* `503`. |
| **Read timeout** | *Never answer (timeout)* (the connection is dropped) or *Delay only* with a delay above your client timeout. |
| **Garbage response** | *Answer with a malformed body* (an empty body sends a truncated cXML document). |

The convention to test against: cXML status `4xx` is **permanent** (fix the document, do not resend
unchanged), `5xx` is **transient** (retry later). The server normally returns these inside **HTTP 200**
responses, as the cXML specification describes; only a scenario's explicit *HTTP status*, malformed body or
dropped connection changes the transport. Make sure your client reads the `Status/@code`, not just the HTTP
status.

## Troubleshooting

Always start with the **Message log**: open the message, read the request and the response, and look at the
list of issues (each has a code, a message and, for XML problems, the line and position).

### 401 Unauthorized

The `Status` detail names the reason:

| Detail / issue code | Cause and fix |
|---|---|
| `No trading partner matches Sender credential domain 'X' identity 'Y'` (`AUTH-UNKNOWN-SENDER`) | No partner has this *Sender domain/identity* (or, failing that, *From*). Compare case and spelling with the `Header`, or create the partner. Alternatively enable `Cxml:AllowUnknownPartners`. |
| `The SharedSecret in the Sender credential is missing or incorrect` (`AUTH-BAD-SECRET`) | The secret in the document is different or absent. Also check for trailing whitespace or a different credential carrying the secret. Set the partner's authentication to "None" to disable the check. |
| `The To credential ... does not match any supplier identity` (`AUTH-TO-MISMATCH`) | Only an error for partners in *Strict* mode (a warning otherwise). Add the identity under **Supplier identities** or as the partner's *Expected To*. |
| `The document has no Header` (`AUTH-NO-HEADER`) | The request has no `Header` / `Sender`. |

A disabled partner gets `403` (`AUTH-PARTNER-INACTIVE`).

### 406 Not Acceptable

| Detail | Cause and fix |
|---|---|
| `The request body is empty` | The client sent no body; check the HTTP method (`POST`) and `--data-binary` / content length. |
| `The request is not a well-formed cXML document: ... (line L, position P)` | Not XML or not cXML (wrong root element, unclosed tag, text before `<?xml`). |
| `The form field does not contain valid base64` | A form post whose `cXML-base64` field is damaged (for example `+` turned into a space is repaired, but truncation is not). |
| `The document failed validation (Strict mode): ...` followed by `Error CXML-DTD: ...` lines | The partner is in *Strict* mode and the document violates the DTD or a rule. The lines show the element, the expected content and the position. Fix the document, or switch the partner to *Lenient* to see the same findings without rejection. |

Common DTD errors: child elements in the wrong order (the cXML DTD fixes the order), a missing required
attribute (`ItemOut@quantity`, `OrderRequestHeader@orderID`, ...), `Money` amounts with a comma instead of a
decimal point, a timestamp ending in `Z` (cXML wants `+00:00`). Documents for other DTDs must name the right file in the DOCTYPE: for example `ConfirmationRequest`
and `ShipNoticeRequest` validate against `Fulfill.dtd`, not `cXML.dtd`.

### 450 Not Implemented

The request type has no handler: this server implements `PunchOutSetupRequest` (create/edit/inspect),
`OrderRequest` and `ProfileRequest`. PunchOut `source` and every other request are answered with 450 and the
list of supported requests. A `Response` or `Message` document posted to `/cxml` is rejected as well.

### 400 / 409 / 412 on orders and PunchOut

`400`: `orderID` or `BuyerCookie` missing, no `ItemOut`, no valid `BrowserFormPost/URL`. `409`, `412`: only in
*Strict* mode (the order already exists, version not higher, unknown order on update/delete, edit/inspect
without items); in *Lenient* mode the same cases are accepted with a warning.

### Start page URL is wrong (behind a proxy or in Docker)

The `StartPage` URL in the `PunchOutSetupResponse` is built from the request the server sees: scheme, host
and port. The Docker image honours `X-Forwarded-For` and `X-Forwarded-Proto`
(`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`), so behind a proxy that sends them and keeps the original `Host`
header (Caddy, Azure Container Apps) the URL is already `https://...`. It is still wrong when the proxy does
not send `X-Forwarded-Proto`, rewrites `Host`, uses another port or path prefix, or the server runs
without the image (`http://localhost:8080/punchout/...` instead of `https://dev.example.com/punchout/...`).
`X-Forwarded-Host` is not evaluated. Set the externally visible base URL:

```bash
Cxml__PublicBaseUrl=https://dev.example.com dotnet run --project src/CxmlDevServer.Web
```

or in `appsettings.json`: `"Cxml": { "PublicBaseUrl": "https://dev.example.com" }`, or `-e Cxml__PublicBaseUrl=...`
for Docker (the self-hosted stack takes it from `CXML_PUBLIC_BASE_URL`, see
[deployment/configuration.md](deployment/configuration.md#forwarded-headers-and-publicbaseurl)). The setting is also used for the URLs in `ProfileResponse` and the `BrowserFormPost` of the
sender templates. If the server is served below a path prefix, include it (`https://host/cxml-dev`).

### The cart never arrives at the buyer

The server only renders the form; the **browser** posts it to your `BrowserFormPost/URL`. So that URL must be
reachable from the user's browser, accept `POST` with `application/x-www-form-urlencoded`, and read the
`cXML-base64` (or `cXML-urlencoded`) field. Check the outbound `PunchOutOrderMessage` in the Message log
(channel `BrowserPost`), and use the "Show the cXML being returned" section of the checkout page to inspect the document. For a buyer-less
check set the `BrowserFormPost/URL` to `{base}/test-buyer/punchout-return` and read the result there.
`The PunchOut session has expired` means more than `Cxml:PunchOutSessionMinutes` passed; `already completed`
means the cart was already returned or cancelled (start a new session).

### Order accepted but nothing changed

A repeated `payloadID` is acknowledged without side effects (`ORDER-DUPLICATE-PAYLOAD`): regenerate the
`payloadID` for every document. Orders are keyed by (partner, `orderID`); an `update` must carry a higher
`orderVersion` to be applied cleanly.
