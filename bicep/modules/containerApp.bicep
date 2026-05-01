@description('Azure region for the Container App.')
param location string

@description('Resource ID of the Container Apps Environment.')
param containerAppsEnvironmentId string

@description('Name of the Container App.')
param containerAppName string

@description('Whether to allow ingress traffic to the Container App.')
param ingressTrafficAllow bool = false

@description('Target port for ingress traffic.')
param targetPort int = 80

@description('Container image to deploy.')
param containerImage string = 'mcr.microsoft.com/k8se/quickstart:latest'

@description('Name of the container.')
param containerName string = 'app'

resource containerApp 'Microsoft.App/containerApps@2026-01-01' = {
  name: containerAppName
  location: location
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: ingressTrafficAllow
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
        stickySessions: {
          affinity: 'none'
        }
      }
    }
    template: {
      containers: [
        {
          name: containerName
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
