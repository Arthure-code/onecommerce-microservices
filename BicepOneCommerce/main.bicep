@description('Niveau du plan')
@allowed([
  'Dev'
  'Test'
  'Prod'
])
param niveauPlan string = 'Dev'

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

@description('Nom du conteneur des images')
param conteneurImages string = 'images'

@description('Suffixe partagé par les noms dans le groupe de ressources')
var randomSuffix = substring(uniqueString(resourceGroup().id), 0, 4)

@description('Adresse de la boutique, seule origine admise à déposer une image')
var urlBoutique = 'https://webapp-OneCommerceMVC-${randomSuffix}.azurewebsites.net'

@description('Configs des App Services')
var appServiceConfigs = [
  {
    planNameSuffix: 'MVCProduits'
    appNames: [
      'OneCommerceMVC'
      'OneProduit'
    ]
    accesBlob: []
  }
  {
    planNameSuffix: 'APIs'
    appNames: [
      'OneFichiers'
      'OneCommandes'
      'OneFidelite'
    ]
    accesBlob: [
      'OneFichiers'
    ]
  }
]

// Plans App Service + Web Apps

module appService 'modules/appService.bicep' = [
  for config in appServiceConfigs: {
    name: 'deploy-${config.planNameSuffix}'
    params: {
      location: location
      niveauPlan: niveauPlan
      planNameSuffix: config.planNameSuffix
      appNames: config.appNames
      applicationTag: applicationTag
      randomSuffix: randomSuffix
      storageAccountName: storage.outputs.storageAccountNameCreated
      conteneurImages: conteneurImages
      urlBoutique: urlBoutique
      applicationsAvecAccesBlob: config.accesBlob
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
    conteneurImages: conteneurImages
    originesAutorisees: [
      urlBoutique
    ]
  }
}

@description('Noms des applications créées, suffixe compris, par plan')
output appNamesCreated array = [
  for (config, i) in appServiceConfigs: {
    plan: config.planNameSuffix
    apps: appService[i].outputs.appNamesCreated
  }
]

@description('Adresse du serveur SQL')
output sqlServerFqdn string = database.outputs.sqlServerFqdn

@description('Nom du compte de stockage créé')
output storageAccountName string = storage.outputs.storageAccountNameCreated
