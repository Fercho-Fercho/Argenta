namespace Argenta.Modules.LibroCompras.Modelos;

/// <summary>
/// Un DTE (factura electrónica FEL) ya parseado desde su XML. Vive solo en
/// memoria mientras se genera el libro: por la regla de privacidad de la
/// suite, esto NUNCA se guarda en la base de datos.
/// </summary>
public sealed class DteFel
{
    public required string TipoDte { get; init; }
    public required string CodigoMoneda { get; init; }

    /// <summary>Fecha de emisión: se usa para ubicar la factura en el libro (columna Fecha).</summary>
    public required DateTime FechaEmision { get; init; }

    /// <summary>Fecha de certificación: se usa SOLO para buscar el tipo de cambio si la factura es en USD.</summary>
    public required DateTime FechaCertificacion { get; init; }

    public required string NitEmisor { get; init; }
    public required string NombreEmisor { get; init; }
    public required string NombreComercial { get; init; }
    public required string DireccionEmisor { get; init; }

    /// <summary>Código de establecimiento del emisor (atributo CodigoEstablecimiento de dte:Emisor). Solo relevante para el libro de Ventas, que se genera un archivo por establecimiento.</summary>
    public required string CodigoEstablecimiento { get; init; }

    public required string IdReceptor { get; init; }
    public required string NombreReceptor { get; init; }

    public required string Serie { get; init; }
    public required string NumeroDte { get; init; }
    public required string NumeroAutorizacion { get; init; }

    public required decimal GranTotal { get; init; }

    /// <summary>true si <c>dte:DatosGenerales</c> trae el atributo <c>Exp="SI"</c> (factura de exportación). Solo relevante para el libro de Ventas.</summary>
    public required bool EsExportacion { get; init; }

    public required IReadOnlyList<ItemFel> Items { get; init; }

    /// <summary>Resumen de impuestos de toda la factura (NombreCorto -> monto total), tal como lo trae el XML.</summary>
    public required IReadOnlyDictionary<string, decimal> TotalImpuestos { get; init; }

    /// <summary>Nombre del archivo .xml dentro del ZIP, para mensajes de error.</summary>
    public required string NombreArchivo { get; init; }

    public string ReferenciaCorta => $"{TipoDte} {Serie}-{NumeroDte}";
}

/// <summary>Una línea (dte:Item) de un DTE: bien o servicio, con sus propios impuestos.</summary>
public sealed class ItemFel
{
    public required int NumeroLinea { get; init; }

    /// <summary>"B" (bien/compra) o "S" (servicio).</summary>
    public required string BienOServicio { get; init; }

    public required decimal Cantidad { get; init; }
    public required string Descripcion { get; init; }
    public required decimal PrecioUnitario { get; init; }

    /// <summary>Total del item, IVA e impuestos especiales incluidos.</summary>
    public required decimal Total { get; init; }

    public required IReadOnlyList<ImpuestoItemFel> Impuestos { get; init; }

    public decimal TotalIva => Impuestos.Where(EsIva).Sum(i => i.MontoImpuesto);

    /// <summary>Impuestos del item que NO son IVA (Petróleo, Tasa Municipal, Turismo Hospedaje, etc.).</summary>
    public decimal TotalEspeciales => Impuestos.Where(i => !EsIva(i)).Sum(i => i.MontoImpuesto);

    /// <summary>Monto neto del item: Total menos IVA menos impuestos especiales.</summary>
    public decimal Neto => Total - TotalIva - TotalEspeciales;

    private static bool EsIva(ImpuestoItemFel impuesto) =>
        string.Equals(impuesto.NombreCorto.Trim(), "IVA", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Un impuesto (dte:Impuesto) dentro de un item.</summary>
public sealed class ImpuestoItemFel
{
    public required string NombreCorto { get; init; }
    public required decimal MontoGravable { get; init; }
    public required decimal MontoImpuesto { get; init; }
}
