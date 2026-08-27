using ContaSuite.Core.Modulos;
using ContaSuite.Wpf.ViewModels.Operaciones;

namespace ContaSuite.Wpf.Modulos;

/// <summary>Registro del módulo Libro de Ventas ante el shell. Sus elementos de menú se agregan a "Operaciones" junto a los de Compras.</summary>
public sealed class LibroVentasModulo : IModuloContable
{
    public string Id => "libro-ventas";
    public string Nombre => "Libro de Ventas";
    public string Icono => "💰";
    public int Orden => 2;

    public IReadOnlyList<ElementoMenuModulo> ObtenerElementosMenu() =>
    [
        new ElementoMenuModulo { Nombre = "Libro de Ventas (XML)", TipoViewModel = typeof(GenerarLibroVentasFelViewModel) },
    ];
}
