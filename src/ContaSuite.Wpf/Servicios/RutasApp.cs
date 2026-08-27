using System.IO;

namespace ContaSuite.Wpf.Servicios;

/// <summary>Rutas de archivos locales de la aplicación (perfil del usuario de Windows).</summary>
public static class RutasApp
{
    public static string CarpetaDatos { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ContaSuite");

    public static string ArchivoBaseDatos => Path.Combine(CarpetaDatos, "contasuite.db");
}
