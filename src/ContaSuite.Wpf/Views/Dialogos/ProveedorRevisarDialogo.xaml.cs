using System.Windows;
using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using ContaSuite.Wpf.Modelos;

namespace ContaSuite.Wpf.Views.Dialogos;

public partial class ProveedorRevisarDialogo : Window
{
    private readonly ProveedorRevisar _proveedor;
    private readonly IReadOnlyCollection<ProveedorRevisar> _existentes;

    public ProveedorRevisarDialogo(ProveedorRevisar proveedor, bool esNuevo, IReadOnlyCollection<ProveedorRevisar> existentes)
    {
        InitializeComponent();
        _proveedor = proveedor;
        _existentes = existentes;
        Title = esNuevo ? "Nuevo proveedor a revisar" : "Editar proveedor a revisar";

        ComboAccion.ItemsSource = OpcionAccionRevisar.Todas;
        TxtNit.Text = proveedor.Nit;
        TxtNombre.Text = proveedor.Nombre;

        ComboAccion.SelectedItem = OpcionAccionRevisar.Todas.FirstOrDefault(o => o.Valor == proveedor.Accion)
            ?? OpcionAccionRevisar.Todas.First(o => o.Valor == AccionRevisar.Revisar);
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(TxtNit.Text))
        {
            TxtError.Text = "El NIT es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            TxtError.Text = "El nombre es obligatorio.";
            return;
        }

        if (ComboAccion.SelectedItem is not OpcionAccionRevisar opcionAccion)
        {
            TxtError.Text = "Debe seleccionar la Acción.";
            return;
        }

        var nitNormalizado = NitUtil.Normalizar(TxtNit.Text);
        var duplicado = _existentes.FirstOrDefault(p => p.Id != _proveedor.Id && p.Nit == nitNormalizado);
        if (duplicado is not null)
        {
            TxtError.Text = $"Ya existe un proveedor con ese NIT: \"{duplicado.Nombre}\".";
            return;
        }

        _proveedor.Nit = TxtNit.Text.Trim();
        _proveedor.Nombre = TxtNombre.Text.Trim();
        _proveedor.Accion = opcionAccion.Valor;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
