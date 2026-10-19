namespace Automotora.API.Services;

/// <summary>El recurso pedido no existe. El middleware la traduce a 404.</summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>La operación es inválida por una regla de negocio. El middleware la traduce a 400.</summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }
}
