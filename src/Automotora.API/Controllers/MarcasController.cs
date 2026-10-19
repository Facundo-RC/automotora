using Automotora.API.DTOs;
using Automotora.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Automotora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MarcasController : ControllerBase
{
    private readonly IMarcaService _servicio;

    public MarcasController(IMarcaService servicio) => _servicio = servicio;

    /// <summary>Lista las marcas con la cantidad de vehículos de cada una.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MarcaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MarcaDto>>> Listar(CancellationToken ct)
        => Ok(await _servicio.ListarAsync(ct));

    /// <summary>Crea una marca nueva.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MarcaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MarcaDto>> Crear([FromBody] MarcaRequestDto dto, CancellationToken ct)
    {
        MarcaDto creada = await _servicio.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(Listar), new { id = creada.Id }, creada);
    }

    /// <summary>Elimina una marca que no tenga vehículos asociados.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);
        return NoContent();
    }
}
