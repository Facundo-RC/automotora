using Automotora.API.DTOs;
using Automotora.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Automotora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VehiculosController : ControllerBase
{
    private readonly IVehiculoService _servicio;

    public VehiculosController(IVehiculoService servicio) => _servicio = servicio;

    /// <summary>Lista vehículos con filtros, orden y paginado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<VehiculoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<VehiculoDto>>> Listar(
        [FromQuery] VehiculoFiltroDto filtro, CancellationToken ct)
    {
        ResultadoPaginado<VehiculoDto> resultado = await _servicio.ListarAsync(filtro, ct);
        return Ok(resultado);
    }

    /// <summary>Devuelve un vehículo por su id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        VehiculoDto vehiculo = await _servicio.ObtenerPorIdAsync(id, ct);
        return Ok(vehiculo);
    }

    /// <summary>Publica un vehículo nuevo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehiculoDto>> Crear([FromBody] VehiculoRequestDto dto, CancellationToken ct)
    {
        VehiculoDto creado = await _servicio.CrearAsync(dto, ct);
        // 201 + cabecera Location apuntando al recurso nuevo.
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    /// <summary>Actualiza todos los datos de un vehículo.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDto>> Actualizar(int id, [FromBody] VehiculoRequestDto dto, CancellationToken ct)
    {
        VehiculoDto actualizado = await _servicio.ActualizarAsync(id, dto, ct);
        return Ok(actualizado);
    }

    /// <summary>Marca el vehículo como vendido o lo vuelve a poner disponible.</summary>
    [HttpPatch("{id:int}/estado-venta")]
    [ProducesResponseType(typeof(VehiculoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDto>> CambiarEstadoVenta(int id, [FromBody] EstadoVentaDto dto, CancellationToken ct)
    {
        VehiculoDto actualizado = await _servicio.CambiarEstadoVentaAsync(id, dto.Vendido, ct);
        return Ok(actualizado);
    }

    /// <summary>Elimina un vehículo.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorRespuesta), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);
        return NoContent();
    }
}
