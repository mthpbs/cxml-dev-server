# Configuration reference and new-environment checklist

Every setting of the cXML Dev Server, how each platform provides it, how secrets are handled, and the ordered
steps to configure a freshly deployed environment. Platform procedures are in
[azure-container-apps.md](azure-container-apps.md) and [self-hosted.md](self-hosted.md).

- [How settings are read](#how-settings-are-read)
- [Settings reference](#settings-reference)
- [Where each platform sets them](#where-each-platform-sets-them)
- [Secrets handling](#secrets-handling)
- [Forwarded headers and PublicBaseUrl](#forwarded-headers-and-publicbaseurl)
- [Admin login](#admin-login)
- [Microsoft 365 sign-in (Entra ID)](#microsoft-365-sign-in-entra-id)
- [Data protection keys](#data-protection-keys)
- [Logging](#logging)
- [New-environment checklist](#new-environment-checklist)
- [Security hardening checklist](#security-hardening-checklist)
- [Example environment matrix](#example-environment-matrix)

## How settings are read

The application is a standard ASP.NET Core host. A setting can come from, in increasing precedence:

1. `appsettings.json` in the application folder (defaults, documented with comments:
   [`src/CxmlDevServer.Web/appsettings.json`](../../src/CxmlDevServer.Web/appsettings.json)),
2. `appsettings.<Environment>.json`,
3. **environment variables**, with `__` (two underscores) as the separator for nesting:
   `Cxml:PublicBaseUrl` becomes `Cxml__PublicBaseUrl`,
4. command-line arguments (`--Cxml:PublicBaseUrl=...`).

In containers use environment variables. Boolean values are `true` and `false`.

## Settings reference

### Hosting

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| URLs to listen on | `ASPNETCORE_URLS` | `http://localhost:5000` locally; **`http://+:8080`** in the image | The image listens on plain HTTP port 8080. TLS is terminated by a proxy in front. Changing the port also breaks the platform probes and the Compose health check. |
| Environment name | `ASPNETCORE_ENVIRONMENT` | `Production` (unset); `Development` with `dotnet run` and the launch profiles | `Development` shows detailed errors. Containers should run `Production`. |
| Forwarded headers | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | unset; **`true` in the image**, in the Compose file and in the Bicep template | Honour `X-Forwarded-For` and `X-Forwarded-Proto` from a reverse proxy. See [below](#forwarded-headers-and-publicbaseurl). |
| Allowed host names | `AllowedHosts` | `*` | ASP.NET Core host filtering. Leave it at `*`: the platform health probes and the container health check address the app by IP address. |

### Database

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | `Data Source=data/cxml-dev.db`; **`Data Source=/app/data/cxml-dev.db`** in the image | SQLite database. A relative path is placed below the application folder; its folder is created on start-up. The folder must be writable (the image's `app` user, uid 1654, owns `/app/data`). |

### cXML server (`Cxml:*`)

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `Cxml:PublicBaseUrl` | `Cxml__PublicBaseUrl` | empty (derived from each request) | Externally visible base URL, without a trailing slash. Used for `StartPage` URLs, `ProfileResponse` URLs, the dashboard's endpoint display and the `BrowserFormPost` URL in sender templates. |
| `Cxml:PunchOutSessionMinutes` | `Cxml__PunchOutSessionMinutes` | `60` | How long a PunchOut session (and its StartPage URL) stays usable. |
| `Cxml:AllowUnknownPartners` | `Cxml__AllowUnknownPartners` | `false` | `false`: senders that match no trading partner get cXML `401`. `true`: they are processed with a warning (no secret check). |
| `Cxml:DefaultValidationMode` | `Cxml__DefaultValidationMode` | `Lenient` | Validation mode for unknown partners: `Strict`, `Lenient` or `Off`. |
| `Cxml:UserAgent` | `Cxml__UserAgent` | `cXML Dev Server 1.0` | `UserAgent` in the `Sender` of documents the server creates. |
| `Cxml:SupplierDomain` | `Cxml__SupplierDomain` | `DUNS` | Supplier credential domain in outbound headers when no supplier identity is stored. |
| `Cxml:SupplierIdentity` | `Cxml__SupplierIdentity` | `123456789` | Supplier credential identity in outbound headers when no supplier identity is stored. |
| `Cxml:DefaultCurrency` | `Cxml__DefaultCurrency` | `USD` | Currency for empty carts and templates. |
| `Cxml:InitializeDatabaseOnStartup` | `Cxml__InitializeDatabaseOnStartup` | `true` | Apply database migrations and seed an empty database at start-up. Leave it on. |
| `Cxml:SeedDemoPartners` | `Cxml__SeedDemoPartners` | `true` (application default, `main.bicepparam` and the Compose stack) | In an **empty** database: `true` also creates the demo trading partners (secrets `devsecret`, `abracadabra`) and a disabled sample scenario; `false` seeds only the supplier identities and the catalog. Has no effect on a database that already has data. |

### Admin login (`Admin:*`)

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `Admin:Enabled` | `Admin__Enabled` | `false` | `true`: the admin UI requires a login. `/cxml`, the PunchOut shop (`/punchout/*`), the test buyer (`/test-buyer/*`) and `/health` stay open. The Compose file and the Bicep template turn it on. |
| `Admin:Username` | `Admin__Username` | `admin` | The single password account. Only used when a password is configured. |
| `Admin:PasswordHash` | `Admin__PasswordHash` | none | ASP.NET Core Identity password hash (format v3), created with `hash-password`. **Preferred.** When the login is enabled, the application needs a password (this or `Admin:Password`), the Microsoft sign-in below, or both; otherwise it does not start. |
| `Admin:Password` | `Admin__Password` | none | Plain-text fallback for secret stores that cannot hold a hash. Ignored when `Admin:PasswordHash` is set. |
| `Admin:SessionHours` | `Admin__SessionHours` | `8` | Sliding lifetime of the login cookie, in hours (at least 1). |
| `Admin:Microsoft:TenantId` | `Admin__Microsoft__TenantId` | none | Directory (tenant) id of your Microsoft 365 organization, a GUID. Single tenant only: `common`, `organizations` and `consumers` are rejected. Setting any of tenant id, client id or secret switches the Microsoft sign-in on and then all three are required. See [Microsoft 365 sign-in](#microsoft-365-sign-in-entra-id). |
| `Admin:Microsoft:ClientId` | `Admin__Microsoft__ClientId` | none | Application (client) id of the Entra app registration, a GUID. |
| `Admin:Microsoft:ClientSecret` | `Admin__Microsoft__ClientSecret` | none | Client secret of the app registration. **Secret**; rotate it before it expires. |
| `Admin:Microsoft:CallbackPath` | `Admin__Microsoft__CallbackPath` | `/signin-oidc` | Path of the sign-in callback; the redirect URI in Entra is the site URL plus this path. |
| `Admin:Microsoft:Instance` | `Admin__Microsoft__Instance` | `https://login.microsoftonline.com/` | Identity platform; change only for a sovereign cloud (an absolute `https` URL). |

### Data Protection (`DataProtection:*`)

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `DataProtection:KeysPath` | `DataProtection__KeysPath` | a `keys` folder next to the SQLite file; **`/app/data/keys`** in the image | Folder for the keys that protect the login cookie, antiforgery tokens and Blazor circuit state. An empty value keeps the keys in memory (a log warning is written). |

### Logging (`Logging:*`)

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` | `Information` | Default level of the console log. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Logging__LogLevel__Microsoft.AspNetCore` | `Warning` | ASP.NET Core framework messages. |
| `Logging:LogLevel:Microsoft.EntityFrameworkCore` | `Logging__LogLevel__Microsoft.EntityFrameworkCore` | `Warning` | Database messages. |
| `Logging:LogLevel:System.Net.Http.HttpClient` | `Logging__LogLevel__System.Net.Http.HttpClient` | `Warning` | Outbound HTTP calls of the Message sender. |

### Command-line tools

The executable doubles as two small tools (the image has no shell utilities). They run instead of the web
host and work as `dotnet CxmlDevServer.Web.dll <command>` and `docker run <image> <command>`:

| Command | Purpose |
|---|---|
| `hash-password [password]` | Prints an `Admin:PasswordHash` value. Without an argument it reads the password from standard input: in an interactive terminal (`docker run -it`) it prompts without echo, otherwise it reads a piped line (`printf '%s' "$PW" \| docker run -i ...`). Exit code 2 when no password was given. |
| `healthcheck [url]` | Requests the URL (default `http://127.0.0.1:8080/health`) and exits `0` for a 2xx answer, otherwise `1`. Used by the Compose health check. |

```bash
docker run --rm -it ghcr.io/mthpbs/cxml-dev-server:latest hash-password
```

## Where each platform sets them

### Self-hosted Compose (`deploy/selfhost/.env`)

| `.env` variable | Default | Becomes |
|---|---|---|
| `DOMAIN` (required) | none | Caddy site address; default for `Cxml__PublicBaseUrl` |
| `ACME_EMAIL` (required) | none | Caddy account e-mail |
| `ADMIN_PASSWORD_HASH` | none | `Admin__PasswordHash` (set it, the three `ADMIN_MS_*` values, or both) |
| `ADMIN_MS_TENANT_ID`, `ADMIN_MS_CLIENT_ID`, `ADMIN_MS_CLIENT_SECRET` | none | `Admin__Microsoft__TenantId`, `Admin__Microsoft__ClientId`, `Admin__Microsoft__ClientSecret` |
| `IMAGE` | `ghcr.io/mthpbs/cxml-dev-server:latest` | Image of the `app` service |
| `ADMIN_USERNAME` | `admin` | `Admin__Username` |
| `CXML_PUBLIC_BASE_URL` | `https://<DOMAIN>` | `Cxml__PublicBaseUrl` |
| `CXML_ALLOW_UNKNOWN_PARTNERS` | `false` | `Cxml__AllowUnknownPartners` |
| `CXML_SEED_DEMO_PARTNERS` | `true` | `Cxml__SeedDemoPartners` |
| `TLS_MODE` | `acme` | Caddy certificate source (`acme` or `internal`) |
| `HTTP_PORT`, `HTTPS_PORT` | `80`, `443` | Published ports of Caddy |
| `BACKUP_INTERVAL_HOURS`, `BACKUP_RETENTION_DAYS` | `24`, `14` | Backup schedule and retention |
| `TZ` | `UTC` | Time zone of the containers |

The Compose file always sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` and `Admin__Enabled=true`. Other
settings from the reference above (for example `Cxml__PunchOutSessionMinutes`) go into the `environment:`
block of the `app` service in `docker-compose.yml`.

### Azure Container Apps (`deploy/azure/main.bicepparam`)

| Bicep parameter | Default in `main.bicepparam` | Becomes |
|---|---|---|
| `containerImage` (env `CONTAINER_IMAGE`) | `ghcr.io/mthpbs/cxml-dev-server:latest` | Container image |
| `adminEnabled` | `true` | `Admin__Enabled` |
| `adminUsername` | `admin` | `Admin__Username` |
| `adminPasswordHash` (env `ADMIN_PASSWORD_HASH`) | empty (while `adminEnabled`: required unless the Microsoft sign-in is complete) | `Admin__PasswordHash`, via the container app secret `admin-password-hash` |
| `adminMicrosoftTenantId`, `adminMicrosoftClientId` (env `ADMIN_MS_TENANT_ID`, `ADMIN_MS_CLIENT_ID`) | empty | `Admin__Microsoft__TenantId`, `Admin__Microsoft__ClientId` (only set when non-empty) |
| `adminMicrosoftClientSecret` (env `ADMIN_MS_CLIENT_SECRET`) | empty | `Admin__Microsoft__ClientSecret`, via the container app secret `admin-microsoft-client-secret` |
| `publicBaseUrl` (env `PUBLIC_BASE_URL`) | empty | `Cxml__PublicBaseUrl` (only set when non-empty) |
| `allowUnknownPartners` | `false` | `Cxml__AllowUnknownPartners` |
| `seedDemoPartners` | `true` | `Cxml__SeedDemoPartners` |
| `ipAllowList` | `[]` (open) | Ingress IP restrictions |
| `customDomains` (env `CUSTOM_DOMAINS_JSON`) | `[]` | Custom domains kept bound to the ingress (`name`, `certificateId`, optional `bindingType`); see the [Azure runbook](azure-container-apps.md#custom-domain-with-a-managed-certificate) |
| `extraEnv` | none | Additional plain environment variables, for example `{ name: 'Cxml__DefaultCurrency', value: 'EUR' }`. Never put secrets here. |
| `cpu`, `memory` | `0.5`, `1Gi` | Replica size |

The template always sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`,
`ConnectionStrings__Default=Data Source=/app/data/cxml-dev.db` and `DataProtection__KeysPath=/app/data/keys`.

### Local

```bash
Admin__Enabled=true Admin__PasswordHash='<hash>' dotnet run --project src/CxmlDevServer.Web
```

Export the variables in your shell profile or a script outside the repository. Do not put the hash into the
committed `appsettings.json`.

## Secrets handling

What is secret: `Admin__PasswordHash` (or `Admin__Password`), `Admin__Microsoft__ClientSecret` (it expires:
note the date and rotate it before then), registry tokens, and every trading partner's shared secret (stored in the database and visible to administrators in the UI).

| Platform | How secrets are supplied | Rules |
|---|---|---|
| **GitHub Actions** | Environment secrets (`ADMIN_PASSWORD_HASH`, `ADMIN_MS_CLIENT_SECRET`, `GHCR_PULL_TOKEN`, `AZURE_*`); the tenant and client ids are plain variables (`ADMIN_MS_TENANT_ID`, `ADMIN_MS_CLIENT_ID`). | Never echo them. The workflow passes them to the Bicep deployment through environment variables, not command-line arguments. Use a GitHub environment with required reviewers for production. Azure access uses OIDC federation: no Azure password or client secret is stored anywhere. |
| **Azure Container Apps** | Bicep `@secure()` parameters become container app secrets (`admin-password-hash`, `admin-microsoft-client-secret`, `registry-password`) referenced by `secretRef`. | Secrets are not visible in the deployment history or in the template output. Users with Contributor rights on the resource group can read them with `az containerapp secret list --show-values`. Changing a secret requires a restart to take effect. Do not put secrets in `extraEnv` or `main.bicepparam`. |
| **Self-hosted Compose** | `deploy/selfhost/.env`. | The file is git-ignored; keep it mode `600`, owned by the deploying user, and back it up separately. Values are visible to anyone who can run `docker inspect` on the host: restrict Docker access. |
| **Local** | Environment variables. | Do not commit hashes or passwords into `appsettings.json`. |

Use `Admin__PasswordHash` rather than `Admin__Password`: the hash is useless for signing in elsewhere, and
the plain password would appear in clear text in container inspection and logs of your platform. The plain
setting exists for secret stores that can only hold a password you must type.

To change the admin password, create a new hash and replace the value (GitHub secret and redeploy; or `.env`
and `docker compose up -d`; see the platform runbooks).

## Forwarded headers and PublicBaseUrl

Both deployments put a TLS-terminating reverse proxy (the Container Apps ingress, Caddy) in front of the
application, which listens on plain HTTP. Two mechanisms make the application build correct public URLs:

1. **Forwarded headers.** With `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` the application takes the client
   address from `X-Forwarded-For` and the scheme from `X-Forwarded-Proto`. That makes the scheme `https` for
   `StartPage` URLs and makes the login cookie `Secure`. **Only these two headers** are honoured:
   `X-Forwarded-Host` is ignored, so the proxy must pass the original `Host` header through unchanged. Caddy
   (`reverse_proxy`) and the Container Apps ingress both do. The setting trusts the headers from any
   sender (the known proxy networks are cleared), so it is only safe when nothing but the proxy can reach the
   application port. Never publish port 8080 to the internet; in the Compose stack it is not published at all.
2. **`Cxml__PublicBaseUrl`.** A fixed, externally visible base URL that overrides the request-derived one.

When do you need `Cxml__PublicBaseUrl`?

| Situation | Set it? |
|---|---|
| Proxy sets `X-Forwarded-Proto` and keeps the original `Host` (Caddy, Container Apps ingress, most proxies) | No. Optional, but pinning it is harmless. |
| A custom domain on Container Apps | Recommended: `https://cxml.contoso.com` |
| Several proxies in a chain, or one that rewrites `Host` | **Yes** |
| Non-standard external port (for example `https://cxml.lan:8443`) | **Yes**, include the port |
| The application served below a path prefix | **Yes**, include the prefix |
| The proxy does not send `X-Forwarded-Proto` | **Yes**, with `https://` |
| Direct access without a proxy (local runs) | No |

What it affects: the `StartPage` URL the buyer's browser is sent to, `ProfileResponse` URLs, the cXML
endpoint shown on the dashboard, and the `BrowserFormPost` URL in the sender templates. It does **not**
change which addresses the server answers on, and it does not affect cookies. A wrong value breaks
PunchOut: the buyer's browser opens a `StartPage` it cannot reach.

If you ever run the image directly on an untrusted network without a proxy, set
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=false`; otherwise clients can forge their address (which defeats the
per-address login rate limit) and the scheme.

## Admin login

- `Admin__Enabled=true` protects every admin page (dashboard, message log, partners, identities, catalog,
  scenarios, rules, sender, sessions, orders). Unauthenticated page requests are redirected with `302` to
  `/login?returnUrl=...`.
- **Always anonymous**: `POST /cxml` and `GET /cxml`, `/punchout/{token}` and its checkout and cancel posts,
  `/test-buyer/punchout-return`, `/health`, static files and the error pages. Buyer systems and their users
  keep working without a login.
- Two ways to sign in, configured independently: **one password account** (`Admin__Username` and the hash)
  and/or **any user of one Microsoft 365 tenant** (see [below](#microsoft-365-sign-in-entra-id)). When both are
  configured the login page offers both, and the password is the break-glass login. There is no user
  management, no password reset UI and no lockout; both login posts are rate limited to **5 attempts per
  minute per client address** (answer `429`).
- The session is a cookie named `cxml-dev-admin`: `HttpOnly`, `SameSite=Lax`, `Secure` when the request is
  HTTPS (as seen through forwarded headers), sliding expiry of `Admin__SessionHours` (default 8), not
  persistent (it ends with the browser session). **Sign out** is at the bottom of the sidebar.
- Return URLs after login are accepted only when they are paths on this site.
- Startup fails fast with a clear message when the login is enabled and no (valid) sign-in method is
  configured, so a misconfigured deployment can never run open by accident.

## Microsoft 365 sign-in (Entra ID)

Instead of (or next to) the shared password, administrators can sign in with their Microsoft 365 work account.
The application is a confidential client of Microsoft Entra ID (OpenID Connect, authorization code flow with
PKCE) for **one tenant**; whoever can sign in to that tenant and is allowed to use the app becomes an admin of
the whole admin UI (there are no roles). Buyer-facing endpoints are not affected.

Register the application once in the Entra admin center (<https://entra.microsoft.com>):

1. **Identity > Applications > App registrations > New registration.** Name it (for example "cXML Dev
   Server"); *Supported account types*: **Accounts in this organizational directory only (single tenant)**.
2. *Redirect URI*: platform **Web**, value `https://<your host>/signin-oidc` (the public URL of the app, as
   buyers reach it, for example `https://cxml.example.com/signin-oidc`). Add `http://localhost:<port>/signin-oidc`
   as a second redirect URI to try it locally. Register.
3. From the *Overview* page copy **Directory (tenant) ID** and **Application (client) ID**.
4. **Certificates & secrets > Client secrets > New client secret.** Copy the secret **value** at once (it is
   shown only once) and note its expiry date. Rotate it before it expires, otherwise the Microsoft sign-in
   stops working (the password login, if configured, keeps working).
5. No API permissions are needed beyond the default *Microsoft Graph > User.Read* (delegated), which covers
   `openid`, `profile` and `email`. The application calls no Graph API.
6. *Optional*: to allow only selected people, open **Identity > Applications > Enterprise applications >** your
   app **> Properties** and set **Assignment required? = Yes**, then assign users or groups under *Users and
   groups*. Without it, every user of the tenant can sign in.

Then give the application the three values (and keep the secret out of files and the repository):

| Platform | Where |
|---|---|
| Local | `Admin__Enabled=true Admin__Microsoft__TenantId=<guid> Admin__Microsoft__ClientId=<guid> Admin__Microsoft__ClientSecret=<secret> dotnet run --project src/CxmlDevServer.Web` |
| Self-hosted Compose | `ADMIN_MS_TENANT_ID`, `ADMIN_MS_CLIENT_ID`, `ADMIN_MS_CLIENT_SECRET` in `deploy/selfhost/.env`, then `docker compose up -d` |
| Azure Container Apps | GitHub variables `ADMIN_MS_TENANT_ID`, `ADMIN_MS_CLIENT_ID` and secret `ADMIN_MS_CLIENT_SECRET` (or the environment variables of the same name when deploying by hand), then deploy again |

How it behaves:

- The login page shows **Sign in with Microsoft** (and the password form too when a password is configured).
  Microsoft shows its account picker every time, so somebody who signed out can choose another account.
- Only accounts of the configured tenant are accepted: the token's issuer and its `tid` claim must match the
  tenant id, otherwise the user is sent back to the login page with "Microsoft sign-in failed or was
  cancelled". The reason is written to the server log, never shown in the browser.
- The result is the same `cxml-dev-admin` cookie as for the password login (same lifetime, same sign-out). The
  signed-in name in the sidebar is the user's sign-in name (`preferred_username`, usually the UPN).
- **Sign out** ends the session of this application only; it does not sign the user out of Microsoft.
- The redirect URI is built from `Cxml__PublicBaseUrl` when that is set (self-hosted Compose and custom
  domains set it), otherwise from the request. It must match the Entra registration exactly, including
  `https`. Behind a proxy keep forwarded headers on (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, already the
  default in the image), or the redirect URI would say `http://`. The browser must reach the application
  over `https` (or `http://localhost`): the sign-in cookies are `Secure`.
- Settings are validated at start-up: a tenant id that is not a GUID (including `common`, `organizations`,
  `consumers`), a missing client id or secret, or a non-`https` `Instance` stops the application with a message.
- Failed attempts: `AADSTS50011` (reply URL mismatch) means the redirect URI in Entra differs from
  `<public URL>/signin-oidc`; `AADSTS7000215` (invalid client secret) means the secret is wrong or expired;
  `AADSTS50105` means *Assignment required* is on and the user is not assigned.

## Data protection keys

The login cookie, antiforgery tokens and Blazor circuit state are protected with ASP.NET Core Data
Protection keys. They are stored in `DataProtection__KeysPath` (`/app/data/keys` in the image), inside the
data volume.

| Platform | Effect |
|---|---|
| Self-hosted | The keys live in the `cxml-data` volume and survive restarts and upgrades. Administrators stay signed in. They are not part of the SQLite backup; losing them only signs everybody out. |
| Azure Container Apps | The volume is ephemeral, so new keys are created at every start: administrators sign in again after every restart, and a login form open during a restart reports "The login form has expired". Harmless. |
| In-memory (`DataProtection__KeysPath` empty) | Every restart signs everybody out. |

Keys are tied to a fixed application name, not to a machine, so a restored data volume works on another host.
They are stored unencrypted in the folder (the framework logs a warning about it), so protect the data
volume like any other secret.

## Logging

The application logs to standard output (console format) at `Information`; the framework categories are at
`Warning`. Platform access: `docker compose logs` ([self-hosted](self-hosted.md#logs)),
`az containerapp logs show` and Log Analytics ([Azure](azure-container-apps.md#logs)). Set
`Logging__LogLevel__Default` to `Warning` to quiet a busy environment or to `Debug` for troubleshooting.
The cXML traffic itself (requests, responses, validation issues) is stored in the database and shown in the
**Message log**, not in the console log.

## New-environment checklist

Work through this in order after the first deployment succeeded. It assumes the admin login is on
(it should be) and the environment is empty (deployed with `SeedDemoPartners=false`). Details of the fields are in
[client-setup.md](../client-setup.md#create-your-own-trading-partner).

1. **Sign in.** Open the URL, sign in as the admin user. Note the **cXML endpoint** shown on the
   dashboard: it must be the `https://` address buyers will use. If it is wrong, fix
   [`Cxml__PublicBaseUrl`](#forwarded-headers-and-publicbaseurl) first.
2. **Supplier identities.** Open *Supplier identities*. This is the `To` credential that buyer
   documents address. Seeded: `DUNS` / `123456789` (default) and `DUNS` / `942888711` (the specification
   example). Add the supplier domain and identity your buyers will use, mark it as the default, and delete the
   seeded ones you do not need. The default identity is also what the server puts in `From` of the cart it
   returns.
3. **Trading partners.** Open *Trading partners*, *New*, once per buyer system or network:
   - *From* and *Sender* domain and identity exactly as the buyer system sends them (copy them from a first
     rejected request in the *Message log*, or ask the buyer team).
   - *Shared secret*: a strong random value, **unique per partner**. Generate it and never reuse one across
     partners or environments:

     ```bash
     openssl rand -base64 24
     ```

   - Leave *Authentication* on *Shared secret* (use *None (identify only)* only for a system that cannot send
     one).
   - Keep *Active* checked.
4. **Remove the demo partners.** If the environment was deployed with `SeedDemoPartners=true`, delete
   *Demo Buyer* (`devsecret`) and *Acme (spec example)* (`abracadabra`), or at least change their
   secrets, as soon as you have real partners: those secrets are public. Better: deploy with
   `SeedDemoPartners=false` so they never exist.
5. **Per partner behavior.** On each partner choose:
   - *Validation mode*: **Lenient** to start (accepts and records issues), **Strict** once the buyer's
     documents are clean (rejects with `406`/`409`/`412`), **Off** only to ignore validation.
   - *Post encoding*: `cXML-base64` (default) or `cXML-urlencoded`, whichever the buyer's return handler
     reads.
   - *operationAllowed*: what the returned `PunchOutOrderMessage` announces (`create`, `edit`, `inspect`,
     `source`). `edit` lets the buyer reopen the cart.
   - *Deployment mode*: `test` or `production` if you want a warning on a mismatch, else *Accept both*.
6. **Catalog.** Open *Catalog*: about 25 demo items are seeded. Edit or delete them, add the items the buyers
   should see in the PunchOut shop (part ID, price, unit of measure, UNSPSC).
7. **Scenarios.** Open *Response scenarios* and make sure none is enabled unintentionally. The seeded sample
   ("fail the first 3 OrderRequests with 500") is created disabled, and only when demo partners are seeded.
   An enabled scenario replaces real processing for matching requests.
8. **Unknown senders.** Confirm `Cxml__AllowUnknownPartners` is `false` unless you deliberately accept anyone
   (it removes the shared-secret protection).
9. **Verify with the Message sender.** Open *Message sender*, pick the new partner, template
   *ProfileRequest* and send it to this server: the response must show `Status code="200"` and appear in the
   *Message log*. Then use the template *PunchOutSetupRequest (create)* and check that the
   `<StartPage><URL>` in the response starts with the public `https://` URL, and open it to see the shop.
   Finally send the same request with a wrong shared secret and confirm a `401`.
10. **Hand the connection details to the buyer teams**: cXML endpoint (`https://<host>/cxml`), the supplier
    `To` domain and identity, their partner's `From`/`Sender` identity and shared secret (send the secret over
    a secure channel, not by e-mail or chat), the return encoding and cXML version. The
    [client-setup guide](../client-setup.md) walks buyers through Ariba-style, Coupa-style and generic
    systems and the common errors.
11. **Record what you configured.** On Azure the partners vanish on every restart (see
    [ephemeral data](azure-container-apps.md#ephemeral-data-and-what-to-do-about-it)), so keep the list of
    partners and secrets in your team's password manager. On self-hosted, check the next scheduled backup
    ran.

## Security hardening checklist

- [ ] `Admin__Enabled=true` with a strong, unique password (a password-manager value, 16+ characters), stored
      as a hash; rotated when people leave.
- [ ] `Cxml__SeedDemoPartners=false`; the demo partners (`devsecret`, `abracadabra`) do not exist.
- [ ] Every trading partner has a random shared secret, unique per partner and environment.
- [ ] `Cxml__AllowUnknownPartners=false`.
- [ ] HTTPS only: Caddy with a real certificate, or the Container Apps ingress with `allowInsecure: false`.
      Buyers and the admin never use plain HTTP.
- [ ] Port 8080 of the application is not reachable from outside (Compose publishes only Caddy; ACA exposes
      only the ingress). This is what makes the forwarded-headers trust safe.
- [ ] Network restriction where possible: ACA `ipAllowList`, or a firewall / VPN in front of the self-hosted
      host. Remember that the browsers of PunchOut users and the buyer's servers must be allowed.
- [ ] Image pinned to a version tag (`1.2.0`, `sha-abc1234`), upgraded deliberately; the host kept patched.
- [ ] Secrets only in secret stores: GitHub environment secrets, container app secrets, a `chmod 600` `.env`.
      Nothing secret in the repository, in `extraEnv`, in `main.bicepparam` or in `appsettings.json`.
- [ ] GitHub environment `production` with required reviewers and a restricted set of deployable
      branches/tags; the Azure identity holds only `Contributor` on one resource group.
- [ ] Treat administrators as privileged: the **Message log** stores the raw documents exactly as received
      (including the shared secret a buyer sent in its `Sender` credential) and the **Message sender**
      makes the server post to any URL you enter. Do not run production buyer traffic or real personal data
      through a dev server.
- [ ] Self-hosted: backups run, a copy leaves the host, and a restore has been rehearsed once.
- [ ] You know that the PunchOut shop (`/punchout/{token}`) and the test buyer
      (`/test-buyer/punchout-return`) are anonymous by design. The shop URL is protected by a 32-byte
      random token and a session lifetime (`Cxml__PunchOutSessionMinutes`); the test buyer accepts and
      displays any document posted to it.

## Example environment matrix

A typical set-up with three environments, using the options of this repository:

| | Developer machine | Shared test (Azure Container Apps) | Shared long-lived (self-hosted) |
|---|---|---|---|
| Purpose | Develop and run the tests | Disposable team environment, workshops, per-release checks | Standing environment with kept partners and history |
| How it runs | `dotnet run` | `deploy-aca.yml` on `v*` tags | `deploy/selfhost` compose stack |
| URL | `http://localhost:5058` | `https://<app>.<region>.azurecontainerapps.io` or a custom domain | `https://cxml.example.com` |
| `Admin__Enabled` | `false` | `true` | `true` |
| `Cxml__SeedDemoPartners` | `true` | `true` (set `false` unless behind an allow-list) | `true` (set `false` on a shared host) |
| `Cxml__AllowUnknownPartners` | `false` | `false` | `false` |
| `Cxml__PublicBaseUrl` | empty | empty, or the custom domain | `https://cxml.example.com` (default from `DOMAIN`) |
| Persistence | `data/cxml-dev.db` | none (ephemeral) | `cxml-data` volume plus daily backups |
| Access control | local only | `ipAllowList` of the company egress ranges | Firewall/VPN, or open with strong secrets |
| Image | built from source | `ghcr.io/mthpbs/cxml-dev-server:1.x.y` | pinned `IMAGE` tag |
| Upgrade | `git pull` | tag a release | `docker compose pull app caddy && docker compose up -d` |

Give each environment its own admin password and its own trading partner secrets, so that a leaked secret
from a throw-away environment says nothing about a standing one.
