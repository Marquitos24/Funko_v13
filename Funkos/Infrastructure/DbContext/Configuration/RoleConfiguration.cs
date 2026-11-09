using Funkos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Funkos.DbContext.Configuration;

public class RoleConfiguration: IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> modelBuilder)
    {
        modelBuilder.ToTable("roles");

        modelBuilder.Property(r => r.Description).HasMaxLength(250);
    }
    
}