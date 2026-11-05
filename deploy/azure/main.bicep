// cXML Dev Server on Azure Container Apps.
//
// Creates (resource group scope):
//   - a Log Analytics workspace and a Consumption-only Container Apps environment, unless existingEnvironmentResourceId is set
//   - the container app: external HTTPS ingress, one replica, EmptyDir (ephemeral) storage for the SQLite database
// It creates no container registry: the image must already be in a registry (GHCR, an existing ACR, Docker Hub).
//
// Usage (current az CLI with the Bicep tooling; the resource group must exist). Secrets and the image are read from environment variables by
// main.bicepparam (readEnvironmentVariable), so they never appear in a file or on a command line:
//   export CONTAINER_IMAGE=ghcr.io/<owner>/cxml-dev-server:<tag>
//   read -rs -p 'Admin password: ' PW; echo
//   export ADMIN_PASSWORD_HASH="$(printf '%s' "$PW" | docker run --rm -i "$CONTAINER_IMAGE" hash-password)"; unset PW
//   export REGISTRY_USERNAME=<github user> REGISTRY_PASSWORD=<token with read:packages>    # only for a private image
//   az deployment group create --resource-group <rg> --parameters deploy/azure/main.bicepparam
// (The other parameters are edited in main.bicepparam. Some az versions also accept extra inline "key=value" arguments
// next to a .bicepparam file; environment variables work with every version.)
//
// Requirement: with adminEnabled=true (the default) adminPasswordHash must be set. The deployment is rejected before the
// container app is created when it is empty; and the application itself refuses to start without a hash.
//
// Every restart and every new revision (= every deployment) resets the database: SQLite is ephemeral here by design.

targetScope = 'resourceGroup'

import { allowedIp, envVar } from 'modules/containerApp.bicep'

// ---------------------------------------------------------------- general

@description('Name of the container app; the app is reachable at https://<appName>.<environment default domain>. Lowercase letters, digits and hyphens.')
@minLength(2)
@maxLength(32)
param appName string = 'cxml-dev-server'

@description('Azure region of all resources. Empty: the region of the resource group.')
param location string = ''

@description('Tags applied to all resources.')
param tags object = {}

// ---------------------------------------------------------------- image and registry

@description('Full image reference, for example ghcr.io/<owner>/cxml-dev-server:1.2.0. Prefer an immutable tag over "latest".')
@minLength(3)
param containerImage string

@description('Registry host of the image (ghcr.io, <name>.azurecr.io). Empty: the image is public and no registry credentials are configured.')
param registryServer string = ''

@description('Registry user name (GHCR: the GitHub user or organization). Empty together with an empty password: anonymous pull.')
param registryUsername string = ''

@secure()
@description('Registry password or token (GHCR: personal access token with read:packages). Stored as a container app secret. Empty: anonymous pull.')
param registryPassword string = ''

@description('Resource ID of a user-assigned managed identity that holds AcrPull on the registry. Alternative to username/password for Azure Container Registry; takes precedence when set.')
param registryIdentityResourceId string = ''

// ---------------------------------------------------------------- environment

@description('Resource ID of an existing Container Apps environment to deploy into (it must be in the same region as "location"). Empty: create a Log Analytics workspace and a new Consumption-only environment.')
param existingEnvironmentResourceId string = ''

@description('Workload profile to run in when using an existing environment. Empty: not set (right for environments without workload profiles). Ignored for a new environment, which uses "Consumption".')
param workloadProfileName string = ''

@description('Name of the new Container Apps environment (at most 32 characters). Ignored with an existing environment.')
@minLength(2)
@maxLength(32)
param environmentName string = take('${appName}-env', 32)

@description('Name of the new Log Analytics workspace. Ignored with an existing environment.')
@minLength(4)
@maxLength(63)
param logAnalyticsWorkspaceName string = '${appName}-logs'

@description('Days of log retention in the new workspace (30-730). Ignored with an existing environment.')
@minValue(30)
@maxValue(730)
param logRetentionDays int = 30

// ---------------------------------------------------------------- sizing

@description('CPU cores of the (single) replica. Together with memory it must be a valid Consumption combination: 0.25/0.5Gi, 0.5/1Gi, 0.75/1.5Gi, 1.0/2Gi, 1.25/2.5Gi, 1.5/3Gi, 1.75/3.5Gi, 2.0/4Gi, and so on up to 4.0/8Gi (memory = 2 x CPU in Gi).')
@allowed([
  '0.25'
  '0.5'
  '0.75'
  '1.0'
  '1.25'
  '1.5'
  '1.75'
  '2.0'
  '2.25'
  '2.5'
  '2.75'
  '3.0'
  '3.25'
  '3.5'
  '3.75'
  '4.0'
])
param cpu string = '0.5'

@description('Memory of the (single) replica; see cpu for the valid combinations.')
@allowed([
  '0.5Gi'
  '1Gi'
  '1.5Gi'
  '2Gi'
  '2.5Gi'
  '3Gi'
  '3.5Gi'
  '4Gi'
  '4.5Gi'
  '5Gi'
  '5.5Gi'
  '6Gi'
  '6.5Gi'
  '7Gi'
  '7.5Gi'
  '8Gi'
])
param memory string = '1Gi'

// ---------------------------------------------------------------- application settings

@description('Require a login for the admin UI (Admin__Enabled). The PunchOut endpoints, the shop, the test buyer and /health stay anonymous. Strongly recommended for an internet-reachable app.')
param adminEnabled bool = true

@description('User name of the admin account (Admin__Username).')
@minLength(1)
param adminUsername string = 'admin'

@secure()
@description('Password hash of the admin account (Admin__PasswordHash). REQUIRED when adminEnabled is true. Create it with: docker run --rm -it <image> hash-password. Stored as a container app secret.')
param adminPasswordHash string = ''

@description('Externally visible base URL (Cxml__PublicBaseUrl), for example https://cxml.contoso.com. Set it when a custom domain or another proxy sits in front. Empty: derived from the request, which is correct for the default azurecontainerapps.io URL.')
param publicBaseUrl string = ''

@description('Process documents from senders that match no trading partner, with a warning, instead of answering 401 (Cxml__AllowUnknownPartners).')
param allowUnknownPartners bool = false

@description('Create the demo trading partners (well-known secrets such as "devsecret") and a sample scenario in a fresh database (Cxml__SeedDemoPartners). Set false on shared, internet-reachable deployments and create real partners in the admin UI.')
param seedDemoPartners bool = true

@description('Ingress allow-list, for example [{ name: \'office\', cidr: \'203.0.113.0/24\' }]. Empty: open to the internet. When set, every other address gets 403 - including buyer systems and the GitHub-hosted runners of the smoke test.')
param ipAllowList allowedIp[] = []

@description('Additional plain-value environment variables, for example [{ name: \'Cxml__DefaultCurrency\', value: \'EUR\' }]. Do not put secrets here.')
param extraEnv envVar[] = []

// ---------------------------------------------------------------- resources

var createEnvironment = empty(existingEnvironmentResourceId)
var effectiveLocation = empty(location) ? resourceGroup().location : location

module logs 'modules/logAnalytics.bicep' = if (createEnvironment) {
  name: 'logAnalytics'
  params: {
    name: logAnalyticsWorkspaceName
    location: effectiveLocation
    tags: tags
    retentionInDays: logRetentionDays
  }
}

module environment 'modules/containerAppsEnvironment.bicep' = if (createEnvironment) {
  name: 'containerAppsEnvironment'
  params: {
    name: environmentName
    location: effectiveLocation
    tags: tags
    logAnalyticsWorkspaceName: logs!.outputs.name
  }
}

var effectiveEnvironmentId = createEnvironment ? environment!.outputs.id : existingEnvironmentResourceId

module app 'modules/containerApp.bicep' = {
  name: 'containerApp'
  params: {
    name: appName
    location: effectiveLocation
    tags: tags
    environmentId: effectiveEnvironmentId
    workloadProfileName: createEnvironment ? 'Consumption' : workloadProfileName
    containerImage: containerImage
    registryServer: registryServer
    registryUsername: registryUsername
    registryPassword: registryPassword
    registryIdentityResourceId: registryIdentityResourceId
    cpu: cpu
    memory: memory
    adminEnabled: adminEnabled
    adminUsername: adminUsername
    adminPasswordHash: adminPasswordHash
    publicBaseUrl: publicBaseUrl
    allowUnknownPartners: allowUnknownPartners
    seedDemoPartners: seedDemoPartners
    ipAllowList: ipAllowList
    extraEnv: extraEnv
    // Bicep's assert is experimental. The module parameter below has @minLength(1), so adminEnabled=true with an empty
    // adminPasswordHash makes the deployment fail early with an error naming the parameter.
    adminCredentialIsSetOrLoginDisabled: (!adminEnabled || !empty(adminPasswordHash)) ? 'ok' : ''
  }
}

// ---------------------------------------------------------------- outputs

@description('Fully qualified domain name of the app.')
output fqdn string = app.outputs.fqdn

@description('Base URL of the app (admin UI, PunchOut shop). Use it as the Cxml__PublicBaseUrl value when you need to set one.')
output url string = 'https://${app.outputs.fqdn}'

@description('URL that buyer systems post cXML requests to.')
output cxmlEndpoint string = 'https://${app.outputs.fqdn}/cxml'

@description('Resource ID of the Container Apps environment that hosts the app.')
output environmentId string = effectiveEnvironmentId

@description('Name of the container app (for az containerapp logs/restart/revision commands).')
output containerAppName string = app.outputs.name

@description('Name of the latest revision.')
output latestRevisionName string = app.outputs.latestRevisionName
