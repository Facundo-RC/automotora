using System.Text.Json.Serialization;
using Automotora.API.Data;
using Automotora.API.Middleware;
using Automotora.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Base de datos ----------
string cadenaConexion = builder.Configuration.GetConnectionString("Automotora")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Automotora' en appsettings.json.");

builder.Services.AddDbContext<AutomotoraDbContext>(opciones =>
    opciones.UseSqlServer(cadenaConexion));

// ---------- Servicios de negocio ----------
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IMarcaService, MarcaService>();

// ---------- API ----------
builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        // Los enums viajan como texto ("Nafta") y no como número: el JSON se lee solo.
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Devuelve los errores de validación con el mismo formato que el resto de la API.
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var errores = contexto.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new Automotora.API.DTOs.ErrorRespuesta
        {
            Mensaje = "Hay errores de validación en los datos enviados.",
            Errores = errores
        });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Automotora API",
        Version = "v1",
        Description = "API de gestión de vehículos de una automotora. Proyecto de Facundo Robaño."
    });

    // Incorpora los comentarios /// a la documentación de Swagger.
    string archivoXml = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string rutaXml = Path.Combine(AppContext.BaseDirectory, archivoXml);
    if (File.Exists(rutaXml))
        opciones.IncludeXmlComments(rutaXml);
});

// Permite que el front se sirva desde otro origen durante el desarrollo.
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy("Front", politica =>
        politica.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// El manejador de errores va primero para atrapar todo lo que pase después.
app.UseMiddleware<ManejadorDeErroresMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Automotora API v1");
    });
}

app.UseCors("Front");

// Sirve el front (wwwroot/index.html) en la raíz del sitio.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Aplica las migraciones pendientes y carga datos de ejemplo al iniciar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AutomotoraDbContext>();
    await db.Database.MigrateAsync();
    await DatosDePrueba.SembrarAsync(db);
}

app.Run();
