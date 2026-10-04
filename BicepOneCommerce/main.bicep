@description('Niveau du plan')
@allowed([
  'Dev'
  'Test'
  'Prod'
])
param NiveauPlan string = 'Dev'

@description('Région de déploiement')
param location string = resourceGroup().location

@description('Mot de passe SQL admin')
@secure()
@minLength(10)
@maxLength(20)
param sqlAdminPassword string

@description('Nom du projet')
param projectName string = 'onecommerce-${uniqueString(resourceGroup().id)}'

@description('Tag Application')
param applicationTag string = 'OneCommerce'

@description('Login admin SQL')
param sqlAdminLogin string = 'sqladmin'

@description('Configs des App Services')
var appServiceConfigs = [
  {
    planNameSuffix: 'MVCProduits'
    appNames: [
      'OneCommerceMVC'
      'OneProduit'
    ]
  }
  {
    planNameSuffix: 'APIs'
    appNames: [
      'OneFichiers'
      'OneCommandes'
      'OneFidelite'
    ]
  }
]

// Plans App Service + Web Apps

module appService 'modules/appService.bicep' = [
  for config in appServiceConfigs: {
    name: 'deploy-${config.planNameSuffix}'
    params: {
      location: location
      NiveauPlan: NiveauPlan
      planNameSuffix: config.planNameSuffix
      appNames: config.appNames
      applicationTag: applicationTag
    }
  }
]

// BD

module database 'modules/database.bicep' = {
  name: 'deploy-database'
  params: {
    location: location
    projectName: projectName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    applicationTag: applicationTag
  }
}

// Stockage

module storage 'modules/storage.bicep' = {
  name: 'deploy-storage'
  params: {
    location: location
    applicationTag: applicationTag
  }
}
