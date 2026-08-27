using System.Globalization;
using System.Windows;
using ContaSuite.Wpf.Modelos;

namespace ContaSuite.Wpf.Views.Dialogos;

public partial class FolioInicialDialogo : Window
{
    private readonly List<FolioInicialFila> _filas;

    public FolioInicialDialogo(IReadOnlyList<FolioInicialFila> filas)
    {
        InitializeComponent();
        _filas = filas.ToList();
        ListaFilas.ItemsSource = _filas;
    }

    private void Continuar_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = string.Empty;

        foreach (var fila in _filas)
        {
            if (!int.TryParse(fila.FolioTexto.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var folio) || folio <= 0)
            {
                TxtError.Text = $"El folio inicial de \"{fila.Etiqueta}\" debe ser un número entero mayor que 0.";
                return;
            }

            fila.FolioInicial = folio;
        }

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
