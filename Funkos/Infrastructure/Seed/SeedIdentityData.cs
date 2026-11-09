using System.Security.Claims;
using Funkos.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Funkos.Infrastructure.Seed;

public class SeedIdentityData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<User>>();

        // Crear roles
        string[] roles = [UserRoles.Admin, UserRoles.User];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role{Name = roleName});
                Log.Information("Rol creado: {Role}", roleName);
            }
        }

        // Crear usuario admin
        var adminEmail = "admin@admin.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new User
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString() //Evita el error de estampilla de seguridad
            };
            
            // Añadir claims a un usuario
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                await userManager.AddClaimAsync(adminUser, new Claim("department", "IT"));
                Log.Information("Usuario admin creado: {Email}", adminEmail);
            }
        }

        // Crear usuario normal
        var userEmail = "user@user.com";
        var normalUser = await userManager.FindByEmailAsync(userEmail);
        if (normalUser is null)
        {
            normalUser = new User
            {
                UserName = "user",
                Email = userEmail,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            var result = await userManager.CreateAsync(normalUser, "User123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(normalUser, UserRoles.User);
                Log.Information("Usuario normal creado: {Email}", userEmail);
            }
        }
    }
}