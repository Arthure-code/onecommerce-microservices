@description('Suffixe du plan')
param planNameSuffix string

@description('Noms des applications')
param appNames array

@description('Niveau du plan')
@allowed([
  'Dev'
  'Test'
  'Prod'
])
param niveauPlan string

@description('Région de déploiement')
param location string

@description('Tag Application')
param applicationTag string

@description('Suffixe partagé par les noms dans le groupe de ressources')
param randomSuffix string

@description('Nom du compte de stockage que les images utilisent')
param storageAccountName string

@description('Conteneur où les images sont déposées')
param conteneurImages string

@description('Adresse de la boutique, seule origine admise à déposer une image')
param urlBoutique string

@description('Applications à qui le rôle sur le conteneur est donné')
param applicationsAvecAccesBlob array = []

@description('SKU du plan')
var skuName = niveauPlan == 'Prod' ? 'S1' : (niveauPlan == 'Test' ? 'B1' : 'F1')

@description('Nom du plan App Service')
var appServicePlanName = 'sp-${planNameSuffix}-${randomSuffix}'

@description('Plan App Service')
resource appServicePlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
    capacity: 1
  }
  tags: {
    Application: applicationTag
  }
}

@description('Applications web')
resource webApps 'Microsoft.Web/sites@2025-03-01' = [
  for appName in appNames: {
    name: 'webapp-${appName}-${randomSuffix}'
    location: location
    identity: {
      type: 'SystemAssigned'
    }
    tags: {
      Application: applicationTag
    }
    properties: {
      serverFarmId: appServicePlan.id
      httpsOnly: true
      siteConfig: {
        minTlsVersion: '1.2'
        ftpsState: 'Disabled'
        http20Enabled: true
      }
    }
  }
]

@description('Compte de stockage auquel les applications accèdent')
resource compteStockage 'Microsoft.Storage/storageAccounts@2025-06-01' existing = {
  name: storageAccountName
}

// Contributeur aux données Blob : c'est ce rôle qui permet à l'application
// de demander une clé de délégation, donc de signer ses liens sans clé de compte.
var roleContributeurBlob = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

@description('Accès au conteneur, donné aux seules applications qui en ont besoin')
resource accesBlob 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (appName, i) in appNames: if (contains(applicationsAvecAccesBlob, appName)) {
    scope: compteStockage
    name: guid(compteStockage.id, webApps[i].id, roleContributeurBlob)
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleContributeurBlob)
      principalId: webApps[i].identity.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

@description('Réglages des applications qui déposent des images')
resource reglagesImages 'Microsoft.Web/sites/config@2025-03-01' = [
  for (appName, i) in appNames: if (contains(applicationsAvecAccesBlob, appName)) {
    parent: webApps[i]
    name: 'appsettings'
    properties: {
      CompteStockage: storageAccountName
      ConteneurImages: conteneurImages
      OriginesBoutique__0: urlBoutique
    }
  }
]

// Bicep refuse une ressource imbriquée dans une expression for, BCP160, donc
// la configuration est attachée à chaque application plutôt que déclarée dedans.
@description('Authentification des applications, laissée explicitement ouverte')
resource authentification 'Microsoft.Web/sites/config@2025-03-01' = [
  for (appName, i) in appNames: {
    parent: webApps[i]
    name: 'authsettingsV2'
    properties: {
      globalValidation: {
        requireAuthentication: false
        unauthenticatedClientAction: 'AllowAnonymous'
      }
      platform: {
        enabled: false
      }
    }
  }
]

@description('Slots "staging" Prod')
resource stagingSlots 'Microsoft.Web/sites/slots@2025-03-01' = [
  for (appName, i) in appNames: if (niveauPlan == 'Prod') {
    parent: webApps[i]
    name: 'staging'
    location: location
    identity: {
      type: 'SystemAssigned'
    }
    tags: {
      Application: applicationTag
    }
    properties: {
      serverFarmId: appServicePlan.id
      httpsOnly: true
      siteConfig: {
        minTlsVersion: '1.2'
        ftpsState: 'Disabled'
        http20Enabled: true
      }
    }
  }
]

@description('Autoscale en production')
resource autoScale 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (niveauPlan == 'Prod') {
  name: 'autoscale-${appServicePlanName}'
  location: location
  tags: {
    Application: applicationTag
  }
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'DefaultProfile'
        capacity: {
          minimum: '1'
          maximum: '4'
          default: '1'
        }
        rules: [
          //  Scale OUT (augmenter)
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricNamespace: 'microsoft.web/serverfarms'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }

          //  Scale IN (réduire)
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricNamespace: 'microsoft.web/serverfarms'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
        ]
      }
    ]
  }
}

@description('Noms des applications créées, suffixe compris')
output appNamesCreated array = [for (appName, i) in appNames: webApps[i].name]
