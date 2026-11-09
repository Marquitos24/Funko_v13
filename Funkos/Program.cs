using System.Text.Json.Serialization;
using FluentValidation;
using Funkos.Infrastructure;
using Funkos.Infrastructure.Seed;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json + appsettings.{Environment}.json + variables de entorno
//var miConfig = builder.Configuration.GetSection("Application").Get<AppConfig>();
    
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

#region Negociación de Codigo
// Negociación de codigo
    #region JSON
    // JSON
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });


    #endregion

    #region XML

    //  XML
    /*
     * builder.Services.AddControllers(options =>
        {
            options.RespectBrowserAcceptHeader = true;   // Respetar header Accept del cliente
            options.ReturnHttpNotAcceptable = true;       // Devolver 406 si formato no soportado
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .AddXmlSerializerFormatters(); // Cambiado a XmlSerializer para DTOs normales (sin [DataContract])
    */

    #endregion
#endregion

#region Imagenes (request body)
// Configurar límite del request body
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});
#endregion


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog(); // <- Aquí conectas Serilog con el sistema de ILogger de .NET y lo Sustitulles

// ============================================================================
//              CONSTRUCCIÓN DE LA APLICACIÓN
// ============================================================================

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await SeedIdentityData.SeedAsync(scope.ServiceProvider);
}

app.UseExceptionHandler();

app.UseAuthentication();  // Primero: quien eres?
app.UseAuthorization();   // Segundo: que puedes hacer?


// Habilitar archivos estáticos desde wwwroot
app.UseStaticFiles();

Console.WriteLine($"WebRootPath: {app.Environment.WebRootPath}"); // Salida: C:\...\Funkos\wwwroot
Console.WriteLine($"ContentRootPath: {app.Environment.ContentRootPath}"); // Salida: C:\...\Funkos

app.MapControllers();
app.Run();


