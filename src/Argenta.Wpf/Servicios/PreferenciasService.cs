using System.IO;
using System.Text.Json;

namespace Argenta.Wpf.Servicios;

/// <summary>
/// Guarda las preferencias en un archivo JSON en la carpeta de datos local
/// de la app (misma carpeta que la base de datos SQLite, ver RutasApp), no
/// en la base de datos: no son datos de facturas ni catálogos, son
/// configuración de la app/usuario.
/// </summary>
public class PreferenciasService : IPreferenciasService
{
    private static readonly string RutaArchivo = Path.Combine(RutasApp.CarpetaDatos, "preferencias.json");
    private static readonly JsonSerializerOptions OpcionesJson = new() { WriteIndented = true };

    public PreferenciasApp Cargar()
    {
        if (!File.Exists(RutaArchivo)) return new PreferenciasApp();

        try
        {
            var json = File.ReadAllText(RutaArchivo);
            return JsonSerializer.Deserialize<PreferenciasApp>(json) ?? new PreferenciasApp();
        }
        catch
        {
            // Archivo corrupto o de una versión futura incompatible: mejor
            // arrancar con los valores por defecto que impedir abrir la app.
            return new PreferenciasApp();
        }
    }

    public void Guardar(PreferenciasApp preferencias)
    {
        Directory.CreateDirectory(RutasApp.CarpetaDatos);
        var json = JsonSerializer.Serialize(preferencias, OpcionesJson);
        File.WriteAllText(RutaArchivo, json);
    }
}
