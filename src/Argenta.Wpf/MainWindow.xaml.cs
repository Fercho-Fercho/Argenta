using System.Windows;
using Argenta.Wpf.Views;

namespace Argenta.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AjustePantalla.Ajustar(this, maximizarSiNoCabe: true);
    }
}
