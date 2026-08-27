using System.Globalization;
using System.Windows;
using ContaSuite.Data.Entidades;

namespace ContaSuite.Wpf.Views.Dialogos;

public partial class TipoCambioDialogo : Window
{
    private readonly TipoCambio _tipoCambio;

    public TipoCambioDialogo(TipoCambio tipoCambio, bool esNuevo)
    {
        InitializeComponent();
        _tipoCambio = tipoCambio;
        Title = esNuevo ? "Nuevo tipo de cambio" : "Editar tipo de cambio";

        FechaPicker.SelectedDate = tipoCambio.Fecha == default ? DateTime.Today : tipoCambio.Fecha;
        TxtValor.Text = tipoCambio.Valor == 0 ? string.Empty : tipoCambio.Valor.ToString(CultureInfo.InvariantCulture);
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = string.Empty;

        if (FechaPicker.SelectedDate is null)
        {
            TxtError.Text = "Debe seleccionar la fecha.";
            return;
        }

        if (!decimal.TryParse(TxtValor.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) || valor <= 0)
        {
            TxtError.Text = "El valor debe ser un número decimal mayor que 0 (use punto para decimales).";
            return;
        }

        _tipoCambio.Fecha = FechaPicker.SelectedDate.Value.Date;
        _tipoCambio.Valor = valor;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
