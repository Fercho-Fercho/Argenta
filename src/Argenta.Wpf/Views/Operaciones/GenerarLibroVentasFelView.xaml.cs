using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Argenta.Wpf.Views.Operaciones;

public partial class GenerarLibroVentasFelView : UserControl
{
    public GenerarLibroVentasFelView()
    {
        InitializeComponent();
    }

    /// <summary>Ver el comentario equivalente en GenerarLibroComprasFelView.xaml.cs: reenvía la rueda del mouse al ScrollPrincipal salvo que el control interno todavía tenga a dónde desplazarse.</summary>
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
