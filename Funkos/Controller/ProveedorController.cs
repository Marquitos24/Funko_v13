using Funkos.Dto.Proveedor;
using Funkos.Errors;
using Funkos.Helper;
using Funkos.Service.Proveedor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Funkos.Controller;

[ApiController]
[Route("api/[controller]")]
public class ProveedorController(IProveedorService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<IEnumerable<ProveedorDtoResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
       var resultado = await service.GetAllAsync(cancellationToken);
       if (resultado.IsSuccess && !resultado.Value.Any()) return Ok("Aún no hay Proveedores");
       
       return resultado.IsSuccess 
           ? Ok(resultado.Value)
           : resultado.Error.ToHttpResult<IEnumerable<ProveedorDtoResponse>>();
    }
    
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProveedorDtoResponse>> GetByIdAync(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest("El identificador no tiene un formato válido.");
        
        var resultado = await service.GetByIdAsync(objectId, cancellationToken);
                
        var actionLinks = ProveedorLinkHelper.GenerateLinks(id,"/api/proveedor");
        Response.Headers.Append("Link", actionLinks);
        
        return resultado.IsSuccess 
            ? Ok(resultado.Value)
            : resultado.Error.ToHttpResult<ProveedorDtoResponse>();
    }
    
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProveedorDtoResponse>> CreateAsync([FromBody] ProveedorCreateDto dto, CancellationToken cancellationToken)
    {
        var resultado = await service.AddAsync(dto, cancellationToken);
        if (resultado.IsFailure) { return resultado.Error.ToHttpResult<ProveedorDtoResponse>(); }
        //HETOAS
        var id = resultado.Value.Id;
        
        var actionLinks = ProveedorLinkHelper.GenerateLinks(id,"/api/proveedor");
        Response.Headers.Append("Link", actionLinks);
        
            return resultado.IsSuccess 
                ? CreatedAtAction(nameof(GetByIdAync), new { id}, resultado.Value)
                : resultado.Error.ToHttpResult<ProveedorDtoResponse>();
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<ProveedorDtoResponse>> UpdateAsync([FromRoute] string id, [FromBody] ProveedorUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest("El identificador no tiene un formato válido.");
        
        var actualizado = await service.UpdateAsync(objectId, dto, cancellationToken);
        //HETOAS
        var actionLinks = ProveedorLinkHelper.GenerateUpdateLinks(id,"/api/proveedor");
        Response.Headers.Append("Link", actionLinks);
        return actualizado.IsSuccess 
            ? Ok(actualizado.Value)
            : actualizado.Error.ToHttpResult<ProveedorDtoResponse>();
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<bool>> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest("El identificador no tiene un formato válido.");
        
        var deleted = await service.DeleteAsync(objectId, cancellationToken);
        //HETOAS de Delete
        var actionLinks = ProveedorLinkHelper.GenerateDeleteLinks(id,"/api/proveedor");
        Response.Headers.Append("Link", actionLinks);
        if (deleted.IsFailure) { return deleted.Error.ToHttpResult<bool>(); }
        return NoContent();
    }
}