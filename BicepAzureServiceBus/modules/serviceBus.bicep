@description('Nom du Service Bus')
param serviceBusName string

@description('Région de déploiement')
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
resource serviceBus 'Microsoft.ServiceBus/namespaces@2026-01-01' = if (createNamespace) {
  name: serviceBusName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  identity: {
    type: 'SystemAssigned'
  }
  tags: {
    Application: applicationTag
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Files d’attente

resource serviceBusQueues 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = [
  for queue in queues: {
    parent: serviceBus
    name: queue.name
    properties: {
      defaultMessageTimeToLive: queue.defaultMessageTimeToLive
      maxSizeInMegabytes: queue.maxSizeInMegabytes
    }
  }
]

@description('Adresse du namespace, vide si le namespace existait déjà')
output serviceBusEndpoint string = serviceBus.?properties.serviceBusEndpoint ?? ''
