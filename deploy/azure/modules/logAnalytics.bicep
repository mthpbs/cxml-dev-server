// Log Analytics workspace that receives the console/system logs of the Container Apps environment.
// Used by main.bicep only when no existing environment is supplied.

@description('Name of the Log Analytics workspace.')
@minLength(4)
@maxLength(63)
param name string

@description('Azure region of the workspace.')
param location string

@description('Tags applied to the workspace.')
param tags object = {}

@description('Days to keep logs (30-730; 30 is the lowest value that is free of extra retention charges).')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
  }
}

@description('Resource ID of the workspace.')
output id string = workspace.id

@description('Name of the workspace.')
output name string = workspace.name
