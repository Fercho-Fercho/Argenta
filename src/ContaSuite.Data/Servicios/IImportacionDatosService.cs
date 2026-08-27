namespace ContaSuite.Data.Servicios;

/// <summary>
/// Importa catálogos (Clientes, Proveedores, Proveedores a revisar, Tipos de
/// Cambio y la memoria de selección) desde un .json, y genera la plantilla
/// vacía correspondiente. Por privacidad, este servicio NUNCA lee ni vuelca
/// datos reales de la base a un archivo — solo importa hacia adentro, o
/// entrega una plantilla en blanco. Ver <see cref="ImportacionDatosService"/>.
/// </summary>
public interface IImportacionDatosService
{
    /// <summary>Parsea el contenido del .json. Lanza <see cref="FormatException"/> con un mensaje claro si el archivo no es válido o la versión no es soportada.</summary>
    DatosContaSuiteJson ParsearJson(string contenidoJson);

    /// <summary>Calcula qué se insertaría/actualizaría, SIN escribir en la base de datos.</summary>
    Task<ResumenImportacion> PrevisualizarAsync(DatosContaSuiteJson datos);

    /// <summary>Aplica la importación: upsert por clave natural, no duplica.</summary>
    Task<ResumenImportacion> ImportarAsync(DatosContaSuiteJson datos);

    /// <summary>Arma el .json de la plantilla vacía (estructura + una fila de ejemplo marcada para borrar). Nunca toca la base de datos.</summary>
    string GenerarPlantillaVacia();
}
