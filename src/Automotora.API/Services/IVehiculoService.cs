using Automotora.API.DTOs;

namespace Automotora.API.Services;

public interface IVehiculoService
{
    Task<ResultadoPaginado<VehiculoDto>> ListarAsync(VehiculoFiltroDto filtro, CancellationToken ct = default);
    Task<VehiculoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<VehiculoDto> CrearAsync(VehiculoRequestDto dto, CancellationToken ct = default);
    Task<VehiculoDto> ActualizarAsync(int id, VehiculoRequestDto dto, CancellationToken ct = default);
    Task<VehiculoDto> CambiarEstadoVentaAsync(int id, bool vendido, CancellationToken ct = default);
    Task EliminarAsync(int id, CancellationToken ct = default);
}

public interface IMarcaService
{
    Task<IReadOnlyList<MarcaDto>> ListarAsync(CancellationToken ct = default);
    Task<MarcaDto> CrearAsync(MarcaRequestDto dto, CancellationToken ct = default);
    Task EliminarAsync(int id, CancellationToken ct = default);
}
