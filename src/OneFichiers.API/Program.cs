using Azure.Storage.Blobs;
using OneFichiers.API.Interfaces;
using OneFichiers.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();

// Le navigateur dépose l'image lui-même, donc l'origine de la boutique doit
// être admise ici quand le repli sur disque reçoit les octets.
string[] originesBoutique = builder.Configuration.GetSection("OriginesBoutique").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(politique => politique
        .WithOrigins(originesBoutique)
        .WithMethods("PUT")
        .WithHeaders("Content-Type", "x-ms-blob-type"));
});

string? connexionStockage = builder.Configuration.GetConnectionString("Stockage");
string conteneurImages = builder.Configuration["ConteneurImages"] ?? "images";

if (string.IsNullOrWhiteSpace(connexionStockage))
{
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<IMagasinImages, MagasinDisque>();
}
else
{
    builder.Services.AddSingleton(_ =>
    {
        var conteneur = new BlobContainerClient(connexionStockage, conteneurImages);
        conteneur.CreateIfNotExists();

        return conteneur;
    });

    builder.Services.AddSingleton<IMagasinImages, MagasinBlob>();
}
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
