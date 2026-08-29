namespace Argenta.Data.Servicios;

/// <summary>
/// Importa/exporta catálogos (Clientes, Proveedores, Proveedores a revisar)
/// en el mismo formato .json en ambos sentidos, y genera la plantilla vacía
/// correspondiente. Por privacidad, este servicio NUNCA lee ni vuelca
/// información de facturas ni la memoria de selección (hashes de facturas
/// incluidas/excluidas) — <see cref="ExportarAsync"/> solo entrega catálogo.
/// Ver <see cref="ImportacionDatosService"/>.
/// </summary>
public interface IImportacionDatosService
{
    /// <summary>Parsea el contenido del .json. Lanza <see cref="FormatException"/> con un mensaje claro si el archivo no es válido o la versión no es soportada.</summary>
    DatosArgentaJson ParsearJson(string contenidoJson);

    /// <summary>Calcula qué se insertaría/actualizaría, SIN escribir en la base de datos.</summary>
    Task<ResumenImportacion> PrevisualizarAsync(DatosArgentaJson datos);

    /// <summary>Aplica la importación: upsert por clave natural, no duplica.</summary>
    Task<ResumenImportacion> ImportarAsync(DatosArgentaJson datos);

    /// <summary>Arma el .json de la plantilla vacía (estructura + una fila de ejemplo marcada para borrar). Nunca toca la base de datos.</summary>
    string GenerarPlantillaVacia();

    /// <summary>
    /// Arma el .json con los catálogos actuales (Clientes + Establecimientos,
    /// Proveedores, Proveedores a revisar), en el mismo formato que
    /// <see cref="ImportarAsync"/> consume — lo exportado se puede volver a
    /// importar sin cambios. Operación de solo lectura: no modifica la base
    /// de datos. Tipos de Cambio y la memoria de selección siempre salen
    /// vacíos (no son catálogo propio del contador / son datos de facturas).
    /// </summary>
    Task<string> ExportarAsync();
}
