using './main.bicep'

// ── Non-secret parameters ─────────────────────────────────────────────────────
// Secrets (postgresAdminPassword, aadClientSecret) are passed via
// --parameters in the GitHub Actions workflow so they never appear in source.

param environmentName = 'prod'

// Set this to the Azure region closest to your users, e.g. 'westeurope', 'eastus'
param location = 'westeurope'

// Globally unique ACR name – alphanumeric only, 5–50 characters.
// Override via the AZURE_CONTAINER_REGISTRY GitHub Secret / --parameters in CI.
param containerRegistryName = 'REPLACE_WITH_ACR_NAME'

param postgresAdminLogin = 'wtadmin'

// Image tags are overridden by CI with the short Git SHA.
param apiImageTag = 'latest'
param webImageTag = 'latest'

// Set these to the values from your Entra ID App Registration.
// Client ID is not secret; override via AZURE_AD_CLIENT_ID GitHub Secret in CI.
param aadClientId = 'REPLACE_WITH_AAD_CLIENT_ID'

// aadTenantId is passed as a secret in CI (AZURE_TENANT_ID).
param aadTenantId = 'REPLACE_WITH_TENANT_ID'
