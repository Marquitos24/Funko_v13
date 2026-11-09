using System.Text;
using FluentValidation;
using Funkos.Cache;
using Funkos.DbContext;
using Funkos.Infrastructure.Storage;
using Funkos.Models;
using Funkos.Mongo;
using Funkos.Repositories;
using Funkos.Repositories.Categoria;
using Funkos.Repositories.Proveedor;
using Funkos.Service;
using Funkos.Service.Categoria;
using Funkos.Service.Decorator;
using Funkos.Service.Proveedor;
using Funkos.Service.Storage;
using Funkos.Validations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using StackExchange.Redis;
using Role = Funkos.Models.Role;

namespace Funkos.Infrastructure;

public static class ApplicationInfraestructure
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        #region Repositorios y Servicios CRUD
        services.AddScoped<IFunkoRepository, FunkoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        
        services.AddScoped<FunkoService>();
        services.AddScoped<ProveedorService>();
        services.AddScoped<CategoriaService>();
        #endregion

        #region VALIDACIONES (FluentValidation)
        // Registrar los validators --> NO ejecuta la validación por sí solo
        services.AddValidatorsFromAssemblyContaining<ProveedorUpdateValidation>();
        // Ejecución automática vía action filter --> Paquete FluentValidation .AspNetCore
        // Requiere llamarse DESPUÉS de AddControllers() = Solo funciona en MVC/controllers
        //builder.Services.AddFluentValidationAutoValidation();
        #endregion

        #region Decorator
        // Decorator como servicio que recibirá el Controller
        services.AddScoped<IFunkoService>(sp => new CachedFunkoServiceDecorator(sp.GetRequiredService<FunkoService>(), sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<ILogger<CachedFunkoServiceDecorator>>()));
        services.AddScoped<ICategoriaService>(sp => new CachedCategoriaServiceDecorator(sp.GetRequiredService<CategoriaService>(), sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<ILogger<CachedCategoriaServiceDecorator>>()));
        services.AddScoped<IProveedorService>(sp => new CachedProveedorServiceDecorator(sp.GetRequiredService<ProveedorService>(), sp.GetRequiredService<ICacheService>(), sp.GetRequiredService<ILogger<CachedProveedorServiceDecorator>>()));
        #endregion

        #region POSTGRESQL
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        // Conectamos desde appsettings
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        #endregion

        #region MONGO
        // MongoClient es thread-safe ==> se Reutiliza durante toda la app. MongoDbContext solo tiene referencias a la BD y a las colecciones.
        // No tiene el comportamiento de seguimiento de entidades de un DbContext de EF Core.
        var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>()!;
        // Registrar configuración
        services.AddSingleton(settings);
        // Crear una única instancia de MongoClient
        services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));
        // Registrar contexto
        services.AddSingleton<MongoDbContext>();
        
        #endregion

        #region REDIS - CACHE
        //IConnectionMultiplexer ==> Objeto principal de StackExchange.Redis que gestiona las conexiones con el servidor Redis.
        // Desde él se puede obtener una BD Redis
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(
            configuration["Redis:ConnectionString"]!));

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
        });

        #endregion

        #region IMAGES
        services.AddScoped<IStorageService, FileSystemStorageService>();
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));

        // Configurar límite de formularios multipart
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
        });
        #endregion

        #region IDENTITY Y JWT
        // ++++++++ IDENTITY Y JWT (SIN VISTAS NI COOKIES DE UI) ++++++
        // AddIdentityCore: Identity sin UI; roles y signInManager se añaden a mano
       services.AddIdentityCore<User>(options =>
            {
                // --- CONTRASEÑA ---
                options.Password.RequireDigit = true;           // Exige al menos un número (0-9).
                options.Password.RequireLowercase = true;       // Exige al menos una minúscula (a-z).
                options.Password.RequireUppercase = true;       // Exige al menos una mayúscula (A-Z).
                options.Password.RequireNonAlphanumeric = true; // Exige al menos un carácter especial (!, ?, @, #...).
                options.Password.RequiredLength = 8;            // Establece el mínimo de caracteres (8)

                // --- USUARIO ---
                options.User.RequireUniqueEmail = true;         // Email único

                // --- CUENTA ---
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Tiempo de bloqueo (15 minutos)
                options.Lockout.MaxFailedAccessAttempts = 5;    // Intentos fallidos permitidos antes del bloqueo (5)
                
                options.ClaimsIdentity.UserIdClaimType = JwtRegisteredClaimNames.Sub;
            })
            .AddRoles<Role>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Forzar JWT Bearer como esquema global evitando conflictos con cookies
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;                 //
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;     //
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;        //
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;           //
            })
            .AddJwtBearer(options =>
            {
                // Desactivado el mapeo automático de claims
                options.MapInboundClaims = false;
                
                #region Validacion del Token

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validar que el token haya sido firmado con nuestra clave secreta
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret no configurado"))),
                    
                    // Validar quién emite el token (Issuer)
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    
                    // Validar para quién va dirigido el token (Audience)
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                   
                    //Validar que el token no haya expirado
                    ValidateLifetime = true,
                    
                    // Evita desfases horarios (da un margen de unos pocos segundos de tolerancia)
                    ClockSkew = TimeSpan.Zero,
                    
                    //Indicamos qué claim del JWT representa los roles
                    RoleClaimType = "role"
                };

                #endregion
            });
        #endregion

        #region AUTORIZACIONES
        // Central para gestionar las autorizacioness 
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build())
            .AddPolicy("RequireAdmin", policy =>
                policy.RequireAssertion(context =>
                        context.User.IsInRole("ADMIN") && context.User.HasClaim("department", "IT")))
                
            .AddPolicy("RequireUser", policy =>
                policy.RequireRole("USER", "ADMIN"));
        
        //Futuras actualizaciones:
            // - Añadir Requirements y handlers personalizados
        #endregion
        
        return services;
    }
}