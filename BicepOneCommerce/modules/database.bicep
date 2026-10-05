@description('Région SQL')
param location string

@description('Nom du projet')
param projectName string = 'onecommerce-${uniqueString(resourceGroup().id)}'

@description('Login admin SQL')
param sqlAdminLogin string = 'sqladmin'

@description('Mot de passe SQL admin')
@secure()
@minLength(10)
@maxLength(20)
param sqlAdminPassword string

@description('Tag Application SQL')
param applicationTag string

@description('Nom du serveur SQL')
var sqlServerName = 'srv-${projectName}'

@description('Nom du pool élastique')
var sqlElasticPoolName = 'pool-${projectName}'

@description('Noms des bases')
var dbNames = [
  'dbProduit'
  'dbCommandes'
  'dbFidelite'
]

@description('Capacité du pool, en DTU')
var poolCapacity = 200

@description('DTU garanties à chaque base')
var dbMinCapacity = 0

@description('DTU maximales par base, plafond du niveau Basic')
var dbMaxCapacity = 5

@description('Tags portés par les ressources du module')
var tags = {
  Application: applicationTag
}

// Serveur SQL

@description('Serveur SQL')
resource sqlServer 'Microsoft.Sql/servers@2025-01-01' = {
  name: sqlServerName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

@description('Pool élastique Basic')
resource sqlElasticPool 'Microsoft.Sql/servers/elasticPools@2025-01-01' = {
  parent: sqlServer
  name: sqlElasticPoolName
  location: location
  sku: {
    name: 'BasicPool'
    tier: 'Basic'
    capacity: poolCapacity
  }
  tags: tags
  properties: {
    perDatabaseSettings: {
      minCapacity: dbMinCapacity
      maxCapacity: dbMaxCapacity
    }
  }
}

// Bases de données

@description('Bases du pool')
resource sqlDatabases 'Microsoft.Sql/servers/databases@2025-01-01' = [
  for dbName in dbNames: {
    parent: sqlServer
    name: dbName
    location: location
    sku: {
      name: 'ElasticPool'
      tier: 'Basic'
    }
    identity: {
      type: 'SystemAssigned'
    }
    tags: tags
    properties: {
      elasticPoolId: sqlElasticPool.id
    }
  }
]

@description('Règle de pare-feu SQL')
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2025-01-01' = {
  parent: sqlServer
  name: 'AllowRange_100_0_0_1_to_100_10_255_255'
  properties: {
    startIpAddress: '100.0.0.1'
    endIpAddress: '100.10.255.255'
  }
}

@description('Adresse à laquelle les applications se connectent')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Noms des bases créées')
output dbNamesCreated array = [for (dbName, i) in dbNames: sqlDatabases[i].name]
