@description('Azure region for the Container App.')
param location string

@description('Resource ID of the Container Apps Environment.')
param containerAppsEnvironmentId string

@description('Container Registry login server (e.g. myregistry.azurecr.io).')
param registryServer string

@description('Container Registry admin username.')
param registryUsername string

@description('Container Registry admin password.')
@secure()
param registryPassword string

@description('Docker image tag to deploy.')
param imageTag string

@description('Internal HTTPS URL for the API Container App (Aspire service discovery).')
param apiInternalUrl string

@description('Entra ID application (client) ID for Easy Auth.')
param aadClientId string

@description('Entra ID application client secret for Easy Auth.')
@secure()
param aadClientSecret string

@description('Entra ID tenant ID to restrict access to.')
param aadTenantId string

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'web'
  location: location
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      ingress: {
        external: true   // publicly reachable; Easy Auth gates access
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: registryPassword
        }
        {
          name: 'aad-client-secret'
          value: aadClientSecret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${registryServer}/web:${imageTag}'
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
              // Aspire service discovery: resolves 'https+http://api' to the internal Container App
              name: 'Services__api__https__0'
              value: apiInternalUrl
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

// Easy Auth: restrict access to authenticated users in the owner's Entra ID tenant only.
// After first deployment, add the redirect URI to the App Registration in Entra ID:
//   https://<web-app-fqdn>/.auth/login/aad/callback
resource authConfig 'Microsoft.App/containerApps/authConfigs@2024-03-01' = {
  parent: webApp
  name: 'current'
  properties: {
    platform: {
      enabled: true
    }
    globalValidation: {
      unauthenticatedClientAction: 'RedirectToLoginPage'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: aadClientId
          clientSecretSettingName: 'aad-client-secret'
          openIdIssuer: 'https://sts.windows.net/${aadTenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            'api://${aadClientId}'
          ]
          defaultAuthorizationPolicy: {
            allowedPrincipals: {}
          }
        }
        isAutoProvisioned: false
      }
    }
    login: {
      routes: {}
    }
  }
}

output url string = 'https://${webApp.properties.configuration.ingress.fqdn}'
output fqdn string = webApp.properties.configuration.ingress.fqdn
