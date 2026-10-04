
using OneCommerce.MVC;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Services;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllersWithViews();



builder.Services.AddHttpClient<IProduitService, ProduitServiceProxy>(client =>
{
    var produitEndpoint = builder.Configuration["ProduitEndpointHttps"]
                      ?? builder.Configuration["ProduitEndpoint"]
                      ?? throw new InvalidOperationException("ProduitEndpoint non configuré");

    client.BaseAddress = new Uri(produitEndpoint);
});


builder.Services.AddHttpClient<IFichiersService, FichiersServiceProxy>(client =>
{
    var fichierEndpoint = builder.Configuration["FichiersEndpointHttps"]
                          ?? builder.Configuration["FichiersEndpoint"]
                          ?? throw new InvalidOperationException("FichierEndpoint non configuré");

    client.BaseAddress = new Uri(fichierEndpoint);
});


builder.Services.AddHttpClient<IFideliteService, FideliteServiceProxy>(client =>
{
    var fideliteEndpoint = builder.Configuration["FideliteEndpointHttps"]
                           ?? builder.Configuration["FideliteEndpoint"]
                           ?? throw new InvalidOperationException("FideliteEndpoint non configuré");

    client.BaseAddress = new Uri(fideliteEndpoint);
});

builder.Services.AddHttpClient<ICommandesService, CommandesServiceProxy>(client =>
{
    var commandesEndpoint = builder.Configuration["CommandesEndpointHttps"]
                           ?? builder.Configuration["CommandesEndpoint"]
                           ?? throw new InvalidOperationException("CommandesEndpoint non configuré");
    client.BaseAddress = new Uri(commandesEndpoint);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Produits/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Produits}/{action=Index}/{id?}");

app.Run();
