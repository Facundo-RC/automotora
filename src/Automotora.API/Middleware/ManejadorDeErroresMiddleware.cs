using System.Text.Json;
using Automotora.API.DTOs;
using Automotora.API.Services;

namespace Automotora.API.Middleware;

/// <summary>
/// Centraliza el manejo de errores: cualquier excepción que suba desde un service
/// sale como JSON con el mismo formato, y nunca se le muestra el stack trace al cliente.
/// </summary>
public class ManejadorDeErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorDeErroresMiddleware> _logger;

    public ManejadorDeErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorDeErroresMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (RecursoNoEncontradoException ex)
        {
            await EscribirRespuestaAsync(contexto, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ReglaDeNegocioException ex)
        {
            await EscribirRespuestaAsync(contexto, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", contexto.Request.Path);
            await EscribirRespuestaAsync(contexto, StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado. Volvé a intentar en unos minutos.");
        }
    }

    private static async Task EscribirRespuestaAsync(HttpContext contexto, int codigo, string mensaje)
    {
        if (contexto.Response.HasStarted)
            return;

        contexto.Response.Clear();
        contexto.Response.StatusCode = codigo;
        contexto.Response.ContentType = "application/json; charset=utf-8";

        string json = JsonSerializer.Serialize(
            new ErrorRespuesta { Mensaje = mensaje },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await contexto.Response.WriteAsync(json);
    }
}
