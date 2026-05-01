targetScope = 'resourceGroup'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

// @description('Azure Container Registry name (globally unique, alphanumeric only, no hyphens — e.g. crworkouttracker).')
// @minLength(5)
// @maxLength(50)
// param containerRegistryName string = 'workouttracker-cr'

// @description('PostgreSQL administrator login name.')
// param postgresAdminLogin string = 'wtadmin'

// @description('PostgreSQL administrator password.')
// @secure()
// param postgresAdminPassword string

// @description('Entra ID application (client) ID for Easy Auth on the Web app.')
// param aadClientId string

// @description('Entra ID application client secret for Easy Auth.')
// @secure()
// param aadClientSecret string

// @description('Entra ID tenant ID. Restricts login to users in this tenant.')
// param aadTenantId string

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: 'workouttracker-cae'
  location: location
  properties: {
    publicNetworkAccess: 'Disabled'
  }
}

module apiContainerApp 'modules/containerApp.bicep' = {
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    containerAppName: 'workouttracker-api-ca'
    ingressTrafficAllow: false
  }
}

module webContainerApp 'modules/containerApp.bicep' = {
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    containerAppName: 'workouttracker-web-ca'
    ingressTrafficAllow: true
  }
}

// module database 'modules/postgres.bicep' = {
//   name: 'database'
//   params: {
//     location: location
//     adminLogin: postgresAdminLogin
//     adminPassword: postgresAdminPassword
//   }
// }
