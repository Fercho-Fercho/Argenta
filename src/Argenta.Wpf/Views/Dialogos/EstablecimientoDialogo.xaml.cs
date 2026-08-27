using System.Globalization;
using System.Windows;
using Argenta.Data.Entidades;
using Argenta.Wpf.Modelos;

namespace Argenta.Wpf.Views.Dialogos;

public partial class EstablecimientoDialogo : Window
{
    private readonly Establecimiento _establecimiento;
    private readonly IReadOnlyCollection<Establecimiento> _existentes;

    public EstablecimientoDialogo(Establecimiento establecimiento, bool esNuevo, IReadOnlyCollection<Establecimiento> existentes)
    {
        InitializeComponent();
        _establecimiento = establecimiento;
        _existentes = existentes;
        Title = esNuevo ? "Nuevo establecimiento" : "Editar establecimiento";

        ComboTipo.ItemsSource = OpcionTipoCliente.Todas;

        TxtNumero.Text = esNuevo ? string.Empty : establecimiento.Numero.ToString(CultureInfo.InvariantCulture);
        TxtNombre.Text = establecimiento.Nombre;
        ComboTipo.SelectedItem = OpcionTipoCliente.Todas.FirstOrDefault(o => o.Valor == establecimiento.Tipo)
            ?? OpcionTipoCliente.Todas.First(o => o.Valor == TipoCliente.Comercial);
        ChkExporta.IsChecked = establecimiento.Exporta;
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = string.Empty;

        if (!int.TryParse(TxtNumero.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero) || numero <= 0)
        {
            TxtError.Text = "El número de establecimiento debe ser un entero mayor que 0.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtNombre.Text))
        {
            TxtError.Text = "El nombre es obligatorio.";
            return;
        }

        if (ComboTipo.SelectedItem is not OpcionTipoCliente opcionTipo)
        {
            TxtError.Text = "Debe seleccionar el Tipo del establecimiento.";
            return;
        }

        // Comparación por referencia (no por Id): mientras se arma un cliente
        // nuevo puede haber varios establecimientos todavía sin guardar
        // (Id = 0 en todos), así que comparar por Id no distinguiría entre
        // "este mismo establecimiento" y "otro que también es nuevo".
        var duplicado = _existentes.FirstOrDefault(e => !ReferenceEquals(e, _establecimiento) && e.Numero == numero);
        if (duplicado is not null)
        {
            TxtError.Text = $"Ya existe un establecimiento número {numero} en este cliente: \"{duplicado.Nombre}\".";
            return;
        }

        _establecimiento.Numero = numero;
        _establecimiento.Nombre = TxtNombre.Text.Trim();
        _establecimiento.Tipo = opcionTipo.Valor;
        _establecimiento.Exporta = ChkExporta.IsChecked == true;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
