using Automotora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Automotora.API.Data;

/// <summary>Carga vehículos de ejemplo la primera vez, para que la app no arranque vacía.</summary>
public static class DatosDePrueba
{
    public static async Task SembrarAsync(AutomotoraDbContext db)
    {
        if (await db.Vehiculos.AnyAsync())
            return;

        var vehiculos = new List<Vehiculo>
        {
            new() { MarcaId = 1, Modelo = "Onix LT", Anio = 2021, Precio = 17500m, Kilometraje = 42000,
                    Combustible = Combustible.Nafta, Transmision = Transmision.Manual, Color = "Blanco",
                    Descripcion = "Único dueño, service oficial al día." },
            new() { MarcaId = 2, Modelo = "Cronos Drive", Anio = 2022, Precio = 19900m, Kilometraje = 28500,
                    Combustible = Combustible.Nafta, Transmision = Transmision.Manual, Color = "Gris" },
            new() { MarcaId = 3, Modelo = "Ranger XLS", Anio = 2019, Precio = 34500m, Kilometraje = 96000,
                    Combustible = Combustible.Diesel, Transmision = Transmision.Automatica, Color = "Negro",
                    Descripcion = "4x4, cubiertas nuevas." },
            new() { MarcaId = 4, Modelo = "208 Allure", Anio = 2023, Precio = 22300m, Kilometraje = 15400,
                    Combustible = Combustible.Nafta, Transmision = Transmision.Automatica, Color = "Azul" },
            new() { MarcaId = 5, Modelo = "Kangoo Express", Anio = 2018, Precio = 14200m, Kilometraje = 132000,
                    Combustible = Combustible.Diesel, Transmision = Transmision.Manual, Color = "Blanco",
                    Descripcion = "Utilitario, ideal reparto." },
            new() { MarcaId = 6, Modelo = "Vitara GLX", Anio = 2020, Precio = 26800m, Kilometraje = 61000,
                    Combustible = Combustible.Nafta, Transmision = Transmision.Automatica, Color = "Rojo" },
            new() { MarcaId = 7, Modelo = "Corolla XEI", Anio = 2022, Precio = 31500m, Kilometraje = 34000,
                    Combustible = Combustible.Hibrido, Transmision = Transmision.Automatica, Color = "Plata",
                    Descripcion = "Híbrido, muy bajo consumo." },
            new() { MarcaId = 8, Modelo = "Polo Comfortline", Anio = 2021, Precio = 21400m, Kilometraje = 47000,
                    Combustible = Combustible.Nafta, Transmision = Transmision.Manual, Color = "Gris" },
            new() { MarcaId = 7, Modelo = "Hilux SRV", Anio = 2017, Precio = 38900m, Kilometraje = 148000,
                    Combustible = Combustible.Diesel, Transmision = Transmision.Automatica, Color = "Gris",
                    Vendido = true },
            new() { MarcaId = 1, Modelo = "Spin LTZ", Anio = 2016, Precio = 15900m, Kilometraje = 121000,
                    Combustible = Combustible.Nafta, Transmision = Transmision.Manual, Color = "Negro" }
        };

        // Fechas escalonadas para que el orden "más reciente" tenga sentido.
        for (int i = 0; i < vehiculos.Count; i++)
            vehiculos[i].FechaPublicacion = DateTime.UtcNow.AddDays(-i * 3);

        db.Vehiculos.AddRange(vehiculos);
        await db.SaveChangesAsync();
    }
}
