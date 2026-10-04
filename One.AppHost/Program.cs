using Aspire.Hosting;
using Projects;
using System.ComponentModel.Design;

var builder = DistributedApplication.CreateBuilder(args);

var commandes = builder.AddProject<OneCommandes_API>("commandes");
var fichiers = builder.AddProject<OneFichiers_API>("fichiers");
var fidelite = builder.AddProject<OneFidelite_API>("fidelite");
var produit = builder.AddProject<OneProduit_API>("produit");


builder.AddProject<OneCommerce_MVC>("mvc")
    .WithExternalHttpEndpoints()
    .WithReference(commandes)
    .WithReference(produit)
    .WithReference(fichiers)
    .WithReference(fidelite)
    .WaitFor(produit)
    .WaitFor(fichiers);





builder.Build().Run();
