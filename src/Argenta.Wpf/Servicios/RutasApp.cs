using System.IO;

namespace Argenta.Wpf.Servicios;

/// <summary>
/// Rutas de archivos locales de la aplicación (perfil del usuario de Windows).
/// Los datos viven en un subdirectorio ("Data") separado de la raíz de
/// instalación de Velopack (%LocalAppData%\Argenta\), porque Velopack borra
/// el contenido no reconocido de esa raíz en instalaciones nuevas — ver
/// <see cref="MigrarDatosCarpetaAnterior"/> para la migración desde la
/// carpeta plana usada antes de este cambio.
/// </summary>
public static class RutasApp
{
    public static string CarpetaDatos { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Argenta", "Data");

    public static string ArchivoBaseDatos => Path.Combine(CarpetaDatos, "argenta.db");

    /// <summary>
    /// Copia la base de datos de instalaciones previas (ContaSuite, o Argenta
    /// sin el subdirectorio "Data") si la base de datos actual todavía no
    /// existe. Debe llamarse una vez al arrancar, antes de abrir la conexión.
    /// </summary>
    public static void MigrarDatosCarpetaAnterior()
    {
        if (File.Exists(ArchivoBaseDatos))
        {
            return;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] candidatos =
        [
            Path.Combine(localAppData, "Argenta", "argenta.db"),
            Path.Combine(localAppData, "ContaSuite", "contasuite.db"),
        ];

        var origen = candidatos.FirstOrDefault(File.Exists);
        if (origen is null)
        {
            return;
        }

        Directory.CreateDirectory(CarpetaDatos);
        File.Copy(origen, ArchivoBaseDatos);
    }
}
