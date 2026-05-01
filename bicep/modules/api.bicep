@description('Azure region for the Container App.')
param location string

@description('Resource ID of the Container Apps Environment.')
param containerAppsEnvironmentId string

@description('PostgreSQL server fully-qualified domain name.')
param postgresHost string

@description('PostgreSQL database name.')
param postgresDatabaseName string

@description('PostgreSQL administrator login name.')
param postgresUsername string

@description('PostgreSQL administrator password.')
@secure()
param postgresPassword string

// Connection string constructed here so it never surfaces as a Bicep output
var connectionString = 'Host=${postgresHost};Port=5432;Database=${postgresDatabaseName};Username=${postgresUsername};Password=${postgresPassword};SSL Mode=Require;Trust Server Certificate=true'

// Placeholder image used until Docker images are built and pushed to ACR.
var placeholderImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-workouttracker-api'
  location: location
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      ingress: {
        external: false   // internal only – not reachable from the internet
        targetPort: 8080
        transport: 'http'
      }
      secrets: [
        {
          name: 'postgres-connection-string'
          value: connectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: placeholderImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              // Aspire Npgsql reads ConnectionStrings__workout-tracker-db
              name: 'ConnectionStrings__workout-tracker-db'
              secretRef: 'postgres-connection-string'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              tcpSocket: {
                port: 8080
              }
              initialDelaySeconds: 30
              periodSeconds: 15
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              tcpSocket: {
                port: 8080
              }
              initialDelaySeconds: 15
              periodSeconds: 10
              failureThreshold: 5
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output internalFqdn string = apiApp.properties.configuration.ingress.fqdn
