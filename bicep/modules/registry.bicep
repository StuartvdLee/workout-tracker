@description('Azure region for the registry.')
param location string

@description('Globally unique name for the Container Registry (alphanumeric, 5–50 chars).')
param registryName string

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

output loginServer string = registry.properties.loginServer
output adminUsername string = registry.listCredentials().username
output adminPassword string = registry.listCredentials().passwords[0].value
