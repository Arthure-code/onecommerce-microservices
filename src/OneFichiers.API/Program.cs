using Azure.Identity;
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

string? compteStockage = builder.Configuration["CompteStockage"];
string conteneurImages = builder.Configuration["ConteneurImages"] ?? "images";

if (string.IsNullOrWhiteSpace(compteStockage))
{
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<IMagasinImages, MagasinDisque>();
}
else
{
    // Aucune clé de compte : l'application signe ses liens avec sa propre
    // identité, à qui le rôle sur le conteneur a été donné au déploiement.
    builder.Services.AddSingleton(_ => new BlobServiceClient(
        new Uri($"https://{compteStockage}.blob.core.windows.net"),
        new DefaultAzureCredential()));

    builder.Services.AddSingleton(fournisseur =>
        fournisseur.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(conteneurImages));

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

await app.RunAsync();
