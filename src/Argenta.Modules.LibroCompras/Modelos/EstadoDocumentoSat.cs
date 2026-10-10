namespace Argenta.Modules.LibroCompras.Modelos;

/// <summary>
/// Una fila del Excel "Consulta de documentos" que se descarga del portal de
/// la SAT, reducida a lo que el libro necesita de ahí: si el documento quedó
/// anulado (el ZIP de XML no trae ese dato — la anulación es un evento
/// posterior en el SAT que no modifica el XML ya certificado) y los nombres
/// de emisor/receptor, que en el Excel sí vienen con tildes y eñes mientras
/// que en muchos XML la SAT los manda con "?" (ver <see cref="Servicios.ReparadorTildesFel"/>).
/// Se cruza por Número de Autorización.
/// Vive solo en memoria mientras se genera el libro (regla de privacidad).
/// </summary>
public sealed record EstadoDocumentoSat(
    string NumeroAutorizacion,
    bool Anulado,
    string Estado,
    string NombreEmisor = "",
    string NombreReceptor = "");
