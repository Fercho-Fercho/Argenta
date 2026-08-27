using ContaSuite.Data.Entidades;

namespace ContaSuite.Wpf.Modelos;

/// <summary>Opción legible del ComboBox de Categoría de proveedor (Normal / Gasolinera / Empresa Eléctrica).</summary>
public sealed record OpcionCategoriaProveedor(CategoriaProveedor Valor, string Texto)
{
    public static readonly OpcionCategoriaProveedor[] Todas =
    [
        new(CategoriaProveedor.Normal, "Normal"),
        new(CategoriaProveedor.Gasolinera, "Gasolinera"),
        new(CategoriaProveedor.EmpresaElectrica, "Empresa Eléctrica"),
    ];

    public static string ATexto(CategoriaProveedor categoria) =>
        Todas.First(o => o.Valor == categoria).Texto;
}
