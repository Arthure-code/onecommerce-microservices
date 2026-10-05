using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting
{
    // Les quatre API démarrent de la même façon. Ce qui leur est propre, la
    // messagerie, le stockage, reste dans leur propre Program.
    public static class ApiDefaults
    {
        public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
        {
            builder.AddServiceDefaults();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            return builder;
        }

        public static WebApplication UseApiDefaults(this WebApplication app)
        {
            app.MapDefaultEndpoints();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            return app;
        }
    }
}
