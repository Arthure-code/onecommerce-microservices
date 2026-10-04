@description('Région de déploiement')
@allowed([
  'CanadaCentral'
  'CanadaEast'
])
param location string

@description('Environnement (dev, prod)')
param environment string 

@description('Préfixe du nom du Service Bus')
var serviceBusName = 'sb-onecommerce-${environment}' 

@description('Niveau tarifaire')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('Créer le Service Bus ou utiliser un existant')
param createNamespace bool = true

@description('Tag Application')
param applicationTag string = 'OneCommerce'

@description('Liste des files d’attente à créer')
var queues = [
  {
    name: 'produits'
    defaultMessageTimeToLive: 'P7D'
    maxSizeInMegabytes: 1024
  }
  {
    name: 'fidelites'
    defaultMessageTimeToLive: 'P1M'
    maxSizeInMegabytes: 1024
  }
  {
    name: 'livraisons'
    defaultMessageTimeToLive: 'PT12H'
    maxSizeInMegabytes: 1024
  }
]

// Module Service Bus

module serviceBus 'modules/serviceBus.bicep' = {
  name: 'deploy-servicebus-${serviceBusName}'
  params: {
    serviceBusName: serviceBusName
    location: location
    skuName: skuName
    createNamespace: createNamespace
    applicationTag: applicationTag
    queues: queues
  }
}
