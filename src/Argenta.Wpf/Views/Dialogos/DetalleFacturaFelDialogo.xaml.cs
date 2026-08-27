using System.Windows;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Wpf.Views.Dialogos;

/// <summary>Muestra una factura FEL completa, de forma visual, como si fuera la factura real (Cambio 6).</summary>
public partial class DetalleFacturaFelDialogo : Window
{
    public DetalleFacturaFelDialogo(DteFel factura)
    {
        InitializeComponent();

        TxtNombreEmisor.Text = factura.NombreEmisor;
        TxtNombreComercial.Text = factura.NombreComercial;
        TxtNombreComercial.Visibility = string.IsNullOrWhiteSpace(factura.NombreComercial) ? Visibility.Collapsed : Visibility.Visible;
        TxtNitEmisor.Text = $"NIT: {factura.NitEmisor}";
        TxtDireccionEmisor.Text = factura.DireccionEmisor;
        TxtDireccionEmisor.Visibility = string.IsNullOrWhiteSpace(factura.DireccionEmisor) ? Visibility.Collapsed : Visibility.Visible;

        TxtTipoDte.Text = factura.TipoDte;
        TxtSerieNumero.Text = $"Serie: {factura.Serie}   No.: {factura.NumeroDte}";
        TxtNumeroAutorizacion.Text = $"Autorización: {factura.NumeroAutorizacion}";
        TxtFechaEmision.Text = $"Emisión: {factura.FechaEmision:dd/MM/yyyy}";
        TxtFechaCertificacion.Text = $"Certificación: {factura.FechaCertificacion:dd/MM/yyyy}";
        TxtMoneda.Text = factura.CodigoMoneda;

        TxtReceptor.Text = $"{factura.NombreReceptor} — NIT: {factura.IdReceptor}";

        GridItems.ItemsSource = factura.Items.Select(item => new ItemDetalle
        {
            NumeroLinea = item.NumeroLinea,
            BienOServicio = string.Equals(item.BienOServicio, "S", StringComparison.OrdinalIgnoreCase) ? "Servicio" : "Bien",
            Cantidad = item.Cantidad,
            Descripcion = item.Descripcion,
            PrecioUnitario = item.PrecioUnitario,
            Total = item.Total,
            ImpuestosTexto = item.Impuestos.Count == 0
                ? "—"
                : string.Join(", ", item.Impuestos.Select(i => $"{i.NombreCorto}: {i.MontoImpuesto:N2}")),
        }).ToList();

        ListaTotalesImpuestos.ItemsSource = factura.TotalImpuestos
            .Select(kv => new TotalImpuestoDetalle { Nombre = kv.Key, Monto = kv.Value })
            .ToList();

        TxtGranTotal.Text = factura.GranTotal.ToString("N2");
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class ItemDetalle
    {
        public int NumeroLinea { get; init; }
        public required string BienOServicio { get; init; }
        public decimal Cantidad { get; init; }
        public required string Descripcion { get; init; }
        public decimal PrecioUnitario { get; init; }
        public decimal Total { get; init; }
        public required string ImpuestosTexto { get; init; }
    }

    private sealed class TotalImpuestoDetalle
    {
        public required string Nombre { get; init; }
        public decimal Monto { get; init; }
    }
}
