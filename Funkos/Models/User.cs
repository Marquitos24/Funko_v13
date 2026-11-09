using Microsoft.AspNetCore.Identity;

namespace Funkos.Models;

public class User : IdentityUser<long>
{
    //Identity ya cuenta con Username y Email

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Rol personalizado con descripcion.
/// </summary>
public class Role : IdentityRole<long>
{
    public string? Description { get; set; }
}

public static class UserRoles
{
    public const string Admin = "ADMIN";
    public const string User = "USER";
}