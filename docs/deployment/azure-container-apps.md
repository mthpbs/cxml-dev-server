# Runbook: Azure Container Apps

This runbook deploys the cXML Dev Server to **Azure Container Apps** with the Bicep template in
[`deploy/azure`](../../deploy/azure), either from your shell or through the GitHub Actions workflow
[`deploy-aca.yml`](../../.github/workflows/deploy-aca.yml). It covers the one-time setup, the first
deployment, verification, day-2 operations and teardown.

> **Read this first: the database is ephemeral.** The template keeps SQLite on an `EmptyDir` volume and runs
> exactly one replica. Every restart, every new revision and therefore every deployment starts with an
> empty database: trading partners, identities, catalog edits, scenarios, the message log, orders and
> PunchOut sessions are gone. See [Ephemeral data](#ephemeral-data-and-what-to-do-about-it). If that is not
> acceptable, use the [self-hosted stack](self-hosted.md).

> **Untested against a live subscription.** The Bicep files are compiled and linted in CI, but this
> repository's CI cannot deploy to Azure. The `az` and `gh` commands below follow the Azure CLI and GitHub CLI
> documentation and were checked against the files in this repository. Flags change between CLI versions:
> run any command with `--help` if it complains, and treat the sections marked *verify* as the first place to
> look when something differs.

- [What gets created](#what-gets-created)
- [Prerequisites](#prerequisites)
- [One-time setup](#one-time-setup)
- [Admin password hash](#admin-password-hash)
- [First deployment from the CLI](#first-deployment-from-the-cli)
- [Deployment through GitHub Actions](#deployment-through-github-actions)
- [Verification checklist](#verification-checklist)
- [Ephemeral data and what to do about it](#ephemeral-data-and-what-to-do-about-it)
- [Restricting access with an IP allow-list](#restricting-access-with-an-ip-allow-list)
- [Custom domain with a managed certificate](#custom-domain-with-a-managed-certificate)
- [Day-2 operations](#day-2-operations)
- [Troubleshooting](#troubleshooting)
- [Teardown](#teardown)

## What gets created

`deploy/azure/main.bicep` is deployed at **resource group scope**. The resource group itself must already
exist. By default it creates:

| Resource | Notes |
|---|---|
| Log Analytics workspace `<appName>-logs` | Receives the console and system logs. 30 days retention (`logRetentionDays`, 30-730). |
| Container Apps environment `<appName>-env` | Consumption-only (no dedicated workload profile). |
| Container app `cxml-dev-server` | External HTTPS ingress to port 8080, `activeRevisionsMode: Single`, **min and max replicas fixed at 1**, `EmptyDir` mounted at `/app/data`, startup, liveness and readiness probes on `/health`. 0.5 vCPU and 1 GiB by default. |

Pass `EXISTING_ENVIRONMENT_ID` to deploy into a Container Apps environment you already have; then no
workspace or environment is created. The template creates **no container registry**: the image must already
be in GHCR, an existing Azure Container Registry or another registry.

Template outputs: `fqdn`, `url`, `cxmlEndpoint`, `environmentId`, `containerAppName`, `latestRevisionName`.

Parameters are read from environment variables by
[`main.bicepparam`](../../deploy/azure/main.bicepparam) (`readEnvironmentVariable()`), so secrets never
appear in a file or on a command line:

| Environment variable | Bicep parameter | Default | Purpose |
|---|---|---|---|
| `CONTAINER_IMAGE` | `containerImage` | `ghcr.io/mthpbs/cxml-dev-server:latest` | Full image reference. Use an immutable tag. |
| `ADMIN_PASSWORD_HASH` | `adminPasswordHash` | empty | Required while `adminEnabled` is true; stored as a container app secret. |
| `REGISTRY_SERVER` | `registryServer` | `ghcr.io` | Registry host. |
| `REGISTRY_USERNAME` | `registryUsername` | empty | Registry user, only for a private image. |
| `REGISTRY_PASSWORD` | `registryPassword` | empty | Registry token (GHCR: a personal access token with `read:packages`), only for a private image. Stored as a secret. |
| `AZURE_LOCATION` | `location` | empty (region of the resource group) | Azure region of the resources. |
| `PUBLIC_BASE_URL` | `publicBaseUrl` | empty | Externally visible URL when a custom domain or another proxy is in front. |
| `EXISTING_ENVIRONMENT_ID` | `existingEnvironmentResourceId` | empty (create one) | Resource ID of an existing Container Apps environment. |
| `CUSTOM_DOMAINS_JSON` | `customDomains` | `[]` | JSON array of custom domains to keep bound, `[{"name":"...","certificateId":"..."}]`. |

All other parameters have defaults in `main.bicep` and are set in `main.bicepparam` (edit that file in your
fork or branch): `appName` (`cxml-dev-server`), `cpu` (`0.5`), `memory` (`1Gi`), `adminEnabled` (`true`),
`adminUsername` (`admin`), `allowUnknownPartners` (`false`), `seedDemoPartners` (`false` in
`main.bicepparam`; `true` is the default of `main.bicep` itself), `ipAllowList` (empty = open),
`extraEnv`, `tags`, `registryIdentityResourceId`, `workloadProfileName`, `environmentName`,
`logAnalyticsWorkspaceName`, `logRetentionDays`.

The template always sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`,
`ConnectionStrings__Default=Data Source=/app/data/cxml-dev.db` and
`DataProtection__KeysPath=/app/data/keys` on the container.

## Prerequisites

| Requirement | Detail |
|---|---|
| Azure subscription | You need rights to create a resource group and a role assignment on it: **Owner** or **User Access Administrator** (plus Contributor) on the subscription or the resource group. |
| Microsoft Entra permission | Ability to create an app registration (the default for members of most tenants, otherwise *Application Developer*). Only needed for the GitHub workflow. |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | A recent version. The Bicep tooling comes with it: run `az bicep upgrade` to get the current Bicep CLI. `main.bicep` uses user-defined types and `import`, which need a current Bicep (it was developed against Bicep 0.48). |
| Docker | To create the admin password hash (any machine with Docker; see below). Alternatively use the .NET SDK. |
| GitHub CLI (`gh`) | Optional, for the workflow setup commands below. The same can be done in the GitHub web UI. |
| An image in a registry | Published by the workflow on the first run (GHCR), or push your own. |

Check your tools:

```bash
az version
```

```bash
az bicep upgrade
```

## One-time setup

Skip the Entra and GitHub parts if you will only ever deploy from your shell (see
[First deployment from the CLI](#first-deployment-from-the-cli)); you still need steps 1 to 3.

All commands run in a Bash shell. Set the variables once per shell session and replace the example values:

```bash
export SUBSCRIPTION_ID="<your-subscription-id>"
export RESOURCE_GROUP="rg-cxml-dev"
export LOCATION="westeurope"
export GITHUB_REPO="mthpbs/cxml-dev-server"   # owner/name of your repository or fork
export GITHUB_ENVIRONMENT="production"        # the GitHub environment the workflow deploys to
```

### 1. Sign in and select the subscription

```bash
az login
```

```bash
az account set --subscription "$SUBSCRIPTION_ID"
```

### 2. Register the resource providers

A new subscription may not have the providers that Container Apps needs. Registration is a one-time step
per subscription and takes a few minutes:

```bash
az provider register --namespace Microsoft.App --wait
```

```bash
az provider register --namespace Microsoft.OperationalInsights --wait
```

### 3. Create the resource group

The template deploys into an existing resource group.

```bash
az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
```

### 4. Create the Entra app for GitHub (OIDC, no stored password)

The workflow signs in with OpenID Connect: GitHub presents a short-lived token and Azure trusts it because
of a **federated credential** on an app registration. No client secret is created or stored.

```bash
APP_ID="$(az ad app create --display-name "cxml-dev-server-github" --query appId --output tsv)"; echo "$APP_ID"
```

```bash
az ad sp create --id "$APP_ID"
```

```bash
SP_OBJECT_ID="$(az ad sp show --id "$APP_ID" --query id --output tsv)"; echo "$SP_OBJECT_ID"
```

Create the federated credential. The `subject` must match **exactly** (case-sensitive) what GitHub puts in
the token. Because the deploy job declares `environment:`, the subject is always the environment form, for
manual runs and for `v*` tag pushes alike:

```json
{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:mthpbs/cxml-dev-server:environment:production",
  "description": "Deploy workflow of the production environment",
  "audiences": ["api://AzureADTokenExchange"]
}
```

Save that as `credential.json` (change `mthpbs/cxml-dev-server` and `production` to your repository and
environment name), then:

```bash
az ad app federated-credential create --id "$APP_ID" --parameters credential.json
```

Every additional GitHub environment you deploy to (for example `staging`) needs its own federated
credential with `environment:staging` in the subject. A credential for a branch or tag
(`repo:...:ref:refs/tags/v1.0.0`) is **not** needed: the environment subject takes precedence when a job
uses an environment.

### 5. Allow the app to deploy into the resource group (least privilege)

`Contributor` scoped to the resource group is enough: the template creates a workspace, an environment and
a container app, and creates no role assignments.

```bash
az role assignment create --assignee-object-id "$SP_OBJECT_ID" --assignee-principal-type ServicePrincipal --role Contributor --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP"
```

Note the tenant ID for the GitHub secrets:

```bash
az account show --query tenantId --output tsv
```

### 6. Registry access to the image

The workflow pushes to `ghcr.io/<owner>/cxml-dev-server` (owner in lowercase). A GHCR package starts out
**private**. Choose one:

- **Make the package public** (simplest; the image contains no secrets, only the open-source application):
  GitHub, your profile or organization, *Packages*, `cxml-dev-server`, *Package settings*, *Change
  visibility*, *Public*. Do this after the first image has been pushed. No pull token is needed.
- **Keep it private and give Azure a pull token**: create a *personal access token (classic)* with only the
  `read:packages` scope (GHCR does not accept fine-grained tokens), and store it as the GitHub secret
  `GHCR_PULL_TOKEN` (below). The workflow passes the repository owner as the registry user name and the token
  as the password; the template stores the token as the container app secret `registry-password`. If the
  repository belongs to an organization and the pull is rejected as unauthorized, verify the token with
  `docker login ghcr.io` first; for CLI deployments you control `REGISTRY_USERNAME` yourself.
- **Azure Container Registry with managed identity**: set `registryServer` to `<name>.azurecr.io` and
  `registryIdentityResourceId` in `main.bicepparam` to a user-assigned managed identity that holds the
  `AcrPull` role on the registry. The identity takes precedence over a username and password. Creating the
  identity and the role assignment is outside this template.

### 7. Configure GitHub

The workflow reads its settings from a GitHub **environment** (here `production`). An environment lets you
scope secrets, require approvals and restrict which branches or tags may deploy. Create it, then add
secrets and variables to it:

```bash
gh api --method PUT "repos/$GITHUB_REPO/environments/$GITHUB_ENVIRONMENT"
```

Secrets (the Azure identifiers are not confidential, but the workflow reads them as secrets):

```bash
gh secret set AZURE_CLIENT_ID --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body "$APP_ID"
```

```bash
gh secret set AZURE_TENANT_ID --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body "<your-tenant-id>"
```

```bash
gh secret set AZURE_SUBSCRIPTION_ID --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body "$SUBSCRIPTION_ID"
```

The admin password hash (see [Admin password hash](#admin-password-hash) for how to create it):

```bash
gh secret set ADMIN_PASSWORD_HASH --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body "$ADMIN_PASSWORD_HASH"
```

Only for a private GHCR package (`gh` prompts for the token, so it stays out of your shell history):

```bash
gh secret set GHCR_PULL_TOKEN --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT"
```

Variables:

```bash
gh variable set AZURE_RESOURCE_GROUP --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body "$RESOURCE_GROUP"
```

Optional variables: `AZURE_LOCATION` (region of the resources; empty means the region of the resource
group), `AZURE_PUBLIC_BASE_URL` (custom domain URL, see below), `AZURE_EXISTING_ENVIRONMENT_ID` and
`AZURE_CUSTOM_DOMAINS_JSON` (bound custom domains, see below).

```bash
gh variable set AZURE_LOCATION --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body "$LOCATION"
```

| Name | Kind | Required | Used for |
|---|---|---|---|
| `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | secret | yes | `azure/login` with OIDC |
| `ADMIN_PASSWORD_HASH` | secret | yes | `Admin__PasswordHash` |
| `GHCR_PULL_TOKEN` | secret | only for a private package | Registry password in the container app |
| `AZURE_RESOURCE_GROUP` | variable | yes | Target resource group |
| `AZURE_LOCATION` | variable | no | `location` parameter |
| `AZURE_PUBLIC_BASE_URL` | variable | no | `publicBaseUrl` parameter |
| `AZURE_EXISTING_ENVIRONMENT_ID` | variable | no | `existingEnvironmentResourceId` parameter |
| `AZURE_CUSTOM_DOMAINS_JSON` | variable | no | `customDomains` parameter (empty means `[]`) |

Recommended on the GitHub side (*Settings*, *Environments*, `production`): add **required reviewers** so a
pushed `v*` tag waits for approval, and limit *Deployment branches and tags* to the tags you release from.

## Admin password hash

The admin login stores no password, only a hash in the ASP.NET Core Identity format. Create it with the
image you are going to deploy (or with the .NET SDK from a checkout). All variants print the hash on
standard output and nothing else.

Interactive, no echo and nothing in your shell history (`-it` is required so the container can prompt):

```bash
docker run --rm -it ghcr.io/mthpbs/cxml-dev-server:latest hash-password
```

Into a shell variable, without the password ever being a command-line argument:

```bash
read -rs -p 'Admin password: ' PW; echo; ADMIN_PASSWORD_HASH="$(printf '%s' "$PW" | docker run --rm -i ghcr.io/mthpbs/cxml-dev-server:latest hash-password)"; unset PW; export ADMIN_PASSWORD_HASH
```

From a source checkout, without Docker:

```bash
dotnet run --project src/CxmlDevServer.Web --no-launch-profile -- hash-password
```

You can also pass the password as an argument (`hash-password 'my password'`), but then it lands in your
shell history and the process list. Use a long random password and keep it in a password manager:

```bash
openssl rand -base64 24
```

The image does not exist in GHCR before the first workflow run. For the very first hash either build the
image locally (`docker build -t cxml-dev-server .`) or use the `dotnet run` form above; the hash is the same
format everywhere.

## First deployment from the CLI

Use this to check the setup before involving GitHub, or as your normal way to deploy. It needs steps 1 to 3
above; the Entra app and the GitHub settings are only for the workflow.

Set the inputs. `main.bicepparam` reads them from the environment:

```bash
export CONTAINER_IMAGE="ghcr.io/mthpbs/cxml-dev-server:v1.0.0"
```

The hash is the variable you created in [Admin password hash](#admin-password-hash) (`ADMIN_PASSWORD_HASH`).
Only for a private image:

```bash
export REGISTRY_USERNAME="<github-user>"; export REGISTRY_PASSWORD="<token-with-read-packages>"
```

Optional: `export AZURE_LOCATION=...`, `export PUBLIC_BASE_URL=...`, `export EXISTING_ENVIRONMENT_ID=...`.

Preview what would change (a what-if needs no extra permissions beyond Contributor):

```bash
az deployment group what-if --resource-group "$RESOURCE_GROUP" --parameters deploy/azure/main.bicepparam
```

Deploy. Note: there is **no `--template-file`**; the `using './main.bicep'` line inside
`main.bicepparam` names the template.

```bash
az deployment group create --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --parameters deploy/azure/main.bicepparam --query properties.outputs.url.value --output tsv
```

The command prints the app URL (for example `https://cxml-dev-server.<random>.westeurope.azurecontainerapps.io`)
when the deployment succeeded. All outputs:

```bash
az deployment group show --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --query properties.outputs
```

*Verify:* some `az` versions accept extra inline `key=value` parameters next to a `.bicepparam` file and some
reject the combination. Environment variables work everywhere, which is why this template uses them. If
`readEnvironmentVariable` complains that a variable is not set, check you exported it in the **same** shell.

## Deployment through GitHub Actions

[`deploy-aca.yml`](../../.github/workflows/deploy-aca.yml) has two jobs:

1. **Build and push image**: builds the Dockerfile and pushes
   `ghcr.io/<owner>/cxml-dev-server:<tag>` using the built-in `GITHUB_TOKEN`. Tags: the git tag (`v1.2.0`),
   `sha-<7 characters>` and, for tags and the default branch, `latest`.
2. **Deploy**: signs in to Azure with OIDC, runs `az deployment group create` against
   `deploy/azure/main.bicepparam` with the image from job 1, then smoke-tests `GET /health` and `GET /cxml`
   on the new URL (10 retries, 10 seconds apart).

Triggers:

| Trigger | Environment | Image |
|---|---|---|
| Push of a tag `v*` | `production` | Built from the tagged commit |
| Manual run (*Actions*, *Deploy to Azure Container Apps*, *Run workflow*) | The `environment` input (default `production`) | Built from the selected ref as `sha-xxxxxxx`, or, if you fill in `image_tag`, **no build**: that existing tag is deployed (this is the rollback path) |

Release a version:

```bash
git tag v1.0.0 && git push origin v1.0.0
```

Or run it by hand:

```bash
gh workflow run deploy-aca.yml --repo "$GITHUB_REPO" --ref main -f environment=production
```

```bash
gh run watch --repo "$GITHUB_REPO"
```

A workflow with a `workflow_dispatch` trigger must exist on the repository's default branch before GitHub
lists it for manual runs. Runs of one environment never overlap, and a running deployment is not cancelled.

The deployment job summary lists the URL, the cXML endpoint and the image. Remember that **each run replaces
the revision and resets the data**.

## Verification checklist

Set the URL (from the deployment output or the job summary):

```bash
APP_URL="$(az containerapp show --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --query "properties.configuration.ingress.fqdn" --output tsv)"; APP_URL="https://$APP_URL"; echo "$APP_URL"
```

Work through the list:

1. **The revision is running and healthy**

   ```bash
   az containerapp revision list --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --query "[].{name:name,active:properties.active,health:properties.healthState,running:properties.runningState}" --output table
   ```

   Expect `Healthy` (the field names can differ between CLI versions; the portal's *Revisions and replicas*
   blade shows the same).

2. **Health endpoint** answers `OK`:

   ```bash
   curl -fsS "$APP_URL/health"
   ```

3. **cXML endpoint** answers the ping with `Status code="200"`:

   ```bash
   curl -fsS "$APP_URL/cxml"
   ```

4. **HTTP is redirected to HTTPS** (`allowInsecure: false`); expect a `301` or `302` with an `https://` `location`:

   ```bash
   curl -sI "http://${APP_URL#https://}/health" | head -n 3
   ```

5. **The admin UI is protected**: expect `302` with `location: /login?returnUrl=%2F`, and `200` for the login page:

   ```bash
   curl -sI "$APP_URL/" | head -n 3
   ```

   ```bash
   curl -s -o /dev/null -w '%{http_code}\n' "$APP_URL/login"
   ```

6. **No demo credentials** (when `seedDemoPartners` is `false`): the specification's example order must be
   refused with `Status code="401"` because no partner exists yet. Run it from the repository root:

   ```bash
   curl -s -X POST "$APP_URL/cxml" -H 'Content-Type: text/xml' --data-binary @cXML-spec/Examples/0-OrderRequest.xml
   ```

   If this returns `Status code="200"`, demo partners were seeded (`Cxml__SeedDemoPartners` is `true`).

7. **Sign in** at `$APP_URL/` with the admin user name (`admin` unless you changed `adminUsername`) and the
   password behind your hash. Open **Trading partners**: it is empty, or shows the two demo partners when they
   are seeded.

8. **StartPage URLs are `https://`**: create a partner (see the
   [new-environment checklist](configuration.md#new-environment-checklist)), then in **Message sender**
   choose the *PunchOutSetupRequest (create)* template, send it to this server and read
   `<StartPage><URL>` in the response: it must start with your `https://` URL. If it shows `http://` see
   [Troubleshooting](#troubleshooting).

9. **Logs flow** (see [logs](#logs)): you see the startup lines
   `Data Protection keys are stored in /app/data/keys.` and the seeding message.

## Ephemeral data and what to do about it

What happens to the data:

| Event | Data |
|---|---|
| Container crash or liveness probe failure | lost (the replica is replaced, the `EmptyDir` goes with it) |
| `az containerapp revision restart` | lost |
| Platform maintenance that moves the replica | lost |
| Any deployment that changes the template, image or non-secret settings (new revision) | lost |
| Browser tab left open during any of the above | the Blazor connection is dropped; reload the page and sign in again |

What is *not* lost, because it lives in Azure: the app configuration (image, environment variables,
secrets), the URL, and the Log Analytics workspace with the **console logs** (not the in-app message log).

On every start of an empty database the server applies its migrations and seeds the **supplier identities**
(`DUNS 123456789` as default and `DUNS 942888711`) and about 25 **catalog items**. With
`Cxml__SeedDemoPartners=true` it also seeds the two demo trading partners (`devsecret`, `abracadabra`) and a
disabled sample scenario. That seeding is the only configuration that survives, because it comes from the
application and its settings.

The admin login survives because it comes from environment variables and a secret, not from the database.
The login cookie does not: Data Protection keys live in `/app/data/keys` on the same `EmptyDir`, so after
a restart administrators must sign in again, and an open login form shows "The login form has expired".
That is harmless: reload and sign in.

Options, from simplest to most involved:

1. **Treat the instance as disposable and accept the manual step.** Deploy rarely (tags, not every commit),
   and after each restart re-create the trading partners in the admin UI. A partner needs five or six fields;
   keep them in your team's password manager or wiki. The
   [new-environment checklist](configuration.md#new-environment-checklist) lists the steps.
2. **Use demo partners only.** Set `seedDemoPartners = true` in `main.bicepparam` so that the two demo
   partners exist after every start. Only do this when the environment is not reachable by strangers, because
   their secrets are public: combine it with an [IP allow-list](#restricting-access-with-an-ip-allow-list).
3. **Accept unknown senders.** `allowUnknownPartners = true` lets any sender through with a warning and the
   default validation mode (`Lenient`; change it with `extraEnv`, name `Cxml__DefaultValidationMode`). Buyer
   teams then need no partner entry and no shared secret, which also means anyone who can reach the URL can
   post documents. Again, pair it with an allow-list.
4. **There is no scripted restore.** The Message Sender only posts cXML documents, and no cXML transaction
   creates a trading partner; the admin UI has no import or export either. Partners can only be re-entered
   by hand in this version.
5. **Move to persistent storage.** The ACA template deliberately does not mount Azure Files: SQLite in WAL
   mode needs real file locking and shared memory, which network file shares such as Azure Files (SMB/NFS)
   do not provide reliably, so a database file on such a share risks corruption. The application only
   supports SQLite today, so a persistent Azure setup would need a code change (a server database) or a
   VM with a managed disk running the [self-hosted stack](self-hosted.md). If the data must persist, use
   the self-hosted stack.

The template runs exactly **one replica** (min 1, max 1) for the same reason: each replica would have its
own different, empty database, and Blazor Server keeps every browser tab's state in memory on one replica.
That also means no scale-to-zero: the replica is always running (and billed).

## Restricting access with an IP allow-list

By default the ingress is open to the internet. In `deploy/azure/main.bicepparam` set:

```bicep
param ipAllowList = [
  { name: 'office', cidr: '203.0.113.0/24' }
  { name: 'vpn', cidr: '198.51.100.7/32' }
]
```

then redeploy. Rule names are lowercase letters, digits and hyphens. With at least one rule, **every other
address gets `403`**. That includes:

- the buyer systems that post to `/cxml`, and the browsers of the people who open the PunchOut shop: add
  their egress addresses, or the integration cannot work;
- the **GitHub-hosted runner** that executes the workflow's smoke test. The deployment itself succeeds, but
  the smoke test step then fails with a `403` and the run turns red. Either accept the red run and verify
  from an allowed address, or remove the smoke test step from your copy of the workflow.

## Custom domain with a managed certificate

You bind a custom domain with the CLI after the first deployment, then record it in the deployment
parameters (`customDomains`) so that later deployments keep it, and tell the app its public URL.

1. **Collect the values** for your DNS records:

   ```bash
   az containerapp show --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --query "{fqdn:properties.configuration.ingress.fqdn, verificationId:properties.customDomainVerificationId}"
   ```

2. **Create DNS records** at your DNS provider for the host name you want (here `cxml.contoso.com`):

   | Type | Name | Value |
   |---|---|---|
   | `CNAME` | `cxml` | the app `fqdn` from step 1 |
   | `TXT` | `asuid.cxml` | the `verificationId` from step 1 |

   Wait until they resolve (`dig +short CNAME cxml.contoso.com`).

3. **Add the host name** to the app:

   ```bash
   az containerapp hostname add --hostname cxml.contoso.com --name cxml-dev-server --resource-group "$RESOURCE_GROUP"
   ```

4. **Bind it with a free managed certificate.** Use `CNAME` validation for a sub-domain such as
   `cxml.contoso.com`; use `HTTP` or `TXT` for an apex domain such as `contoso.com` (see
   `az containerapp hostname bind --help`):

   ```bash
   az containerapp hostname bind --hostname cxml.contoso.com --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --environment cxml-dev-server-env --validation-method CNAME
   ```

   `--environment` is the name of the Container Apps environment (`cxml-dev-server-env` by default).
   Issuing the certificate takes a few minutes.

5. **Record the binding in the template parameters.** A Bicep deployment replaces the ingress configuration,
   so a domain that is not listed in `customDomains` would be unbound by the next deployment. Find the
   managed certificate that step 4 created and note its resource ID:

   ```bash
   az containerapp env certificate list --name cxml-dev-server-env --resource-group "$RESOURCE_GROUP" --managed-certificates-only --query "[].{name:name,subject:properties.subjectName,id:id}" --output table
   ```

   Then export (CLI deployments) or store as the GitHub variable `AZURE_CUSTOM_DOMAINS_JSON` (workflow) a JSON
   array with every bound domain; the default is `[]`. `bindingType` defaults to `SniEnabled` when a
   `certificateId` is given:

   ```bash
   export CUSTOM_DOMAINS_JSON='[{"name":"cxml.contoso.com","certificateId":"<managed-certificate-resource-id>"}]'
   ```

   ```bash
   gh variable set AZURE_CUSTOM_DOMAINS_JSON --repo "$GITHUB_REPO" --env "$GITHUB_ENVIRONMENT" --body '[{"name":"cxml.contoso.com","certificateId":"<managed-certificate-resource-id>"}]'
   ```

   *Verify:* flags of `az containerapp env certificate list` differ between CLI versions; the portal's
   *Certificates* blade of the environment shows the same resource ID.

6. **Tell the app its public URL.** Behind a custom domain the ingress still preserves the `Host` header, so
   the URLs would already be right; setting `publicBaseUrl` pins them so that the `*.azurecontainerapps.io`
   address cannot leak into StartPage URLs. Export it and redeploy:

   ```bash
   export PUBLIC_BASE_URL="https://cxml.contoso.com"
   ```

   Redeploy by CLI as in [First deployment](#first-deployment-from-the-cli), or set the repository/environment
   variable `AZURE_PUBLIC_BASE_URL` and run the workflow.

7. **Verify** with `curl -fsS https://cxml.contoso.com/health`, and again after the next deployment to
   confirm the binding survived (`az containerapp hostname list --name cxml-dev-server --resource-group "$RESOURCE_GROUP"`).

## Day-2 operations

Replace `cxml-dev-server` with your `appName` if you changed it. Older CLI versions need
`az extension add --name containerapp --upgrade` before the `az containerapp` commands.

### Logs

Live console output (the application writes its logs to stdout):

```bash
az containerapp logs show --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --type console --follow --tail 100
```

System events of the platform (image pulls, probe failures, restarts):

```bash
az containerapp logs show --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --type system --tail 100
```

History in Log Analytics (30 days by default). First find the workspace ID, then query the
`ContainerAppConsoleLogs_CL` table:

```bash
WORKSPACE_ID="$(az monitor log-analytics workspace show --resource-group "$RESOURCE_GROUP" --workspace-name cxml-dev-server-logs --query customerId --output tsv)"
```

The `az monitor log-analytics query` command may offer to install the `log-analytics` extension on first use.

```bash
az monitor log-analytics query --workspace "$WORKSPACE_ID" --analytics-query "ContainerAppConsoleLogs_CL | where ContainerAppName_s == 'cxml-dev-server' | order by TimeGenerated desc | project TimeGenerated, RevisionName_s, Log_s | take 100" --output table
```

The same query pasted into the portal (*Log Analytics workspace*, *Logs*):

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "cxml-dev-server"
| order by TimeGenerated desc
| project TimeGenerated, RevisionName_s, Log_s
| take 100
```

System logs are in `ContainerAppSystemLogs_CL`. Table and column names depend on the environment's log
format; if the query reports an unknown table, look at the table list in the *Logs* blade. The in-app
**Message log** is the primary tool for cXML problems, but it disappears on restart: export a problematic
exchange (message detail page) before restarting.

### Restart

A restart **resets the data**. Find the revision name, then restart it:

```bash
az containerapp revision list --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --query "[?properties.active].name" --output tsv
```

```bash
az containerapp revision restart --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --revision "<revision-name>"
```

### Update configuration

Settings that are not secret live in `main.bicepparam` (and the `extraEnv` list). Edit the file in the
repository, commit and redeploy (workflow or CLI). Bicep is the source of truth: a hand edit with
`az containerapp update --set-env-vars ...` works for an experiment but is overwritten by the next
deployment. Any change creates a new revision and resets the data.

### Rotate the admin password

Create a new hash (see [Admin password hash](#admin-password-hash)), then either:

- **through the pipeline**: update the GitHub secret `ADMIN_PASSWORD_HASH` and run the workflow again; or
- **in place**: update the container app secret and restart the revision so the new value is read:

  ```bash
  az containerapp secret set --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --secrets "admin-password-hash=$ADMIN_PASSWORD_HASH"
  ```

  ```bash
  az containerapp revision restart --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --revision "<revision-name>"
  ```

  (Change the GitHub secret too, or the next deployment brings the old password back.)

Both paths restart the app, so the data resets and every session is signed out.

### Roll back

Deploy a previous image tag. Through the workflow, run it manually with the `image_tag` input (no build
happens):

```bash
gh workflow run deploy-aca.yml --repo "$GITHUB_REPO" --ref main -f environment=production -f image_tag=v1.0.0
```

From the CLI, point `CONTAINER_IMAGE` at the older tag and repeat the
[deployment command](#first-deployment-from-the-cli). The data is empty after the rollback as after any
deployment, so no database migration problem can occur.

### Update the image

Release a new tag (`git tag v1.1.0 && git push origin v1.1.0`) or run the workflow manually. Prefer
immutable tags over `latest`: redeploying an unchanged template (the same `latest` reference) does not
change the revision template, so Azure may not create a new revision or pull the image again.

### Scale and size

- Leave replicas at 1 (see [above](#ephemeral-data-and-what-to-do-about-it)). Do not set `maxReplicas > 1`.
- CPU and memory are the `cpu` and `memory` parameters (default 0.5 vCPU and 1 GiB). The Consumption profile
  only accepts matching pairs: 0.25/0.5Gi, 0.5/1Gi, 0.75/1.5Gi, 1.0/2Gi, and so on up to 4.0/8Gi (memory is
  twice the CPU in GiB). The default is plenty for dozens of buyers.
- Besides the replica, log ingestion is the other running cost. For a quiet environment lower the log level
  to `Warning` through `extraEnv` (name `Logging__LogLevel__Default`); the template does not allow less than
  30 days of retention (`logRetentionDays`).

## Troubleshooting

| Symptom | Likely cause and fix |
|---|---|
| PunchOut `StartPage` URL starts with `http://` | The forwarded-headers setting is missing, or the request reached the app without `X-Forwarded-Proto`. Check the container env: `az containerapp show --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --query "properties.template.containers[0].env[?name=='ASPNETCORE_FORWARDEDHEADERS_ENABLED']"` must show `true`. Or set `PUBLIC_BASE_URL` to the `https://` address. |
| `StartPage` URL has the wrong host (the `azurecontainerapps.io` name after you added a custom domain) | Set `PUBLIC_BASE_URL` (see [custom domain](#custom-domain-with-a-managed-certificate)). |
| `Status code="401"` from `/cxml` right after a deployment or restart | The data was reset and the trading partner no longer exists. Re-create it in the admin UI (see [Ephemeral data](#ephemeral-data-and-what-to-do-about-it)). Look at the message in the **Message log** to see which sender it carried. |
| Login says "The login form has expired" or returns to the form | The Data Protection keys were regenerated by a restart while the form was open. Reload `/login` and sign in again. |
| Login answers `429` | More than 5 login attempts per minute from one address. Wait a minute. |
| Admin UI redirects to `/login` in a loop | Cookies blocked, or a `Host`/scheme mismatch between the browser URL and a proxy in front. Use the app URL directly; check that no other proxy rewrites `Host`. |
| `curl` returns `403` | The [IP allow-list](#restricting-access-with-an-ip-allow-list) does not contain your address. |
| Revision `Failed` or restarts in a loop; logs show `OptionsValidationException ... Admin:Enabled is true but neither Admin:PasswordHash nor Admin:Password is set` or `not a valid ASP.NET Core Identity password hash` | The hash is empty or damaged (for example a truncated paste). Create it again and redeploy. The template itself rejects an empty hash before creating anything when `adminEnabled` is true. |
| Startup probe keeps failing | Check the console log for the real error. The probe allows about 50 seconds (10 attempts, 5 seconds apart) for migration and seeding. A changed `ASPNETCORE_URLS` (not 8080) also fails the probes. |
| System log: image pull failed, `unauthorized` or `manifest unknown` | The tag does not exist (check `ghcr.io/<owner>/cxml-dev-server` tags), or the package is private and `GHCR_PULL_TOKEN` is missing, expired or lacks `read:packages`. Make the package public or fix the token. The registry owner in the image reference must be lowercase. |
| Deployment error `MissingSubscriptionRegistration` | Register the providers (step 2 of the setup). |
| Deployment error about CPU and memory | The pair is not a valid Consumption combination; see [Scale and size](#scale-and-size). |
| Deployment error about the environment's location | `AZURE_LOCATION` differs from the region of `EXISTING_ENVIRONMENT_ID`. Use the same region or leave `AZURE_LOCATION` empty. |
| Deployment error `adminCredentialIsSetOrLoginDisabled` / `MinLength` | `adminEnabled` is true and `ADMIN_PASSWORD_HASH` is empty. Export it or set the GitHub secret. |
| Errors about `readEnvironmentVariable`, `import` or unknown syntax in a Bicep file | The Bicep CLI is too old (`az bicep upgrade`), or the variable is not exported in this shell. |
| `AADSTS70021: No matching federated identity record found` in `azure/login` | The federated credential subject does not match. It must read `repo:<owner>/<repo>:environment:<environment name>` exactly, including case. The job must use that environment. |
| `AuthorizationFailed` during the deployment | The service principal lacks `Contributor` on the resource group (step 5), or the resource group name in `AZURE_RESOURCE_GROUP` is wrong. |
| Workflow smoke test fails but the deployment succeeded | The new revision needed longer than the retries, the IP allow-list returns `403` to runners, or the app is unhealthy: see the logs. |
| Custom domain stops working after a deployment | It is missing from `CUSTOM_DOMAINS_JSON` / `AZURE_CUSTOM_DOMAINS_JSON`, so the deployment unbound it. Add it (step 5 of [Custom domain](#custom-domain-with-a-managed-certificate)) and redeploy, or run `az containerapp hostname bind` again. |
| Browser shows the Blazor "Reconnecting" overlay repeatedly | The replica restarted (data reset) or WebSockets are blocked by a corporate proxy between the browser and Azure. Reload; check the system log for restarts. |

## Teardown

Delete everything the template created by deleting the resource group (this also deletes any other
resource you placed in it):

```bash
az group delete --name "$RESOURCE_GROUP" --yes --no-wait
```

Remove the Entra app and with it the federated credential and role assignment:

```bash
az ad app delete --id "$APP_ID"
```

Remove the GitHub environment (and its secrets and variables):

```bash
gh api --method DELETE "repos/$GITHUB_REPO/environments/$GITHUB_ENVIRONMENT"
```

Container images stay in GHCR until you delete the package versions. If you only want to stop the cost but
keep the setup, delete just the container app and re-run the deployment later:

```bash
az containerapp delete --name cxml-dev-server --resource-group "$RESOURCE_GROUP" --yes
```
