@description('Région du stockage')
param location string

@description('Tag Application stockage')
param applicationTag string

@description('Origines admises à écrire et lire les images depuis un navigateur')
param originesAutorisees array = []

@description('Nom du conteneur des images')
param conteneurImages string = 'images'

@description('Nom du compte storage')
var storageAccountName = 'stone${uniqueString(resourceGroup().id)}'

@description('Tags portés par les ressources du module')
var tags = {
  Application: applicationTag
}

// Compte de stockage

@description('Compte de stockage ZRS')
resource storageAccount 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_ZRS'
  }
  kind: 'StorageV2'
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    // Le chiffrement d'infrastructure ne s'active qu'à la création du compte.
    encryption: {
      keySource: 'Microsoft.Storage'
      requireInfrastructureEncryption: true
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
        queue: {
          enabled: true
          keyType: 'Account'
        }
      }
    }
  }
}

// Conteneur Blob "images"

@description('Service Blob par défaut')
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-06-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: originesAutorisees
          allowedMethods: [
            'GET'
            'HEAD'
            'PUT'
          ]
          allowedHeaders: [
            'content-type'
            'x-ms-blob-type'
          ]
          exposedHeaders: [
            'etag'
          ]
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

@description('Conteneur Blob images')
resource imagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-06-01' = {
  parent: blobService
  name: conteneurImages
  properties: {
    publicAccess: 'None'
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

@description('Nom du compte créé')
output storageAccountNameCreated string = storageAccount.name

@description('Adresse du point de terminaison Blob')
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
