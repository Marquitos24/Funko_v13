using Funkos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Funkos.DbContext.Configuration;

public class UserConfuguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> modelBuilder)
    {
        modelBuilder.ToTable("users");
        
        modelBuilder.Property(u => u.UserName).HasMaxLength(50);
        modelBuilder.HasIndex(p => p.UserName).IsUnique();

        modelBuilder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        modelBuilder.HasIndex(p => p.Email).IsUnique();

        modelBuilder.Property(u => u.CreatedAt)
            .IsRequired();

        modelBuilder.Property(u => u.IsActive).IsRequired();
    }
}