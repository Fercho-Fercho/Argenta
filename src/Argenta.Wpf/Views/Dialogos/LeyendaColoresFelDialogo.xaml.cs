using System.Windows;

namespace Argenta.Wpf.Views.Dialogos;

public partial class LeyendaColoresFelDialogo : Window
{
    public LeyendaColoresFelDialogo()
    {
        InitializeComponent();
        AjustePantalla.Ajustar(this);
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
}
