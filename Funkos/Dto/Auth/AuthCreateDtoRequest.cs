using System.ComponentModel.DataAnnotations;
using Funkos.Validations;

namespace Funkos.Dto.Auth;

/// <summary>
/// DTO para el registro de nuevos usuarios.
/// Define los datos necesarios para crear una nueva cuenta.
/// </summary>
public record AuthCreateDtoRequest
{
    [Required (ErrorMessage =  "El nombre de usuario es obligatorio")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Solo se permiten letras, números y guiones bajos")]
    public required string Username { get; set; } = string.Empty;
    
    [Required (ErrorMessage =  "El correo electronico es obligatorio")]
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "El correo electronico no puede superar los 256 caracteres")]
    public required string Email { get; set; } = string.Empty;
    
    [Required (ErrorMessage =  "Añada una contraseña")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public required string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO para actualizar datos de usuario.
/// </summary>
public record AuthUpdateDtoRequest
{
    public long Id;
    
    [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Solo se permiten letras, números y guiones bajos")]
    public string Username { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "Debe ser un correo electrónico válido")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "El correo electronico no puede superar los 256 caracteres")]
    public string Email { get; set; } = string.Empty;
    
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO para el inicio de sesión de usuarios.
/// Credenciales necesarias para obtener token JWT.
/// </summary>
public record LoginRequest
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string Username { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}