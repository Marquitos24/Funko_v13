using System.Text.Json.Serialization;

namespace Funkos.Dto.Auth;

public record AuthDtoResponse(
    long Id,
    string Username,
    string Email,
    IList<string> Roles,
    // Si es null, se elimina por completo del JSON gracias a esta línea
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Departament,
    DateTime CreatedAt,
    DateTime? LastLoginAt
    );