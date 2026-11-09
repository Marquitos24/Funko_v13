using Funkos.Dto.Categoria;
using Funkos.Errors;
using Funkos.Helper;
using Funkos.Service.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Funkos.Controller;


[ApiController]
[Route("api/[controller]")]
public class CategoriaController(ICategoriaService service, ILogger<CategoriaController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoriaDtoResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var resultado = await service.GetAllAsync(cancellationToken);
        if (resultado.Value.Count <= 0) return Ok("Aún no hay Categorias");
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : resultado.Error.ToHttpResult<List<CategoriaDtoResponse>>();
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CategoriaWithFunkosResponseDto>> GetByIdAync([FromRoute] string id, CancellationToken cancellationToken)
    {
        var resultado = await service.GetByIdAsync(id, cancellationToken);

        var actionLinks = CategoriaLinkHelper.GenerateLinks(id, "/api/categorias");
        Response.Headers.Append("Link", actionLinks);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : resultado.Error.ToHttpResult<CategoriaWithFunkosResponseDto>();
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CategoriaDtoResponse>> CreateAsync([FromBody] CategoriaCreateDto dto, CancellationToken cancellationToken)
    {
        var resultado = await service.AddAsync(dto, cancellationToken);
        if (resultado.IsFailure)
        {
            return resultado.Error.ToHttpResult<CategoriaDtoResponse>();
        }

        //HETOAS
        var actionLinks = CategoriaLinkHelper.GenerateLinks(resultado.Value.IdY, "/api/categorias");
        Response.Headers.Append("Link", actionLinks);

        return resultado.IsSuccess
            ? CreatedAtAction(nameof(GetByIdAync), new { id = resultado.Value.IdY }, resultado.Value)
            : resultado.Error.ToHttpResult<CategoriaDtoResponse>();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<CategoriaDtoResponse>> UpdateAsync([FromRoute] string id,
        [FromBody] CategoriaUpdateDto dto, CancellationToken cancellationToken)
    {
        var actualizado = await service.UpdateAsync(id, dto, cancellationToken);
        //HETOAS
        var actionLinks = CategoriaLinkHelper.GenerateUpdateLinks(id, "/api/categorias");
        Response.Headers.Append("Link", actionLinks);
        return actualizado.IsSuccess
            ? Ok(actualizado.Value)
            : actualizado.Error.ToHttpResult<CategoriaDtoResponse>();

    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<bool>> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        //HETOAS de Delete
        var actionLinks = CategoriaLinkHelper.GenerateDeleteLinks(id, "/api/categorias");
        Response.Headers.Append("Link", actionLinks);
        if (deleted.IsFailure)
        {
            return deleted.Error.ToHttpResult<bool>();
        }

        return NoContent();
    }
}