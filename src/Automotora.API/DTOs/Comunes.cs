using Automotora.API.Models;

namespace Automotora.API.DTOs;

/// <summary>Filtros y paginado que acepta el listado de vehículos.</summary>
public class VehiculoFiltroDto
{
    private const int TamanoMaximo = 50;
    private int _tamanoPagina = 12;

    /// <summary>Busca en modelo y descripción.</summary>
    public string? Texto { get; set; }

    public int? MarcaId { get; set; }
    public int? AnioMin { get; set; }
    public int? AnioMax { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public Combustible? Combustible { get; set; }
    public Transmision? Transmision { get; set; }

    /// <summary>Si es null devuelve vendidos y disponibles.</summary>
    public bool? Vendido { get; set; }

    /// <summary>precio | precio_desc | anio | anio_desc | km | reciente (por defecto).</summary>
    public string? OrdenarPor { get; set; }

    public int Pagina { get; set; } = 1;

    public int TamanoPagina
    {
        get => _tamanoPagina;
        // El tope evita que un cliente pida 10.000 filas de una.
        set => _tamanoPagina = value is < 1 or > TamanoMaximo ? TamanoMaximo : value;
    }
}

/// <summary>Respuesta paginada estándar de la API.</summary>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalItems { get; set; }
    public int TotalPaginas => TamanoPagina == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)TamanoPagina);
    public bool HayPaginaSiguiente => Pagina < TotalPaginas;
    public bool HayPaginaAnterior => Pagina > 1;
}

/// <summary>Formato único de error que devuelve toda la API.</summary>
public class ErrorRespuesta
{
    public string Mensaje { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errores { get; set; }
}
