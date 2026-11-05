// The cXML Dev Server container app: external HTTPS ingress, one replica, ephemeral EmptyDir storage.
// Deployed by main.bicep; see that file for the parameter documentation and the deployment commands.

@export()
@description('One IP range of the ingress allow-list.')
type allowedIp = {
  @description('Rule name: lowercase letters, digits and hyphens (for example "office").')
  name: string
  @description('IPv4/IPv6 address or CIDR range (for example "203.0.113.0/24").')
  cidr: string
}

@export()
@description('A custom domain bound to the app ingress. Declared here so that a deployment keeps a binding made with "az containerapp hostname bind".')
type customDomain = {
  @description('Host name, for example cxml.contoso.com.')
  name: string
  @description('Resource ID of the managed or uploaded certificate in the Container Apps environment. Empty: the host name is added without a certificate.')
  certificateId: string?
  @description('SniEnabled (default when certificateId is set) or Disabled (default without a certificate).')
  bindingType: ('SniEnabled' | 'Disabled')?
}

@export()
@description('An additional plain-value environment variable.')
type envVar = {
  @description('Variable name; use __ for configuration nesting (for example Cxml__DefaultCurrency).')
  name: string
  @description('Variable value.')
  value: string
}

@description('Name of the container app (also the first label of its URL).')
@minLength(2)
@maxLength(32)
param name string

@description('Azure region of the container app. Must be the region of the environment.')
param location string

@description('Tags applied to the container app.')
param tags object = {}

@description('Resource ID of the Container Apps environment that hosts the app.')
param environmentId string

@description('Workload profile of the environment to run in. Empty: do not set it (environments without workload profiles).')
param workloadProfileName string = ''

@description('Full image reference, for example ghcr.io/<owner>/cxml-dev-server:1.2.0.')
@minLength(3)
param containerImage string

@description('Registry host of the image (ghcr.io, <name>.azurecr.io). Empty: public image without registry credentials.')
param registryServer string = ''

@description('Registry user name. Used together with registryPassword.')
param registryUsername string = ''

@secure()
@description('Registry password or token (GHCR: a PAT with read:packages). Stored as the container app secret "registry-password".')
param registryPassword string = ''

@description('Resource ID of a user-assigned managed identity that may pull from the registry (ACR: role AcrPull). Takes precedence over username/password.')
param registryIdentityResourceId string = ''

@description('CPU cores per replica.')
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

@description('Memory per replica. On the Consumption profile it must be twice the CPU in Gi (0.25/0.5Gi, 0.5/1Gi, 1.0/2Gi, 2.0/4Gi, ...).')
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

@description('Turn the admin login on (Admin__Enabled).')
param adminEnabled bool = true

@description('Admin user name (Admin__Username).')
@minLength(1)
param adminUsername string = 'admin'

@secure()
@description('Admin password hash (Admin__PasswordHash), created with: docker run --rm -it <image> hash-password. Stored as the container app secret "admin-password-hash".')
param adminPasswordHash string = ''

@description('Externally visible base URL (Cxml__PublicBaseUrl), for example https://cxml.contoso.com. Empty: derived from the request (correct for the default *.azurecontainerapps.io URL).')
param publicBaseUrl string = ''

@description('Process documents from unknown senders with a warning instead of answering 401 (Cxml__AllowUnknownPartners).')
param allowUnknownPartners bool = false

@description('Create the demo trading partners with well-known secrets on a fresh database (Cxml__SeedDemoPartners). Use false on shared, internet-reachable deployments.')
param seedDemoPartners bool = true

@description('Ingress allow-list. Empty: reachable from everywhere. Otherwise only these ranges can reach the app (all others get 403).')
param ipAllowList allowedIp[] = []

@description('Additional environment variables.')
param extraEnv envVar[] = []

@description('Custom domains of the ingress. Empty: none. Note that a deployment replaces the ingress configuration, so list every domain you want to keep here.')
param customDomains customDomain[] = []

// Validation hook: main.bicep passes a non-empty value only when the admin login is off or a hash was supplied. The
// length constraint makes the deployment fail early, with this parameter name in the error, instead of creating an app
// that refuses to start. (Bicep's assert statement is still experimental, so a parameter constraint is used.)
@description('Set by main.bicep. Empty means adminEnabled=true without adminPasswordHash, which is rejected.')
@minLength(1)
#disable-next-line no-unused-params
param adminCredentialIsSetOrLoginDisabled string

var useRegistryIdentity = !empty(registryServer) && !empty(registryIdentityResourceId)
var useRegistryPassword = !useRegistryIdentity && !empty(registryServer) && !empty(registryUsername) && !empty(registryPassword)
var useAdminHash = adminEnabled && !empty(adminPasswordHash)

// Secrets exist only when a value was supplied; an empty secret would be rejected by the platform.
var registrySecrets = useRegistryPassword ? [
  {
    name: 'registry-password'
    value: registryPassword
  }
] : []
var adminSecrets = useAdminHash ? [
  {
    name: 'admin-password-hash'
    value: adminPasswordHash
  }
] : []

var registries = useRegistryIdentity ? [
  {
    server: registryServer
    identity: registryIdentityResourceId
  }
] : useRegistryPassword ? [
  {
    server: registryServer
    username: registryUsername
    passwordSecretRef: 'registry-password'
  }
] : []

var baseEnv = [
  // Trust X-Forwarded-For/-Proto from the ingress, so that StartPage URLs are https://.
  { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
  { name: 'ConnectionStrings__Default', value: 'Data Source=/app/data/cxml-dev.db' }
  // Login cookie / antiforgery keys live next to the database: they survive a container restart only as far as the data does.
  { name: 'DataProtection__KeysPath', value: '/app/data/keys' }
  { name: 'Admin__Enabled', value: adminEnabled ? 'true' : 'false' }
  { name: 'Admin__Username', value: adminUsername }
  { name: 'Cxml__AllowUnknownPartners', value: allowUnknownPartners ? 'true' : 'false' }
  { name: 'Cxml__SeedDemoPartners', value: seedDemoPartners ? 'true' : 'false' }
]
var adminHashEnv = useAdminHash ? [
  { name: 'Admin__PasswordHash', secretRef: 'admin-password-hash' }
] : []
var publicBaseUrlEnv = empty(publicBaseUrl) ? [] : [
  { name: 'Cxml__PublicBaseUrl', value: publicBaseUrl }
]
var env = concat(baseEnv, adminHashEnv, publicBaseUrlEnv, extraEnv)

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: useRegistryIdentity ? {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${registryIdentityResourceId}': {}
    }
  } : {
    type: 'None'
  }
  properties: {
    managedEnvironmentId: environmentId
    workloadProfileName: empty(workloadProfileName) ? null : workloadProfileName
    configuration: {
      // One active revision: a new deployment replaces the old one (no traffic splitting).
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto' // HTTP/1.1 + WebSockets (the Blazor admin UI needs WebSockets) and HTTP/2
        allowInsecure: false // plain HTTP is redirected to HTTPS by the ingress
        customDomains: [for domain in customDomains: {
          name: domain.name
          certificateId: domain.?certificateId
          bindingType: domain.?bindingType ?? (empty(domain.?certificateId ?? '') ? 'Disabled' : 'SniEnabled')
        }]
        ipSecurityRestrictions: [for rule in ipAllowList: {
          name: rule.name
          description: 'Allowed by the ipAllowList parameter'
          ipAddressRange: rule.cidr
          action: 'Allow'
        }]
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: registries
      secrets: concat(registrySecrets, adminSecrets)
    }
    template: {
      containers: [
        {
          name: 'cxml-dev-server'
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: env
          volumeMounts: [
            {
              volumeName: 'data'
              mountPath: '/app/data'
            }
          ]
          probes: [
            {
              // Gives the app time to migrate and seed the SQLite database; liveness/readiness start only after this passes.
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 2
              periodSeconds: 5
              timeoutSeconds: 3
              failureThreshold: 10
            }
            {
              // Restarts the container when it stops answering.
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              // Takes the replica out of rotation while it does not answer.
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 10
              timeoutSeconds: 5
              failureThreshold: 3
              successThreshold: 1
            }
          ]
        }
      ]
      // Exactly one replica, always. Two reasons:
      //  1. The SQLite database lives on an EmptyDir volume, which belongs to a single replica: a second replica would
      //     have its own, different, empty database (and would not share partners, sessions or orders).
      //  2. Blazor Server keeps the UI state of every browser tab in a circuit in the memory of one replica; scaling
      //     out or to zero would break open admin sessions and PunchOut shop sessions.
      // Consequences: no scale-to-zero (the app is always billed), and every restart or new revision (each deployment)
      // starts with an empty database that is re-seeded from configuration.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'data'
          // EmptyDir: lives as long as the replica. Data is lost when the container restarts, is moved or a new revision is created.
          storageType: 'EmptyDir'
        }
      ]
    }
  }
}

@description('Name of the container app.')
output name string = app.name

@description('Fully qualified domain name of the ingress.')
output fqdn string = app.properties.configuration.ingress.fqdn

@description('Name of the latest revision.')
output latestRevisionName string = app.properties.latestRevisionName
