using System.Security.Claims;
using System.Text;
using Funkos.Dto.Auth;
using Funkos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using LoginRequest = Funkos.Dto.Auth.LoginRequest;

namespace Funkos.Controller;

/// <summary>
/// Controller de autenticacion usando ASP.NET Core Identity.
/// Gestiona el registro de usuarios, inicio de sesión (generación de tokens JWT) y obtención del perfil actual.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthIdentityController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, ILogger<AuthIdentityController> logger) : ControllerBase
{
    // UserManager<User> ==> gestiona CRUD de usuarios (crear, buscar, actualizar, eliminar, gestionar roles)
    //  SignInManager<User> ==> gestiona las operaciones de login (verificar contraseña, bloqueo, login externo)
    
    /// <summary>
    /// Registra un usuario nuevo con Identity.
    /// </summary>
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] AuthCreateDtoRequest request)
    {
        var user = new User
        { //Datos por defecto de entity
            UserName = request.Username,
            Email = request.Email,
        };
        
        //Guardamos y creamos el nuevo usuario con las reglas de contraseña
        var result = await userManager.CreateAsync(user, request.Password);
        logger.LogInformation("Creando usuario");

        
        if (!result.Succeeded)
        {
            logger.LogWarning("Error de registro de usuario");
            return BadRequest(new ProblemDetails
            {
                Title = "Error de registro",
                Detail = string.Join(", ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }
        // A todo nuevo usuario se le asigna el rol de USER
        await userManager.AddToRoleAsync(user, UserRoles.User);
        logger.LogInformation("Asignando rol de usuario");
        // Retornamos 201 con sus datos
        return CreatedAtAction(nameof(GetCurrentUser), new { }, new
        {
            user.Id,
            user.UserName,
            user.Email
        });
    }

    /// <summary>
    /// Inicia sesion verificando las credenciales y genera un Token JWT para autenticar futuras peticiones.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        //Buscamos al usuario por su nombre de usuario
        var userName = await  userManager.FindByNameAsync(request.Username);
        
        if (userName is null)
        {
            logger.LogWarning("Credenciales invalidas del usuario");
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales invalidas",
                Status = StatusCodes.Status401Unauthorized
            });
        }
        logger.LogInformation("Usuario {Username} encontrado", userName.UserName);
        // Verificamos que la contraseña sea correcta (bloqueando la cuenta si excede los intentos permitidos = 5)
        var result = await signInManager.CheckPasswordSignInAsync(userName, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogWarning("Contraseña incorrecta del usuario");
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales invalidas",
                Status = StatusCodes.Status401Unauthorized
            });
        }
        
        var userLogin = userName.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(userName);  
        logger.LogInformation("Usuario {Username} con inicio de sesion el {UserLogin}", userName.UserName, userLogin);

        
        // Generar JWT (esqueleto mínimo con JwtSecurityTokenHandler; la versión completa con claims, JTI e IOptions = 16.4.2
        
        // Obtenemos los roles asociados al usuario para incluirlos dentro de las credenciales del token
        var roles = await userManager.GetRolesAsync(userName);
        
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userName.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, userName.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("username", userName.UserName ?? string.Empty),
            new("role", roles.FirstOrDefault() ?? UserRoles.User),
        };
        // Si user == ADMIN, le añadimos el claim de departamento
        if (roles.Contains(UserRoles.Admin)) 
        {
            claims.Add(new Claim("department", "IT")); 
        }
        
        // Obtenemos y validamos la clave secreta configurada en appsettings.json
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret no configurado"));

        //Configuramos los Claims ==> información del usuario que viaja dentro del token. Ademas de su tiempo de expiración
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddMinutes(15), // El token expira en 15 minutos
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        });
        // Retornamos el token JWT generado junto con el Id del usuario
        return Ok(new { Token = tokenHandler.WriteToken(token), UserId = userName.Id });
    }

    /// <summary>
    /// Devuelve el usuario autenticado actual.
    /// </summary>
    [Authorize] // Exige que la petición incluya un token JWT válido en las cabeceras
    [HttpGet("me")]
    public async Task<ActionResult<AuthDtoResponse>> GetCurrentUser()
    {
        // Extraemos el usuario actual a partir del contexto de seguridad de la petición
        var user = await userManager.GetUserAsync(User);
        if (user is null) {logger.LogWarning("Usuario NO encontrado"); return NotFound();}

        // Consultamos sus roles actuales
        var roles = await userManager.GetRolesAsync(user);
        
        if(user.UserName is null ||  user.Email is null){logger.LogWarning("Usuario NO encontrado"); return NotFound();}
        
        string? department = roles.Contains(UserRoles.Admin) ? User.FindFirst("department")?.Value : null;
        
        // Retornamos un objeto plano con la información de perfil
        return Ok(new AuthDtoResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            roles,
            department, 
            user.CreatedAt,
            user.LastLoginAt
        ));
    }
}