targetScope = 'resourceGroup'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Name of the application')
param appName string = 'workouttracker'

@description('administratorLogin for PostgreSQL server')
@secure()
param postgresqlAdministratorLogin string

@description('administratorLoginPassword for PostgreSQL server')
@secure()
param postgresqlAdministratorLoginPassword string

// @description('Entra ID application (client) ID for Easy Auth on the Web app.')
// param aadClientId string

// @description('Entra ID application client secret for Easy Auth.')
// @secure()
// param aadClientSecret string

// @description('Entra ID tenant ID. Restricts login to users in this tenant.')
// param aadTenantId string

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: '${appName}-cae'
  location: location
  properties: {
    publicNetworkAccess: 'Disabled'
  }
}

module apiContainerApp 'modules/containerApp.bicep' = {
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    containerAppName: '${appName}-api-ca'
    ingressTrafficAllow: false
  }
}

module webContainerApp 'modules/containerApp.bicep' = {
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    containerAppName: '${appName}-web-ca'
    ingressTrafficAllow: true
  }
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' = {
  name: '${appName}-psql'
  location: 'northeurope'
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: postgresqlAdministratorLogin
    administratorLoginPassword: postgresqlAdministratorLoginPassword
    version: '17'
    storage: {
      storageSizeGB: 32
      autoGrow: 'Enabled'
      tier: 'P4'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2025-08-01' = {
  parent: postgresServer
  name: '${appName}_db'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.UTF8'
  }
}
