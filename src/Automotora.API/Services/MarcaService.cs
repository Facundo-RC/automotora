using Automotora.API.Data;
using Automotora.API.DTOs;
using Automotora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Automotora.API.Services;

public class MarcaService : IMarcaService
{
    private readonly AutomotoraDbContext _db;

    public MarcaService(AutomotoraDbContext db) => _db = db;

    public async Task<IReadOnlyList<MarcaDto>> ListarAsync(CancellationToken ct = default)
    {
        return await _db.Marcas
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                CantidadVehiculos = m.Vehiculos.Count
            })
            .ToListAsync(ct);
    }

    public async Task<MarcaDto> CrearAsync(MarcaRequestDto dto, CancellationToken ct = default)
    {
        string nombre = dto.Nombre.Trim();

        bool yaExiste = await _db.Marcas.AnyAsync(m => m.Nombre == nombre, ct);
        if (yaExiste)
            throw new ReglaDeNegocioException($"Ya existe una marca llamada '{nombre}'.");

        var marca = new Marca { Nombre = nombre };
        _db.Marcas.Add(marca);
        await _db.SaveChangesAsync(ct);

        return new MarcaDto { Id = marca.Id, Nombre = marca.Nombre, CantidadVehiculos = 0 };
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        Marca marca = await _db.Marcas
            .Include(m => m.Vehiculos)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new RecursoNoEncontradoException($"No existe una marca con id {id}.");

        // No se borra una marca que dejaría vehículos huérfanos.
        if (marca.Vehiculos.Count > 0)
            throw new ReglaDeNegocioException($"No se puede eliminar la marca porque tiene {marca.Vehiculos.Count} vehículo(s) asociado(s).");

        _db.Marcas.Remove(marca);
        await _db.SaveChangesAsync(ct);
    }
}
