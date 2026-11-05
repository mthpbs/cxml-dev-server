// Container Apps managed environment (Consumption-only) that sends its logs to a Log Analytics workspace.
// Used by main.bicep only when no existing environment is supplied.

@description('Name of the Container Apps environment.')
@minLength(2)
@maxLength(32)
param name string

@description('Azure region of the environment.')
param location string

@description('Tags applied to the environment.')
param tags object = {}

@description('Name of the Log Analytics workspace (in this resource group) that receives the logs.')
param logAnalyticsWorkspaceName string

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
    // Consumption-only environment: no dedicated workload profile, billed per use.
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: false
  }
}

@description('Resource ID of the environment.')
output id string = environment.id

@description('Name of the environment.')
output name string = environment.name

@description('Default domain of the environment (apps get <app>.<defaultDomain>).')
output defaultDomain string = environment.properties.defaultDomain
