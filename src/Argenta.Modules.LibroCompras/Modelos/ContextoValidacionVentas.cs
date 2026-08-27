using Argenta.Data.Entidades;

namespace Argenta.Modules.LibroCompras.Modelos;

/// <summary>
/// Contexto para las reglas de validación del libro de Ventas que necesitan
/// saber de qué establecimiento se trata (Tipo/Exporta viven ahí, no en
/// Cliente) además del lote de facturas de ESE establecimiento — a
/// diferencia de las reglas compartidas con compras (tipo de cambio, tipo de
/// DTE, cuadre de ítems), que solo dependen de las facturas.
/// </summary>
public sealed record ContextoValidacionVentas(IReadOnlyList<DteFel> Facturas, Cliente Cliente, Establecimiento Establecimiento);
