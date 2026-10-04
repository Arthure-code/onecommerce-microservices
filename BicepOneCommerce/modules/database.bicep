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

// Serveur SQL

@description('Serveur SQL')
resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  tags: {
    Application: applicationTag
  }
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

@description('Pool élastique Basic')
resource sqlElasticPool 'Microsoft.Sql/servers/elasticPools@2014-04-01' = {
  parent: sqlServer
  name: sqlElasticPoolName
  location: location
  tags: {
    Application: applicationTag
  }
  properties: {
    edition: 'Basic'
    dtu: 200
    databaseDtuMin: 0
    databaseDtuMax: 5
  }
}

// Bases de données

@description('Bases du pool')
resource sqlDatabases 'Microsoft.Sql/servers/databases@2014-04-01' = [
  for dbName in dbNames: {
    parent: sqlServer
    name: dbName
    location: location
    tags: {
      Application: applicationTag
    }
    properties: {
      edition: 'Basic'
      requestedServiceObjectiveName: 'ElasticPool'
      elasticPoolName: sqlElasticPoolName
    }
    dependsOn: [
      sqlElasticPool
    ]
  }
]

@description('Règle de pare-feu SQL')
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowRange_100_0_0_1_to_100_10_255_255'
  properties: {
    startIpAddress: '100.0.0.1'
    endIpAddress: '100.10.255.255'
  }
}
