using Automotora.API.DTOs;
using Automotora.API.Data;
using Automotora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Automotora.API.Services;

/// <summary>
/// Toda la lógica de negocio de vehículos. Los controllers no tocan el DbContext:
/// reciben la petición, delegan acá y devuelven el resultado.
/// </summary>
public class VehiculoService : IVehiculoService
{
    private readonly AutomotoraDbContext _db;
    private readonly ILogger<VehiculoService> _logger;

    public VehiculoService(AutomotoraDbContext db, ILogger<VehiculoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ResultadoPaginado<VehiculoDto>> ListarAsync(VehiculoFiltroDto filtro, CancellationToken ct = default)
    {
        // AsNoTracking: es una consulta de sólo lectura, no hace falta que EF siga las entidades.
        IQueryable<Vehiculo> consulta = _db.Vehiculos
            .AsNoTracking()
            .Include(v => v.Marca);

        consulta = AplicarFiltros(consulta, filtro);
        consulta = AplicarOrden(consulta, filtro.OrdenarPor);

        // Se cuenta antes de paginar para saber cuántas páginas hay en total.
        int total = await consulta.CountAsync(ct);

        int pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;

        List<Vehiculo> vehiculos = await consulta
            .Skip((pagina - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .ToListAsync(ct);

        return new ResultadoPaginado<VehiculoDto>
        {
            Items = vehiculos.Select(MapearADto).ToList(),
            Pagina = pagina,
            TamanoPagina = filtro.TamanoPagina,
            TotalItems = total
        };
    }

    public async Task<VehiculoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        Vehiculo vehiculo = await _db.Vehiculos
            .AsNoTracking()
            .Include(v => v.Marca)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new RecursoNoEncontradoException($"No existe un vehículo con id {id}.");

        return MapearADto(vehiculo);
    }

    public async Task<VehiculoDto> CrearAsync(VehiculoRequestDto dto, CancellationToken ct = default)
    {
        await ValidarMarcaAsync(dto.MarcaId, ct);
        await ValidarDuplicadoAsync(dto, null, ct);

        var vehiculo = new Vehiculo
        {
            MarcaId = dto.MarcaId,
            Modelo = dto.Modelo.Trim(),
            Anio = dto.Anio,
            Precio = dto.Precio,
            Kilometraje = dto.Kilometraje,
            Combustible = dto.Combustible,
            Transmision = dto.Transmision,
            Color = dto.Color?.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            ImagenUrl = dto.ImagenUrl?.Trim(),
            Vendido = dto.Vendido,
            FechaPublicacion = DateTime.UtcNow
        };

        _db.Vehiculos.Add(vehiculo);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Vehículo creado con id {Id}", vehiculo.Id);

        return await ObtenerPorIdAsync(vehiculo.Id, ct);
    }

    public async Task<VehiculoDto> ActualizarAsync(int id, VehiculoRequestDto dto, CancellationToken ct = default)
    {
        Vehiculo vehiculo = await _db.Vehiculos.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new RecursoNoEncontradoException($"No existe un vehículo con id {id}.");

        await ValidarMarcaAsync(dto.MarcaId, ct);
        await ValidarDuplicadoAsync(dto, id, ct);

        vehiculo.MarcaId = dto.MarcaId;
        vehiculo.Modelo = dto.Modelo.Trim();
        vehiculo.Anio = dto.Anio;
        vehiculo.Precio = dto.Precio;
        vehiculo.Kilometraje = dto.Kilometraje;
        vehiculo.Combustible = dto.Combustible;
        vehiculo.Transmision = dto.Transmision;
        vehiculo.Color = dto.Color?.Trim();
        vehiculo.Descripcion = dto.Descripcion?.Trim();
        vehiculo.ImagenUrl = dto.ImagenUrl?.Trim();
        vehiculo.Vendido = dto.Vendido;

        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    public async Task<VehiculoDto> CambiarEstadoVentaAsync(int id, bool vendido, CancellationToken ct = default)
    {
        Vehiculo vehiculo = await _db.Vehiculos.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new RecursoNoEncontradoException($"No existe un vehículo con id {id}.");

        vehiculo.Vendido = vendido;
        await _db.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(id, ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        Vehiculo vehiculo = await _db.Vehiculos.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new RecursoNoEncontradoException($"No existe un vehículo con id {id}.");

        _db.Vehiculos.Remove(vehiculo);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Vehículo {Id} eliminado", id);
    }

    // ---------- privados ----------

    private static IQueryable<Vehiculo> AplicarFiltros(IQueryable<Vehiculo> consulta, VehiculoFiltroDto f)
    {
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            string texto = f.Texto.Trim();
            consulta = consulta.Where(v =>
                v.Modelo.Contains(texto) ||
                (v.Descripcion != null && v.Descripcion.Contains(texto)) ||
                (v.Marca != null && v.Marca.Nombre.Contains(texto)));
        }

        if (f.MarcaId.HasValue)
            consulta = consulta.Where(v => v.MarcaId == f.MarcaId.Value);

        if (f.AnioMin.HasValue)
            consulta = consulta.Where(v => v.Anio >= f.AnioMin.Value);

        if (f.AnioMax.HasValue)
            consulta = consulta.Where(v => v.Anio <= f.AnioMax.Value);

        if (f.PrecioMin.HasValue)
            consulta = consulta.Where(v => v.Precio >= f.PrecioMin.Value);

        if (f.PrecioMax.HasValue)
            consulta = consulta.Where(v => v.Precio <= f.PrecioMax.Value);

        if (f.Combustible.HasValue)
            consulta = consulta.Where(v => v.Combustible == f.Combustible.Value);

        if (f.Transmision.HasValue)
            consulta = consulta.Where(v => v.Transmision == f.Transmision.Value);

        if (f.Vendido.HasValue)
            consulta = consulta.Where(v => v.Vendido == f.Vendido.Value);

        return consulta;
    }

    private static IQueryable<Vehiculo> AplicarOrden(IQueryable<Vehiculo> consulta, string? orden) => orden switch
    {
        "precio"      => consulta.OrderBy(v => v.Precio),
        "precio_desc" => consulta.OrderByDescending(v => v.Precio),
        "anio"        => consulta.OrderBy(v => v.Anio),
        "anio_desc"   => consulta.OrderByDescending(v => v.Anio),
        "km"          => consulta.OrderBy(v => v.Kilometraje),
        _             => consulta.OrderByDescending(v => v.FechaPublicacion)
    };

    private async Task ValidarMarcaAsync(int marcaId, CancellationToken ct)
    {
        bool existe = await _db.Marcas.AnyAsync(m => m.Id == marcaId, ct);
        if (!existe)
            throw new ReglaDeNegocioException($"No existe una marca con id {marcaId}.");
    }

    /// <summary>Evita cargar dos veces el mismo auto por error de tipeo.</summary>
    private async Task ValidarDuplicadoAsync(VehiculoRequestDto dto, int? idActual, CancellationToken ct)
    {
        string modelo = dto.Modelo.Trim();

        bool duplicado = await _db.Vehiculos.AnyAsync(v =>
            v.MarcaId == dto.MarcaId &&
            v.Modelo == modelo &&
            v.Anio == dto.Anio &&
            v.Kilometraje == dto.Kilometraje &&
            (idActual == null || v.Id != idActual.Value), ct);

        if (duplicado)
            throw new ReglaDeNegocioException("Ya existe un vehículo cargado con la misma marca, modelo, año y kilometraje.");
    }

    private static VehiculoDto MapearADto(Vehiculo v) => new()
    {
        Id = v.Id,
        MarcaId = v.MarcaId,
        Marca = v.Marca?.Nombre ?? string.Empty,
        Modelo = v.Modelo,
        Anio = v.Anio,
        Precio = v.Precio,
        Kilometraje = v.Kilometraje,
        Combustible = v.Combustible.ToString(),
        Transmision = v.Transmision.ToString(),
        Color = v.Color,
        Descripcion = v.Descripcion,
        ImagenUrl = v.ImagenUrl,
        Vendido = v.Vendido,
        FechaPublicacion = v.FechaPublicacion
    };
}
