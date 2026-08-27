namespace Argenta.Modules.LibroCompras.Modelos;

/// <summary>
/// Una fila del Excel de facturas recibidas que se descarga del portal de la
/// SAT, ya tipada. Vive solo en memoria mientras se genera el libro: por la
/// regla de privacidad de la suite, esto NUNCA se guarda en la base de datos.
/// </summary>
public sealed class FacturaSat
{
    /// <summary>Número de fila en el Excel original (para mensajes de validación).</summary>
    public required int NumeroFilaExcel { get; init; }

    public required DateTime FechaEmision { get; init; }
    public required string TipoDte { get; init; }
    public required string Serie { get; init; }
    public required string NumeroDte { get; init; }
    public required string ClasificacionEmisor { get; init; }
    public required string Exportacion { get; init; }
    public required string UbicacionTemporal { get; init; }
    public required string NitEmisor { get; init; }
    public required string NombreEmisor { get; init; }
    public required string IdReceptor { get; init; }
    public required string NombreReceptor { get; init; }
    public required string Estado { get; init; }
    public required string Moneda { get; init; }

    public required decimal GranTotal { get; init; }
    public required decimal Iva { get; init; }
    public required decimal Petroleo { get; init; }
    public required decimal TurismoHospedaje { get; init; }
    public required decimal TurismoPasajes { get; init; }
    public required decimal TimbrePrensa { get; init; }
    public required decimal Bomberos { get; init; }
    public required decimal TasaMunicipal { get; init; }
    public required decimal BebidasAlcoholicas { get; init; }
    public required decimal Tabaco { get; init; }
    public required decimal Cemento { get; init; }
    public required decimal BebidasNoAlcoholicas { get; init; }
    public required decimal TarifaPortuaria { get; init; }

    /// <summary>Suma de todos los impuestos/tasas distintos del IVA y el Petróleo.</summary>
    public decimal OtrosImpuestos =>
        TurismoHospedaje + TurismoPasajes + TimbrePrensa + Bomberos + TasaMunicipal +
        BebidasAlcoholicas + Tabaco + Cemento + BebidasNoAlcoholicas + TarifaPortuaria;

    public bool EsAnulada => string.Equals(Estado?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase);

    public string ReferenciaCorta => $"{TipoDte} {Serie}-{NumeroDte}";
}
