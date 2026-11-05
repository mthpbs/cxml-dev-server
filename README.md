# cXML Dev Server

A **cXML supplier simulator** for testing procurement and buyer-side integrations. Point your procurement
system (SAP Ariba, Coupa, a custom e-procurement application, ...) at it and exercise PunchOut and purchase
orders against a server that implements **cXML 1.2.071** and shows you exactly what it received, what it
answered and why.

Built with .NET 10, Blazor Server and EF Core (SQLite). Nothing to install besides the .NET SDK; the cXML
DTDs are embedded and validation never touches the network.

## Features

- **PunchOut**: `PunchOutSetupRequest` with operation `create`, `edit` and `inspect`. The server answers with a
  one-session `StartPage` URL and serves a small demo shop. Checkout returns a `PunchOutOrderMessage` to the
  buyer's `BrowserFormPost` URL as an auto-submitting form (`cXML-base64` or `cXML-urlencoded`, per partner).
  `source` is answered with 450. You can browse, edit and inspect every session.
- **OrderRequest**: `new`, `update` and `delete`. Orders are validated, persisted with their lines, addresses and
  version history, and acknowledged with `Status 200`. A repeated `payloadID` is acknowledged without creating
  the order twice.
- **ProfileRequest**: answered with the transactions this server supports (as required by the cXML spec).
- **Validation**: every inbound document is checked against the **cXML 1.2.071 DTD** (embedded, offline) and
  against business rules the DTD cannot express (timestamp format, order total, unique line numbers, currency
  and country codes, ...). Each trading partner has a validation mode: **Strict** (reject with 406),
  **Lenient** (accept and record the issues) or **Off**.
- **Message log and inspector**: every document in and out, pretty-printed request and response, validation issues
  with line numbers, correlated messages (one PunchOut session or one order), and "replay in sender".
- **Response scenarios**: make the server misbehave on purpose: fail the next N requests with 500, answer 560 or
  406, delay, return a malformed body, or never answer. Ideal for testing retry logic.
- **Message sender**: build a request from a template (PunchOut create/edit/inspect, OrderRequest,
  ProfileRequest), edit it and post it to this server or to any cXML endpoint.
- **Built-in test buyer**: a `BrowserFormPost` receiver (`/test-buyer/punchout-return`) that decodes, validates and
  displays the returned `PunchOutOrderMessage`, so you can run a complete PunchOut round trip with no
  procurement system at all.
- **Admin UI** for trading partners, supplier identities, catalog, PunchOut sessions, purchase orders,
  messages, scenarios and rules.
- **Extensible**: new transactions, rules and pipeline steps register in one line each; see
  [docs/extending.md](docs/extending.md).

## Quick start

Prerequisite: the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone <this repository>
cd cxml-dev-server
dotnet run --project src/CxmlDevServer.Web
```

With the default launch profile the server listens on **http://localhost:5058** (the `https` profile also uses
https://localhost:7194; run `dotnet run --project src/CxmlDevServer.Web --launch-profile https`). The SQLite
database `data/cxml-dev.db` is created below the project folder and seeded with demo data on first start.

To listen elsewhere:

```bash
dotnet run --project src/CxmlDevServer.Web --urls http://0.0.0.0:8080
```

### Docker

```bash
docker build -t cxml-dev-server .
docker run --rm -p 8080:8080 -v cxml-data:/app/data cxml-dev-server
```

or `docker compose up --build`. The server listens on port 8080 in the container; the database lives in the
`/app/data` volume. See the comments in the [Dockerfile](Dockerfile) and [docker-compose.yml](docker-compose.yml).

## Try it in 2 minutes

Start the server (examples use `http://localhost:5058`; use `http://localhost:8080` for Docker), then:

**1. Ping**

```bash
curl -i http://localhost:5058/cxml
```

Answers `200` with `<Status code="200" text="OK">cXML Dev Server is running.</Status>`.

**2. Post the specification's example order**

```bash
curl -s -X POST http://localhost:5058/cxml \
  -H 'Content-Type: text/xml' \
  --data-binary @cXML-spec/Examples/0-OrderRequest.xml
```

The seeded partner "Acme (spec example)" matches the credentials in that file, so the order is accepted:
`<Status code="200" text="OK" .../>`. Open **Purchase orders** in the UI to see order `DO1234`. (Posting the
same file again is acknowledged as a duplicate `payloadID` and creates nothing new.)

**3. Start a PunchOut session**

```bash
curl -s -X POST http://localhost:5058/cxml \
  -H 'Content-Type: text/xml' \
  --data-binary @- <<'XML'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE cXML SYSTEM "http://xml.cxml.org/schemas/cXML/1.2.071/cXML.dtd">
<cXML payloadID="1700000000.1.1@curl" timestamp="2026-01-01T10:00:00+00:00" xml:lang="en-US">
  <Header>
    <From><Credential domain="NetworkId"><Identity>AN01000000001</Identity></Credential></From>
    <To><Credential domain="DUNS"><Identity>123456789</Identity></Credential></To>
    <Sender>
      <Credential domain="NetworkId">
        <Identity>AN01000000001</Identity>
        <SharedSecret>devsecret</SharedSecret>
      </Credential>
      <UserAgent>curl</UserAgent>
    </Sender>
  </Header>
  <Request deploymentMode="test">
    <PunchOutSetupRequest operation="create">
      <BuyerCookie>my-cookie-123</BuyerCookie>
      <BrowserFormPost>
        <URL>http://localhost:5058/test-buyer/punchout-return</URL>
      </BrowserFormPost>
    </PunchOutSetupRequest>
  </Request>
</cXML>
XML
```

The response contains `<PunchOutSetupResponse><StartPage><URL>http://localhost:5058/punchout/...</URL>`.
Open that URL in a browser, add a few items and press **Return to procurement system**: the browser posts the
cart to the built-in test buyer, which shows the decoded and validated `PunchOutOrderMessage` with your
`BuyerCookie` echoed.

**4. See the error paths**

```bash
# Wrong shared secret: HTTP 200 with <Status code="401" text="Unauthorized">
sed 's/abracadabra/wrong/' cXML-spec/Examples/0-OrderRequest.xml |
  curl -s -X POST http://localhost:5058/cxml -H 'Content-Type: text/xml' --data-binary @-

# Not XML: <Status code="406" text="Not Acceptable"> with the parse error
curl -s -X POST http://localhost:5058/cxml -H 'Content-Type: text/xml' -d '<cXML><oops>'
```

cXML-level errors are answered with **HTTP 200** and the error in the cXML `Status`, as the specification
describes (see [docs/architecture.md](docs/architecture.md)). Everything above also appears in the **Message
log** with its full request and response.

Prefer clicking? **Message sender** in the UI builds the same requests from templates.

## Default credentials

Created on the first start of an empty database (editable in the UI under *Trading partners* and
*Supplier identities*).

| Purpose | Name | Domain | Identity | Shared secret |
|---|---|---|---|---|
| Supplier identity (`To`), default | Demo Supplier | `DUNS` | `123456789` | none |
| Supplier identity (`To`) | Specification example supplier | `DUNS` | `942888711` | none |
| Trading partner (`From` and `Sender`) | Demo Buyer | `NetworkId` | `AN01000000001` | `devsecret` |
| Trading partner `From` | Acme (spec example) | `AribaNetworkUserId` | `admin@acme.com` | none |
| Trading partner `Sender` | Acme (spec example) | `AribaNetworkUserId` | `sysadmin@ariba.com` | `abracadabra` |

Both partners use Lenient validation. "Demo Buyer" returns PunchOut carts as `cXML-base64` and allows `edit`;
"Acme (spec example)" returns `cXML-urlencoded` and allows `create`. A disabled sample scenario ("fail the first
3 OrderRequests with 500") is seeded as well, plus about 25 catalog items.

## Endpoints

| Method and path | Purpose |
|---|---|
| `POST /cxml` | The cXML endpoint: PunchOutSetupRequest, OrderRequest, ProfileRequest. Accepts `text/xml` and `application/xml`, and also a form post with a `cXML-base64` or `cXML-urlencoded` field. |
| `GET /cxml` | Ping; answers a cXML `Response` with `Status 200`. |
| `GET /punchout/{token}` | The PunchOut shop for one session (the `StartPage` URL). |
| `POST /punchout/{token}/checkout` | Returns the cart: HTML page that auto-posts the `PunchOutOrderMessage` to the buyer's `BrowserFormPost` URL. |
| `POST /punchout/{token}/cancel` | Leaves the session: HTML page that posts an empty cart (create) or the original items with status 204 (edit). |
| `POST /test-buyer/punchout-return` | Built-in buyer receiver for the returned cart; decodes, validates, logs and displays it. |
| `GET /health` | Liveness probe; answers `OK`. |
| `/` | Admin UI (behind `/login` when `Admin:Enabled` is true): `/messages`, `/sessions`, `/orders`, `/sender`, `/scenarios`, `/partners`, `/identities`, `/catalog`, `/rules`. |

## Configuration

Settings live in `src/CxmlDevServer.Web/appsettings.json`. Override them with environment variables (use `__`
for the section separator, e.g. `Cxml__PublicBaseUrl`), `appsettings.Production.json` or command-line arguments.

| Setting | Default | Meaning |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=data/cxml-dev.db` | SQLite database. A relative path is placed below the application folder and created on startup. |
| `Cxml:PublicBaseUrl` | empty | Externally visible base URL (reverse proxy, tunnel, Docker host name). Used for `StartPage` and `ProfileResponse` URLs and the sender templates. Empty: derived from each request. |
| `Cxml:PunchOutSessionMinutes` | `60` | How long a PunchOut session (and its StartPage URL) stays usable. |
| `Cxml:AllowUnknownPartners` | `false` | `false`: senders that match no trading partner get 401. `true`: they are processed with a warning. |
| `Cxml:DefaultValidationMode` | `Lenient` | Validation mode for unknown partners: `Strict`, `Lenient` or `Off`. |
| `Cxml:UserAgent` | `cXML Dev Server 1.0` | `UserAgent` in the `Sender` of documents this server creates. |
| `Cxml:SupplierDomain` / `Cxml:SupplierIdentity` | `DUNS` / `123456789` | Supplier credential used in outbound headers when no supplier identity is stored. |
| `Cxml:DefaultCurrency` | `USD` | Currency for empty carts and templates. |
| `Cxml:InitializeDatabaseOnStartup` | `true` | Apply EF migrations and seed demo data at startup. |
| `Cxml:SeedDemoPartners` | `true` | `false`: an empty database gets no demo trading partners and no sample scenario (supplier identities and the catalog are still seeded). Use it for shared environments, since the demo partners have well-known secrets. |
| `Admin:Enabled` | `false` | `true`: the admin UI requires a login. `/cxml`, the PunchOut shop, the test buyer and `/health` stay open. |
| `Admin:Username` | `admin` | User name of the admin account. |
| `Admin:PasswordHash` | none | Password hash (ASP.NET Core Identity format); see *Admin login* below. Required (or `Admin:Password`) when the login is enabled, otherwise the application does not start. |
| `Admin:Password` | none | Plain-text fallback for secret stores; ignored when a hash is set. |
| `Admin:SessionHours` | `8` | Sliding lifetime of the login cookie. |
| `DataProtection:KeysPath` | `keys` folder next to the SQLite file | Where cookie and antiforgery keys are stored, so logins survive restarts. Empty: in memory. |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | unset (`true` in the Docker image) | Environment variable that makes the server honour `X-Forwarded-For` and `X-Forwarded-Proto` from a reverse proxy, so URLs it creates are `https://` behind a TLS-terminating proxy. |

### Admin login

By default the admin UI is open (local development). To protect a shared instance, create a password hash and
enable the login:

```bash
dotnet run --project src/CxmlDevServer.Web --no-launch-profile -- hash-password 'my password'   # prints the hash
# or: docker run -it --rm cxml-dev-server hash-password       (prompts, or reads the password from stdin)

export Admin__Enabled=true
export Admin__PasswordHash='<the hash>'
dotnet run --project src/CxmlDevServer.Web
```

Only the admin pages are protected. Buyers keep using `/cxml`, the PunchOut shop and the test buyer without a
login. The login is rate limited (5 attempts per minute and client address). The executable also has
`healthcheck [url]`, which exits 0 for a 2xx answer and 1 otherwise (default `http://127.0.0.1:8080/health`),
for container health checks in images without curl.

## Project layout

```
src/
  Cxml.Protocol/                 cXML library: models, reader, writer, DTD validator (no ASP.NET/EF)
  CxmlDevServer.Core/            domain, inbound pipeline, handlers, rules, PunchOut services
  CxmlDevServer.Infrastructure/  EF Core SQLite, migrations, seed data, outbound HTTP client
  CxmlDevServer.Web/             ASP.NET Core host: /cxml endpoints, Blazor admin UI, PunchOut shop
tests/
  Cxml.Protocol.Tests/           parsing, writing, DTD validation, encoders
  CxmlDevServer.Core.Tests/      pipeline, handlers, scenarios (SQLite in memory)
  CxmlDevServer.Web.Tests/       HTTP integration tests and component tests
cXML-spec/                       the cXML specification, examples and DTDs (the DTDs are embedded in the build)
docs/                            architecture, extending, client setup
```

Dependencies point one way: Web, then Infrastructure, then Core, then Protocol.

## Running the tests

```bash
dotnet build CxmlDevServer.slnx
dotnet test CxmlDevServer.slnx
```

The Web tests host the real application against a temporary SQLite file; no external services are needed.

## Documentation

- [docs/client-setup.md](docs/client-setup.md): configure your procurement system (generic, Ariba-style,
  Coupa-style), create your own trading partner, test error handling with scenarios, troubleshooting.
- [docs/architecture.md](docs/architecture.md): layers, the inbound pipeline, PunchOut and order flows, data model,
  validation, authentication, logging.
- [docs/extending.md](docs/extending.md): add a transaction, rule, pipeline step, UI page or migration.
- [cXML-spec/](cXML-spec/): the cXML specification this server implements.
