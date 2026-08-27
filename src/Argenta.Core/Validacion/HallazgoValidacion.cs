namespace Argenta.Core.Validacion;

/// <summary>Qué tan grave es un hallazgo de validación.</summary>
public enum SeveridadValidacion
{
    /// <summary>El usuario debe reconocerlo, pero puede continuar.</summary>
    Advertencia,

    /// <summary>Impide generar el reporte hasta que se corrija.</summary>
    Bloqueante,
}

/// <summary>
/// Resultado de evaluar una regla de validación sobre el lote de documentos
/// que se va a procesar (por ejemplo, una factura con datos inconsistentes,
/// o una fecha sin tipo de cambio registrado).
/// </summary>
public sealed record HallazgoValidacion(SeveridadValidacion Severidad, string Mensaje, string? Referencia = null);
