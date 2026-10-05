using Microsoft.Extensions.Azure;
using OneCommandes.API.Interfaces;
using OneCommandes.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiDefaults();

//Ajout du serviceBusClient au conteneur d'IoC
string? connexionServiceBus = builder.Configuration.GetConnectionString("SvCConnectionString");

if (string.IsNullOrWhiteSpace(connexionServiceBus))
{
    builder.Services.AddSingleton<IServiceBusHelper, ServiceBusHorsLigne>();
}
else
{
    builder.Services.AddAzureClients
        (configure =>
        {
            configure.AddServiceBusClient(connexionServiceBus);
        });

    builder.Services.AddScoped<IServiceBusHelper, ServiceBusHelper>();
}

var app = builder.Build();

app.UseApiDefaults();

await app.RunAsync();
