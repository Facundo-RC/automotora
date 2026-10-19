using System.ComponentModel.DataAnnotations;

namespace Automotora.API.Models;

/// <summary>Marca de vehículo (Chevrolet, Fiat, Toyota...).</summary>
public class Marca
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Vehículos asociados a la marca.</summary>
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
