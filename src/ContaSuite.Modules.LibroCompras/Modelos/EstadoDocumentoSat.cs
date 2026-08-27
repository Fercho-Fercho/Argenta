namespace ContaSuite.Modules.LibroCompras.Modelos;

/// <summary>
/// Una fila del Excel "Consulta de documentos" que se descarga del portal de
/// la SAT, reducida a lo único que el libro de Ventas necesita de ahí: si el
/// documento quedó anulado. El ZIP de XML (FEL) no trae ese dato — la
/// anulación es un evento posterior en el SAT que no modifica el XML ya
/// certificado — así que hay que cruzarlo aparte por Número de Autorización.
/// Vive solo en memoria mientras se genera el libro (regla de privacidad).
/// </summary>
public sealed record EstadoDocumentoSat(string NumeroAutorizacion, bool Anulado, string Estado);
