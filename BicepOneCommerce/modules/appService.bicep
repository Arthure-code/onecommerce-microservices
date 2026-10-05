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

@description('SKU du plan')
var skuName = niveauPlan == 'Prod' ? 'S1' : (niveauPlan == 'Test' ? 'B1' : 'F1')

@description('Suffixe aléatoire')
var randomSuffix = substring(uniqueString(resourceGroup().id), 0, 4)

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
