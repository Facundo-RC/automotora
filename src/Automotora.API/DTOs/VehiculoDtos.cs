using System.ComponentModel.DataAnnotations;
using Automotora.API.Models;

namespace Automotora.API.DTOs;

/// <summary>Lo que la API devuelve al cliente. No expone la entidad de base directamente.</summary>
public class VehiculoDto
{
    public int Id { get; set; }
    public int MarcaId { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public decimal Precio { get; set; }
    public int Kilometraje { get; set; }
    public string Combustible { get; set; } = string.Empty;
    public string Transmision { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Vendido { get; set; }
    public DateTime FechaPublicacion { get; set; }
}

/// <summary>Datos que se reciben para crear o actualizar un vehículo.</summary>
public class VehiculoRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "La marca es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La marca indicada no es válida.")]
    public int MarcaId { get; set; }

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "El modelo no puede superar los 80 caracteres.")]
    public string Modelo { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100.")]
    public int Anio { get; set; }

    [Range(0.01, 99999999, ErrorMessage = "El precio debe ser mayor a cero.")]
    public decimal Precio { get; set; }

    [Range(0, 2000000, ErrorMessage = "El kilometraje no puede ser negativo.")]
    public int Kilometraje { get; set; }

    [EnumDataType(typeof(Combustible), ErrorMessage = "El combustible indicado no es válido.")]
    public Combustible Combustible { get; set; }

    [EnumDataType(typeof(Transmision), ErrorMessage = "La transmisión indicada no es válida.")]
    public Transmision Transmision { get; set; }

    [StringLength(40)]
    public string? Color { get; set; }

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    public string? Descripcion { get; set; }

    [StringLength(300)]
    [Url(ErrorMessage = "La URL de la imagen no tiene un formato válido.")]
    public string? ImagenUrl { get; set; }

    public bool Vendido { get; set; }

    /// <summary>Validación que no se puede expresar con un atributo suelto.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Anio > DateTime.UtcNow.Year + 1)
        {
            yield return new ValidationResult(
                "El año no puede ser posterior al próximo año.",
                new[] { nameof(Anio) });
        }
    }
}

/// <summary>Cuerpo del PATCH que marca un vehículo como vendido o disponible.</summary>
public class EstadoVentaDto
{
    public bool Vendido { get; set; }
}
