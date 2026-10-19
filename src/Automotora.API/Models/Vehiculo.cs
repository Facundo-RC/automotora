using System.ComponentModel.DataAnnotations;

namespace Automotora.API.Models;

/// <summary>Vehículo publicado por la automotora.</summary>
public class Vehiculo
{
    public int Id { get; set; }

    [Required]
    public int MarcaId { get; set; }

    public Marca? Marca { get; set; }

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [MaxLength(80)]
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Año de fabricación.</summary>
    public int Anio { get; set; }

    /// <summary>Precio de venta en dólares.</summary>
    public decimal Precio { get; set; }

    /// <summary>Kilómetros recorridos.</summary>
    public int Kilometraje { get; set; }

    public Combustible Combustible { get; set; }

    public Transmision Transmision { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [MaxLength(300)]
    public string? ImagenUrl { get; set; }

    /// <summary>Marca el vehículo como vendido sin borrarlo del historial.</summary>
    public bool Vendido { get; set; }

    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;
}
