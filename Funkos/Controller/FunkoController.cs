using Funkos.Dto.Funko;
using Funkos.Service;
using Microsoft.AspNetCore.Mvc;
using Funkos.Dto;
using Funkos.Errors;
using Funkos.Helper;
using Funkos.Service.Storage;
using Microsoft.AspNetCore.Authorization;

namespace Funkos.Controller;

[ApiController]
[Route("api/[controller]")]
public class FunkoController(IFunkoService service, ILogger<FunkoController> _logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FunkosDtoResponse>>> GetAllAsync([FromQuery] FunkoFilterDto filter,
        CancellationToken cancellationToken)
    {
        var resultado = await service.FindAllPagedAsync(filter, cancellationToken);
        //if (resultado.Value.Count <= 0) { return Ok("Aún no hay Funkos"); }

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : resultado.Error.ToHttpResult<List<FunkosDtoResponse>>();
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<FunkosDtoResponse>> GetByIdAync([FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var resultado = await service.GetByIdAsync(id, cancellationToken);

        var actionLinks = LinkHelper.GenerateLinks(id, "/api/funkos");
        Response.Headers.Append("Link", actionLinks);

        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : resultado.Error.ToHttpResult<FunkosDtoResponse>();
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(FunkosDtoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FunkosDtoResponse>> CreateAsync([FromForm] FunkoCreateDto dto,
        CancellationToken cancellationToken)
    {
        var resultado = await service.AddAsync(dto, cancellationToken);
        if (resultado.IsFailure)
        {
            return resultado.Error.ToHttpResult<FunkosDtoResponse>();
        }

        //HETOAS
        var actionLinks = LinkHelper.GenerateLinks(resultado.Value.Id, "/api/funkos");
        Response.Headers.Append("Link", actionLinks);

        return resultado.IsSuccess
            ? CreatedAtAction(nameof(GetByIdAync), new { id = resultado.Value.Id }, resultado.Value)
            : resultado.Error.ToHttpResult<FunkosDtoResponse>();
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<FunkosDtoResponse>> UpdateAsync([FromRoute] long id, [FromForm] FunkoUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var actualizado = await service.UpdateAsync(id, dto, cancellationToken);
        //HETOAS
        var actionLinks = LinkHelper.GenerateUpdateLinks(id, "/api/funkos");
        Response.Headers.Append("Link", actionLinks);
        return actualizado.IsSuccess
            ? Ok(actualizado.Value)
            : actualizado.Error.ToHttpResult<FunkosDtoResponse>();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<bool>> DeleteAsync([FromRoute] long id, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        //HETOAS de Delete
        var actionLinks = LinkHelper.GenerateDeleteLinks(id, "/api/funkos");
        Response.Headers.Append("Link", actionLinks);
        if (deleted.IsFailure)
        {
            return deleted.Error.ToHttpResult<bool>();
        }

        return NoContent();
    }
}