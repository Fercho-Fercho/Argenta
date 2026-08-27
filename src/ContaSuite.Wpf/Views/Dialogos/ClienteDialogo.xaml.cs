using System.Collections.ObjectModel;
using System.Windows;
using ContaSuite.Data.Entidades;

namespace ContaSuite.Wpf.Views.Dialogos;

public partial class ClienteDialogo : Window
{
    private readonly Cliente _cliente;
    private readonly ObservableCollection<Establecimiento> _establecimientos;

    public ClienteDialogo(Cliente cliente, bool esNuevo)
    {
        InitializeComponent();
        _cliente = cliente;
        Title = esNuevo ? "Nuevo cliente" : "Editar cliente";

        TxtNombre.Text = cliente.Nombre;
        TxtNit.Text = cliente.Nit;
        ChkActivo.IsChecked = cliente.Activo;

        _establecimientos = new ObservableCollection<Establecimiento>(cliente.Establecimientos);
        GridEstablecimientos.ItemsSource = _establecimientos;
    }

    private void AgregarEstablecimiento_Click(object sender, RoutedEventArgs e)
    {
        var nuevo = new Establecimiento();
        var dialogo = new EstablecimientoDialogo(nuevo, esNuevo: true, _establecimientos) { Owner = this };
        if (dialogo.ShowDialog() != true) return;

        _establecimientos.Add(nuevo);
    }

    private void EditarEstablecimiento_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not Establecimiento establecimiento) return;

        var dialogo = new EstablecimientoDialogo(establecimiento, esNuevo: false, _establecimientos) { Owner = this };
        if (dialogo.ShowDialog() != true) return;

        // Las columnas son de solo lectura y Establecimiento no notifica
        // cambios (es una entidad EF simple), así que se refresca la grilla
        // a mano para que la fila muestre los valores editados.
        GridEstablecimientos.Items.Refresh();
    }

    private void EliminarEstablecimiento_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not Establecimiento establecimiento) return;

        _establecimientos.Remove(establecimiento);
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(TxtNombre.Text)) { TxtError.Text = "El nombre es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(TxtNit.Text)) { TxtError.Text = "El NIT es obligatorio."; return; }
        if (_establecimientos.Count == 0) { TxtError.Text = "El cliente debe tener al menos un establecimiento."; return; }

        _cliente.Nombre = TxtNombre.Text.Trim();
        _cliente.Nit = TxtNit.Text.Trim();
        _cliente.Activo = ChkActivo.IsChecked == true;
        _cliente.Establecimientos = _establecimientos.ToList();

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
