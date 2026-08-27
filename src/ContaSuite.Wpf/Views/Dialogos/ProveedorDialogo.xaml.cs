using System.Windows;
using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using ContaSuite.Wpf.Modelos;

namespace ContaSuite.Wpf.Views.Dialogos;

public partial class ProveedorDialogo : Window
{
    private readonly Proveedor _proveedor;
    private readonly IReadOnlyCollection<Proveedor> _existentes;

    public ProveedorDialogo(Proveedor proveedor, bool esNuevo, IReadOnlyCollection<Proveedor> existentes)
    {
        InitializeComponent();
        _proveedor = proveedor;
        _existentes = existentes;
        Title = esNuevo ? "Nuevo proveedor" : "Editar proveedor";

        ComboTipo.ItemsSource = OpcionTipoProveedor.Todas;
        ComboCategoria.ItemsSource = OpcionCategoriaProveedor.Todas;
        TxtNit.Text = proveedor.Nit;
        TxtNombre.Text = proveedor.Nombre;

        // En alta nueva no se preselecciona Tipo: el usuario debe elegirlo explícitamente.
        ComboTipo.SelectedItem = esNuevo
            ? null
            : OpcionTipoProveedor.Todas.FirstOrDefault(o => o.Valor == proveedor.Tipo);

        // La categoría sí tiene un valor por defecto (Normal) tanto en alta como en edición.
        ComboCategoria.SelectedItem = OpcionCategoriaProveedor.Todas.FirstOrDefault(o => o.Valor == proveedor.Categoria)
            ?? OpcionCategoriaProveedor.Todas.First(o => o.Valor == CategoriaProveedor.Normal);
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

        if (ComboTipo.SelectedItem is not OpcionTipoProveedor opcion)
        {
            TxtError.Text = "Debe seleccionar el Tipo del proveedor (Compra o Servicio).";
            return;
        }

        if (ComboCategoria.SelectedItem is not OpcionCategoriaProveedor opcionCategoria)
        {
            TxtError.Text = "Debe seleccionar la Categoría del proveedor.";
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
        _proveedor.Tipo = opcion.Valor;
        _proveedor.Categoria = opcionCategoria.Valor;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
