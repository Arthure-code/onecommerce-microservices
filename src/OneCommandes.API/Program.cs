using Microsoft.Extensions.Azure;
using OneCommandes.API.Interfaces;
using OneCommandes.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Ajout du serviceBusClient au conteneur d'IoC
builder.Services.AddAzureClients
    (configure =>
    {
        configure.AddServiceBusClient(builder.Configuration.GetConnectionString("SvCConnectionString"));

    });

builder.Services.AddScoped<IServiceBusHelper, ServiceBusHelper>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
