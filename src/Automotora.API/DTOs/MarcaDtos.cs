using System.ComponentModel.DataAnnotations;

namespace Automotora.API.DTOs;

public class MarcaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadVehiculos { get; set; }
}

public class MarcaRequestDto
{
    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
