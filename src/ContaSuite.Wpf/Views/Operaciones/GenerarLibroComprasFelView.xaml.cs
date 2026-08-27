using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ContaSuite.Wpf.Views.Operaciones;

public partial class GenerarLibroComprasFelView : UserControl
{
    public GenerarLibroComprasFelView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// El DataGrid/ListBox principal y el de "aparte" traen su propio
    /// ScrollViewer interno, que por defecto se queda con la rueda del mouse
    /// aunque no tenga nada que desplazar (bug conocido de WPF): eso hacía
    /// que solo se pudiera hacer scroll de la pantalla parándose justo sobre
    /// la barra visible. Aquí se reenvía la rueda al ScrollPrincipal salvo
    /// que el control interno todavía tenga a dónde desplazarse en esa
    /// dirección, para no romper su propio scroll cuando sí aplica.
    /// </summary>
    private void ReenviarRuedaAlScrollPrincipal(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;

        var scrollInterno = BuscarScrollViewerDescendiente((DependencyObject)sender);
        var puedeConsumirLocalmente = scrollInterno is not null &&
            ((e.Delta > 0 && scrollInterno.VerticalOffset > 0) ||
             (e.Delta < 0 && scrollInterno.VerticalOffset < scrollInterno.ScrollableHeight));

        if (puedeConsumirLocalmente) return;

        ScrollPrincipal.ScrollToVerticalOffset(ScrollPrincipal.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static ScrollViewer? BuscarScrollViewerDescendiente(DependencyObject padre)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(padre); i++)
        {
            var hijo = VisualTreeHelper.GetChild(padre, i);
            if (hijo is ScrollViewer scrollViewer) return scrollViewer;

            var encontrado = BuscarScrollViewerDescendiente(hijo);
            if (encontrado is not null) return encontrado;
        }

        return null;
    }
}
