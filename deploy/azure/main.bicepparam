// Example parameters for main.bicep. Copy and adjust; do not put secrets in this file.
//
// Values that differ per deployment, and all secrets, come from environment variables (readEnvironmentVariable):
//   CONTAINER_IMAGE       full image reference, e.g. ghcr.io/<owner>/cxml-dev-server:1.2.0
//   ADMIN_PASSWORD_HASH   output of: docker run --rm -it <image> hash-password          (required while adminEnabled = true)
//   REGISTRY_SERVER       default ghcr.io                                  REGISTRY_USERNAME / REGISTRY_PASSWORD   private images only
//   AZURE_LOCATION        region (empty: region of the resource group)
//   PUBLIC_BASE_URL       custom-domain URL, e.g. https://cxml.contoso.com (empty: derived from the request)
//   CUSTOM_DOMAINS_JSON   JSON array of bound custom domains, e.g. [{"name":"cxml.contoso.com","certificateId":"<resource ID>"}] (default [])
//   EXISTING_ENVIRONMENT_ID  resource ID of an existing Container Apps environment (empty: create one)
//
//   az deployment group create --resource-group <rg> --parameters deploy/azure/main.bicepparam
//
// To test this file: ADMIN_PASSWORD_HASH=x az bicep build-params --file deploy/azure/main.bicepparam --stdout

using 'main.bicep'

param appName = 'cxml-dev-server'
param location = readEnvironmentVariable('AZURE_LOCATION', '')

// Required. Prefer an immutable tag (a version or commit SHA) over "latest".
param containerImage = readEnvironmentVariable('CONTAINER_IMAGE', 'ghcr.io/mthpbs/cxml-dev-server:latest')

// Registry. Public image: leave the server empty (or keep ghcr.io with empty username/password for a public GHCR package).
param registryServer = readEnvironmentVariable('REGISTRY_SERVER', 'ghcr.io')
param registryUsername = readEnvironmentVariable('REGISTRY_USERNAME', '')
param registryPassword = readEnvironmentVariable('REGISTRY_PASSWORD', '')
// Azure Container Registry with a user-assigned managed identity (role AcrPull) instead of a password:
// param registryIdentityResourceId = '/subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.ManagedIdentity/userAssignedIdentities/<name>'

// Empty: create a Log Analytics workspace and a new Consumption-only environment.
// Existing environment (EXISTING_ENVIRONMENT_ID): '/subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.App/managedEnvironments/<name>'
param existingEnvironmentResourceId = readEnvironmentVariable('EXISTING_ENVIRONMENT_ID', '')

param cpu = '0.5'
param memory = '1Gi'

// Admin login. The hash is REQUIRED while adminEnabled is true.
param adminEnabled = true
param adminUsername = 'admin'
param adminPasswordHash = readEnvironmentVariable('ADMIN_PASSWORD_HASH', '')

// Set when a custom domain is bound to the app, for example 'https://cxml.contoso.com'.
param publicBaseUrl = readEnvironmentVariable('PUBLIC_BASE_URL', '')

param allowUnknownPartners = false
// true: seed the demo partners (devsecret, abracadabra) in a fresh database. Set false for shared or
// internet-reachable deployments: the demo partners have well-known secrets.
param seedDemoPartners = true

// Restrict who can reach the app (empty = open). Buyer systems must be inside these ranges.
param ipAllowList = []
// param ipAllowList = [
//   { name: 'office', cidr: '203.0.113.0/24' }
// ]

// Custom domains bound with "az containerapp hostname bind": keep them across deployments (see the Azure runbook).
param customDomains = json(readEnvironmentVariable('CUSTOM_DOMAINS_JSON', '[]'))

// param extraEnv = [
//   { name: 'Cxml__DefaultCurrency', value: 'EUR' }
// ]

param tags = {
  app: 'cxml-dev-server'
}
