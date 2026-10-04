@description('Nom du Service Bus')
param serviceBusName string

@description('Région autorisée')
@allowed([
  'CanadaCentral'
  'CanadaEast'
])
param location string

@description('Niveau tarifaire du Service Bus')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string

@description('Créer le namespace ou utiliser un existant')
param createNamespace bool = true

@description('Tag Application')
param applicationTag string

@description('Liste des files d’attente à créer')
param queues array

// Namespace du Service Bus
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = if (createNamespace) {
  name: serviceBusName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  tags: {
    Application: applicationTag
  }
  properties: {}
}

// Files d’attente

resource serviceBusQueues 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = [
  for queue in queues: {
    parent: serviceBus
    name: queue.name
    properties: {
      defaultMessageTimeToLive: queue.defaultMessageTimeToLive
      maxSizeInMegabytes: queue.maxSizeInMegabytes
    }
  }
]
