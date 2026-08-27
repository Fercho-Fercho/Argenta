namespace ContaSuite.Wpf.Servicios;

/// <summary>Búsqueda y aplicación de actualizaciones mediante Velopack.</summary>
public interface IActualizacionService
{
    /// <summary>Busca, descarga y aplica una actualización si existe. Devuelve un mensaje para mostrar al usuario.</summary>
    Task<string> BuscarYAplicarAsync();
}
