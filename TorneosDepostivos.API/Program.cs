using Microsoft.EntityFrameworkCore;
// Asegúrate de tener el using donde está tu DbContext, por ejemplo:
// using TorneosDepostivos.API.Datos; 

namespace TorneosDepostivos.API
{
  public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // IMPORTANTE: Aquí deberías usar tu propia clase, ej: <CineContext>
            // Si tu clase se llama realmente 'DbContext', asegúrate de que no sea la de Microsoft.
            builder.Services.AddDbContext<TorneosDeportivosDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("TorneosDeportivosDbContext")
                ?? throw new InvalidOperationException("Connection string 'TorneosDeportivosDbContext' not found.")));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services
                .AddControllers()
                .AddNewtonsoftJson(
                    options =>
                        options.SerializerSettings.ReferenceLoopHandling
                            = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );

            var app = builder.Build();

            // ---------------------------------------------------------
            // BLOQUE PARA APLICAR MIGRACIONES AUTOMÁTICAS EN RENDER
            // ---------------------------------------------------------
            using(var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    // Obtiene tu DbContext
                    // NOTA: Cambia <DbContext> por el nombre real de tu clase heredada (ej. <CineContext>)
                    var context = services.GetRequiredService<TorneosDeportivosDbContext>();

                    // Aplica cualquier migración pendiente automáticamente
                    context.Database.Migrate();
                }
                catch(Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Ocurrió un error al migrar la base de datos.");
                }
            }
            // ---------------------------------------------------------

            //if(app.Environment.IsDevelopment()) // Comentado para ver Swagger en Render si lo deseas
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}