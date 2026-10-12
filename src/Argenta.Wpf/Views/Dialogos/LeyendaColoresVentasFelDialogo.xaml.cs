using System.Windows;

namespace Argenta.Wpf.Views.Dialogos;

public partial class LeyendaColoresVentasFelDialogo : Window
{
    public LeyendaColoresVentasFelDialogo()
    {
        InitializeComponent();
        AjustePantalla.Ajustar(this);
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}
