@description('Région du stockage')
param location string

@description('Tag Application stockage')
param applicationTag string

@description('Nom du compte storage')
var storageAccountName = 'stone${uniqueString(resourceGroup().id)}'

// Compte de stockage

@description('Compte de stockage ZRS')
resource storageAccount 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_ZRS'
  }
  properties: {
    accessTier: 'Hot'
  }
  tags: {
    Application: applicationTag
  }
}

// Conteneur Blob "images"

@description('Service Blob par défaut')
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-06-01' = {
  parent: storageAccount
  name: 'default'
}

@description('Conteneur Blob images')
resource imagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-06-01' = {
  parent: blobService
  name: 'images'
  properties: {
    publicAccess: 'None' // privé
  }
}

// File d’attente "q-commande"

@description('Service de files d`attente')
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2025-06-01' = {
  parent: storageAccount
  name: 'default'
}

@description('File d`attente q-commande')
resource commandeQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2025-06-01' = {
  parent: queueService
  name: 'q-commande'
}
