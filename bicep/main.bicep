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

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: 'workouttrackercr'
  location: location
  sku: {
    name: 'Basic'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// // Shared managed identity used by Container Apps to pull images from ACR.
// // Avoids the need for ACR admin credentials entirely.
// resource containerAppsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
//   name: 'workouttracker-id'
//   location: location
// }

// // Grant AcrPull on the registry to the managed identity.
// var acrPullRoleDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

// resource registryRef 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
//   name: containerRegistryName
// }

// resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
//   scope: registryRef
//   name: guid(registryRef.id, containerAppsIdentity.id, acrPullRoleDefinitionId)
//   properties: {
//     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleDefinitionId)
//     principalId: containerAppsIdentity.properties.principalId
//     principalType: 'ServicePrincipal'
//   }
// }

// module containerAppsEnv 'modules/containerAppsEnv.bicep' = {
//   name: 'containerAppsEnv'
//   params: {
//     location: location
//   }
// }

// module database 'modules/postgres.bicep' = {
//   name: 'database'
//   params: {
//     location: location
//     adminLogin: postgresAdminLogin
//     adminPassword: postgresAdminPassword
//   }
// }

// module apiApp 'modules/api.bicep' = {
//   name: 'apiApp'
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
//     postgresHost: database.outputs.serverFqdn
//     postgresDatabaseName: database.outputs.databaseName
//     postgresUsername: database.outputs.adminLogin
//     postgresPassword: postgresAdminPassword
//     registryLoginServer: registry.outputs.loginServer
//     managedIdentityId: containerAppsIdentity.id
//   }
// }

// module webApp 'modules/web.bicep' = {
//   name: 'webApp'
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
//     apiInternalUrl: 'https://${apiApp.outputs.internalFqdn}'
//     aadClientId: aadClientId
//     aadClientSecret: aadClientSecret
//     aadTenantId: aadTenantId
//     registryLoginServer: registry.outputs.loginServer
//     managedIdentityId: containerAppsIdentity.id
//   }
// }

// @description('Public URL of the Web Container App.')
// output webAppUrl string = webApp.outputs.url

// @description('FQDN to register as the redirect URI in the Entra ID App Registration.')
// output aadRedirectUri string = 'https://${webApp.outputs.fqdn}/.auth/login/aad/callback'

// @description('PostgreSQL server FQDN, used by the migration CI step.')
// output postgresServerFqdn string = database.outputs.serverFqdn

// @description('ACR login server for use in CI/CD.')
// output registryLoginServer string = registry.outputs.loginServer
