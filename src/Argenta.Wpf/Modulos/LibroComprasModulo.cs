using Argenta.Core.Modulos;
using Argenta.Wpf.ViewModels.Operaciones;

namespace Argenta.Wpf.Modulos;

/// <summary>
/// Registro del módulo Libro de Compras ante el shell. Para agregar un módulo
/// nuevo (por ejemplo Libro de Ventas) se crea una clase equivalente que
/// implemente <see cref="IModuloContable"/> y se registra en
/// <c>App.xaml.cs</c>; no requiere tocar esta clase.
/// </summary>
public sealed class LibroComprasModulo : IModuloContable
{
    public string Id => "libro-compras";
    public string Nombre => "Libro de Compras";
    public string Icono => "🧾";
    public int Orden => 1;

    // "Generar libro de compras" (basado en Excel de la SAT) se quitó del
    // menú: quedó sustituido por "Libro de Compras (XML)". GenerarLibroComprasViewModel/View
    // siguen intactos (GeneradorLibroComprasXlsx y MotorClasificacion los sigue usando la pestaña XML).
    public IReadOnlyList<ElementoMenuModulo> ObtenerElementosMenu() =>
    [
        new ElementoMenuModulo { Nombre = "Libro de Compras (XML)", TipoViewModel = typeof(GenerarLibroComprasFelViewModel) },
    ];
}
