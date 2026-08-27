using ContaSuite.Data.Entidades;

namespace ContaSuite.Wpf.Modelos;

/// <summary>Opción legible del ComboBox de Tipo de proveedor (Compra / Servicio).</summary>
public sealed record OpcionTipoProveedor(TipoProveedor Valor, string Texto)
{
    public static readonly OpcionTipoProveedor[] Todas =
    [
        new(TipoProveedor.Compra, "Compra"),
        new(TipoProveedor.Servicio, "Servicio"),
    ];

    public static string ATexto(TipoProveedor tipo) =>
        Todas.First(o => o.Valor == tipo).Texto;
}
