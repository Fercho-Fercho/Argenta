namespace Argenta.Wpf.Servicios;

/// <summary>Tema de color de la app.</summary>
public enum TemaApp
{
    Claro,
    Oscuro,
}

/// <summary>
/// Preferencias del usuario/app (no datos de facturas: se guardan sin
/// problema en un archivo local, ver <see cref="IPreferenciasService"/>).
/// Estructura lista para agregar más opciones a futuro (carpeta de salida
/// por defecto, formato de fecha, etc.) sin romper el archivo ya guardado.
/// </summary>
public class PreferenciasApp
{
    public TemaApp Tema { get; set; } = TemaApp.Claro;
}
