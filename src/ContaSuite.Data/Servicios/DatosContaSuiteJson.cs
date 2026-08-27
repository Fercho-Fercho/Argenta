namespace ContaSuite.Data.Servicios;

/// <summary>
/// Forma exacta del archivo .json de importación/plantilla de ContaSuite.
/// Los nombres de propiedad se serializan en snake_case (ver
/// <see cref="ImportacionDatosService"/>) para que coincidan con el formato
/// documentado ("proveedores_revisar", "tipos_cambio", etc.).
/// </summary>
public sealed class DatosContaSuiteJson
{
    public int Version { get; set; } = 1;
    public List<DatosClienteJson> Clientes { get; set; } = [];
    public List<DatosProveedorJson> Proveedores { get; set; } = [];
    public List<DatosProveedorRevisarJson> ProveedoresRevisar { get; set; } = [];
    public List<DatosTipoCambioJson> TiposCambio { get; set; } = [];
    public List<DatosSeleccionJson> Selecciones { get; set; } = [];
}

public sealed class DatosClienteJson
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Al menos un establecimiento es obligatorio (ver <see cref="Entidades.Establecimiento"/>).</summary>
    public List<DatosEstablecimientoJson> Establecimientos { get; set; } = [];
}

/// <summary>Tipo: "Profesional" | "Comercial" | "Hotel". Numero es único dentro del cliente, no correlativo.</summary>
public sealed class DatosEstablecimientoJson
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Exporta { get; set; }
}

/// <summary>Tipo: "Compra" | "Servicio". Categoria: "Normal" | "Gasolinera" | "EmpresaElectrica".</summary>
public sealed class DatosProveedorJson
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
}

/// <summary>Accion: "Revisar" | "ExcluirSiempre".</summary>
public sealed class DatosProveedorRevisarJson
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
}

/// <summary>Fecha en formato "yyyy-MM-dd".</summary>
public sealed class DatosTipoCambioJson
{
    public string Fecha { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

/// <summary>
/// TipoLibro: "Compras" | "Ventas". IdentificadorFactura ya es el hash
/// opaco (ver <see cref="Entidades.SeleccionFactura"/>) — nunca el UUID real
/// de la factura, respetando la regla de privacidad de esa tabla.
/// </summary>
public sealed class DatosSeleccionJson
{
    public string NitCliente { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string TipoLibro { get; set; } = string.Empty;
    public string IdentificadorFactura { get; set; } = string.Empty;
    public bool Incluida { get; set; } = true;
}

/// <summary>Resultado de previsualizar o aplicar una importación.</summary>
public sealed record ResumenImportacion(
    int ClientesNuevos, int ClientesActualizados,
    int EstablecimientosNuevos, int EstablecimientosActualizados,
    int ProveedoresNuevos, int ProveedoresActualizados,
    int ProveedoresRevisarNuevos, int ProveedoresRevisarActualizados,
    int TiposCambioNuevos, int TiposCambioActualizados,
    int SeleccionesNuevas, int SeleccionesActualizadas,
    int FilasIgnoradas,
    IReadOnlyList<string> Errores)
{
    public int TotalNuevos => ClientesNuevos + EstablecimientosNuevos + ProveedoresNuevos + ProveedoresRevisarNuevos + TiposCambioNuevos + SeleccionesNuevas;
    public int TotalActualizados => ClientesActualizados + EstablecimientosActualizados + ProveedoresActualizados + ProveedoresRevisarActualizados + TiposCambioActualizados + SeleccionesActualizadas;
}
