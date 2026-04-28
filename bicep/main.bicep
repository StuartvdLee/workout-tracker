targetScope = 'resourceGroup'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Azure Container Registry name (globally unique, alphanumeric only, no hyphens — e.g. crworkouttracker).')
@minLength(5)
@maxLength(50)
param containerRegistryName string = 'crworkouttracker'

@description('PostgreSQL administrator login name.')
param postgresAdminLogin string = 'wtadmin'

@description('PostgreSQL administrator password.')
@secure()
param postgresAdminPassword string

@description('Docker image tag for the API service.')
param apiImageTag string = 'latest'

@description('Docker image tag for the Web service.')
param webImageTag string = 'latest'

@description('Entra ID application (client) ID for Easy Auth on the Web app.')
param aadClientId string

@description('Entra ID application client secret for Easy Auth.')
@secure()
param aadClientSecret string

@description('Entra ID tenant ID. Restricts login to users in this tenant.')
param aadTenantId string

module registry 'modules/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    registryName: containerRegistryName
  }
}

module containerAppsEnv 'modules/containerAppsEnv.bicep' = {
  name: 'containerAppsEnv'
  params: {
    location: location
  }
}

module database 'modules/postgres.bicep' = {
  name: 'database'
  params: {
    location: location
    adminLogin: postgresAdminLogin
    adminPassword: postgresAdminPassword
  }
}

module apiApp 'modules/api.bicep' = {
  name: 'apiApp'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
    registryServer: registry.outputs.loginServer
    registryUsername: registry.outputs.adminUsername
    registryPassword: registry.outputs.adminPassword
    imageTag: apiImageTag
    postgresHost: database.outputs.serverFqdn
    postgresDatabaseName: database.outputs.databaseName
    postgresUsername: database.outputs.adminLogin
    postgresPassword: postgresAdminPassword
  }
}

module webApp 'modules/web.bicep' = {
  name: 'webApp'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
    registryServer: registry.outputs.loginServer
    registryUsername: registry.outputs.adminUsername
    registryPassword: registry.outputs.adminPassword
    imageTag: webImageTag
    apiInternalUrl: 'https://${apiApp.outputs.internalFqdn}'
    aadClientId: aadClientId
    aadClientSecret: aadClientSecret
    aadTenantId: aadTenantId
  }
}

@description('Public URL of the Web Container App.')
output webAppUrl string = webApp.outputs.url

@description('FQDN to register as the redirect URI in the Entra ID App Registration.')
output aadRedirectUri string = 'https://${webApp.outputs.fqdn}/.auth/login/aad/callback'

@description('PostgreSQL server FQDN, used by the migration CI step.')
output postgresServerFqdn string = database.outputs.serverFqdn

@description('ACR login server for use in CI/CD.')
output registryLoginServer string = registry.outputs.loginServer
