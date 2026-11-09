using Funkos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Funkos.DbContext;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, long>(options) // Ya se reconce EntityFrameworkCore.DbContext(options), 
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Funko> Funkos => Set<Funko>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Clase base de Identity ==> configura automáticamente tablas, índices, claves y relaciones.
        base.OnModelCreating(modelBuilder); 
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        //TABLAS DE IDENTITY
        modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserRole<long>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<long>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<long>>().ToTable("role_claims");
    }
}