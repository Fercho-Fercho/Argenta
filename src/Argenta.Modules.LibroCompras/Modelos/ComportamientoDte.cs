namespace Argenta.Modules.LibroCompras.Modelos;

/// <summary>
/// Cómo afecta un tipo de DTE al libro de compras (fuente XML/FEL). Ver
/// <see cref="ComportamientoDteCatalogo"/> para la tabla completa de tipos.
/// </summary>
public enum ComportamientoDte
{
    /// <summary>Grupo A: normal con IVA. El neto va a Compras/Servicios según el ítem, IVA a Iva, impuestos especiales a Exento.</summary>
    Afecta,

    /// <summary>Grupo B: no afecta. El Gran Total completo va a Exento; Compras, Servicios e Iva quedan en 0.</summary>
    NoAfecta,

    /// <summary>Grupo C: igual que Afecta, pero todos los valores de la fila quedan en negativo.</summary>
    Resta,

    /// <summary>Grupo D: se lista aparte, en una tabla separada al final del libro; no suma en los totales.</summary>
    Aparte,
}

/// <summary>
/// Tabla de referencia de comportamiento por tipo de DTE (columna "Tipo" de
/// <c>dte:DatosGenerales</c>). Para agregar un tipo nuevo, o cambiar el grupo
/// de uno existente, basta con editar <see cref="Mapa"/>.
///
/// Tabla completa:
/// <code>
/// FACT Factura                                                → AFECTA (IVA)
/// FCAM Factura Cambiaria                                       → AFECTA (IVA)
/// FCAP Factura Cambiaria Pequeño Contribuyente                 → AFECTA (IVA)
/// FESP Factura Especial                                        → AFECTA (IVA)
/// NDEB Nota de Débito                                          → AFECTA (IVA)
/// NCRE Nota de Crédito                                         → RESTA (negativo)
/// NABN Nota de Abono                                           → APARTE (abajo)
/// CIVA Constancia de Exención de IVA                           → APARTE (abajo)
/// FPEQ Factura Pequeño Contribuyente                           → NO AFECTA (Exento)
/// RDON Recibo de Donación                                      → NO AFECTA (Exento)
/// RECI Recibo                                                  → NO AFECTA (Exento)
/// FACA Factura Contribuyente Agropecuario                      → NO AFECTA (Exento)
/// FCCA Factura Cambiaria Contribuyente Agropecuario            → NO AFECTA (Exento)
/// FAPE Factura Pequeño Contribuyente Régimen Electrónico       → NO AFECTA (Exento)
/// FCPE Factura Cambiaria Pequeño Contrib. Régimen Electrónico  → NO AFECTA (Exento)
/// FAAE Factura Contrib. Agropecuario Rég. Electrónico Especial → NO AFECTA (Exento)
/// FCAE Factura Cambiaria Contrib. Agrop. Rég. Elec. Especial   → NO AFECTA (Exento)
/// CAIS Constancia de Adquisición de Insumos y Servicios        → NO AFECTA (Exento)
/// NEV  Nota de Envío                                           → NO AFECTA (Exento)
/// RANT Recibo de Anticipo                                      → NO AFECTA (Exento)
/// FACP Factura Provisional                                     → NO AFECTA (Exento)
/// FEPE Factura Específica                                      → NO AFECTA (Exento)
/// FARP Factura Contribuyente Régimen Primario                  → NO AFECTA (Exento)
/// FCRP Factura Cambiaria Contribuyente Régimen Primario        → NO AFECTA (Exento)
/// FPEC Factura Contribuyente Régimen Pecuario                  → NO AFECTA (Exento)
/// FCPC Factura Cambiaria Contribuyente Régimen Pecuario        → NO AFECTA (Exento)
/// </code>
/// </summary>
public static class ComportamientoDteCatalogo
{
    private static readonly Dictionary<string, ComportamientoDte> Mapa = new(StringComparer.OrdinalIgnoreCase)
    {
        // Grupo A — AFECTA (normal con IVA)
        ["FACT"] = ComportamientoDte.Afecta,
        ["FCAM"] = ComportamientoDte.Afecta,
        ["FCAP"] = ComportamientoDte.Afecta,
        ["FESP"] = ComportamientoDte.Afecta,
        ["NDEB"] = ComportamientoDte.Afecta,

        // Grupo C — RESTA (negativo)
        ["NCRE"] = ComportamientoDte.Resta,

        // Grupo D — APARTE (tabla separada, no suma)
        ["NABN"] = ComportamientoDte.Aparte,
        ["CIVA"] = ComportamientoDte.Aparte,

        // Grupo B — NO AFECTA (Gran Total completo a Exento)
        ["FPEQ"] = ComportamientoDte.NoAfecta,
        ["RDON"] = ComportamientoDte.NoAfecta,
        ["RECI"] = ComportamientoDte.NoAfecta,
        ["FACA"] = ComportamientoDte.NoAfecta,
        ["FCCA"] = ComportamientoDte.NoAfecta,
        ["FAPE"] = ComportamientoDte.NoAfecta,
        ["FCPE"] = ComportamientoDte.NoAfecta,
        ["FAAE"] = ComportamientoDte.NoAfecta,
        ["FCAE"] = ComportamientoDte.NoAfecta,
        ["CAIS"] = ComportamientoDte.NoAfecta,
        ["NEV"] = ComportamientoDte.NoAfecta,
        ["RANT"] = ComportamientoDte.NoAfecta,
        ["FACP"] = ComportamientoDte.NoAfecta,
        ["FEPE"] = ComportamientoDte.NoAfecta,
        ["FARP"] = ComportamientoDte.NoAfecta,
        ["FCRP"] = ComportamientoDte.NoAfecta,
        ["FPEC"] = ComportamientoDte.NoAfecta,
        ["FCPC"] = ComportamientoDte.NoAfecta,
    };

    /// <summary>True si el tipo de DTE está en la tabla de referencia conocida.</summary>
    public static bool EsConocido(string tipoDte) => Mapa.ContainsKey((tipoDte ?? string.Empty).Trim());

    /// <summary>
    /// Comportamiento del tipo de DTE. Si no está en la tabla (tipo no
    /// reconocido), se trata como Grupo A (Afecta) por defecto, para no
    /// romper las sumas del libro; el llamador debe advertir aparte
    /// (ver <c>ValidacionTipoDteFel</c>).
    /// </summary>
    public static ComportamientoDte Obtener(string tipoDte) =>
        Mapa.TryGetValue((tipoDte ?? string.Empty).Trim(), out var comportamiento) ? comportamiento : ComportamientoDte.Afecta;
}
