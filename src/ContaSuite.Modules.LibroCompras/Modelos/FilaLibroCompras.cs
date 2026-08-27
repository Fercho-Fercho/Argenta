using CommunityToolkit.Mvvm.ComponentModel;

namespace ContaSuite.Modules.LibroCompras.Modelos;

/// <summary>Una fila ya clasificada del libro de compras (columnas A a L del reporte final).</summary>
public sealed partial class FilaLibroCompras : ObservableObject
{
    /// <summary>Correlativo (columna A). Nulo para RECI/FPEQ, que no lo llevan en el modelo de libro.</summary>
    public int? Numero { get; set; }

    public required DateTime Fecha { get; init; }
    public required string Docto { get; init; }
    public required string Serie { get; init; }
    public required string NoDoc { get; init; }
    public required string Nit { get; init; }
    public required string Proveedor { get; init; }

    public decimal Compras { get; set; }
    public decimal Servicios { get; set; }
    public decimal Exento { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }

    /// <summary>El proveedor no está en el catálogo: se clasificó como Compra por defecto y debe revisarse.</summary>
    public bool ProveedorNoEncontrado { get; init; }

    /// <summary>Es una nota de crédito (NCRE): los valores ya vienen en negativo.</summary>
    public bool EsNotaCredito { get; init; }

    /// <summary>Proveedor de combustible (o factura con Petróleo > 0): usado en el resumen de IVA Créditos.</summary>
    public bool EsGasolina { get; set; }

    /// <summary>
    /// Factura FEL de origen: solo se llena cuando la fila viene de la pestaña
    /// "Libro de Compras (XML)" (<c>MotorClasificacionFel</c>); queda en null
    /// para el flujo basado en el Excel de la SAT. Se usa para mostrar el
    /// detalle visual de la factura al hacer clic en "Ver".
    /// </summary>
    public DteFel? OrigenFel { get; init; }

    /// <summary>
    /// El NIT del emisor está en el catálogo "Proveedores a revisar" con
    /// acción Revisar (pestaña XML): se resalta en naranja, igual que
    /// <see cref="ProveedorNoEncontrado"/>, pero sin cambiar montos ni el
    /// estado de inclusión.
    /// </summary>
    public bool MarcadoParaRevisar { get; set; }

    /// <summary>
    /// El NIT del emisor está en el catálogo "Proveedores a revisar" con
    /// acción ExcluirSiempre (pestaña XML): la fila arranca excluida
    /// (<see cref="Incluida"/> = false) y se resalta en rojo suave. El
    /// usuario puede volver a marcarla como incluida en cualquier momento.
    /// </summary>
    public bool ExcluidoPorCatalogo { get; set; }

    /// <summary>
    /// Filtro de IVA (solo para FACT/FCAM/NCRE): true si el IVA calculado
    /// (<see cref="CalculoIva"/>) difiere del IVA real de la factura en más del
    /// umbral permitido. Ver <c>MotorClasificacion.UmbralDiferenciaIva</c>.
    /// </summary>
    public bool DescuadreIva { get; set; }

    /// <summary>
    /// IVA teórico (Monto × 12%), solo informativo para la columna "Cálculo de
    /// IVA" de la vista previa. Nulo para documentos a los que no aplica el
    /// filtro (RECI, FPEQ, CIVA, etc.).
    /// </summary>
    public decimal? CalculoIva { get; set; }

    /// <summary>
    /// Si la factura se incluye en el libro generado. El usuario la controla
    /// desde la vista previa (por defecto, todas incluidas). Al generar, las
    /// filas no incluidas se omiten del Excel y de los totales, y el
    /// correlativo se recalcula solo sobre las incluidas.
    /// </summary>
    [ObservableProperty]
    private bool incluida = true;
}
