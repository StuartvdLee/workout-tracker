targetScope = 'resourceGroup'

@description('administratorLogin for PostgreSQL server')
@secure()
param postgresqlAdministratorLogin string

@description('administratorLoginPassword for PostgreSQL server')
@secure()
param postgresqlAdministratorLoginPassword string

var location = resourceGroup().location
var appName = 'workouttracker'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: '${appName}cr'
  location: location
  sku: {
    name: 'Basic'
  }
}

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: '${appName}-cae'
  location: location
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

module apiContainerApp 'modules/containerApp.bicep' = {
  name: '${appName}-api-ca-module'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    containerAppName: '${appName}-api-ca'
  }
}

module webContainerApp 'modules/containerApp.bicep' = {
  name: '${appName}-web-ca-module'
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
    network: {
      publicNetworkAccess: 'Enabled'
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
