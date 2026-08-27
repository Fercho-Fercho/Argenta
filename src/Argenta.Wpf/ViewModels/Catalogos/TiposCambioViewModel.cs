using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Data.Entidades;
using Argenta.Data.Repositorios;
using Argenta.Modules.LibroCompras.Servicios;
using Argenta.Wpf.Views.Dialogos;
using Microsoft.Win32;

namespace Argenta.Wpf.ViewModels.Catalogos;

/// <summary>CRUD de Tipo de Cambio + importación (upsert) del CSV del Banguat.</summary>
public partial class TiposCambioViewModel : ObservableObject
{
    private readonly ITipoCambioRepositorio _repositorio;
    private readonly LectorTipoCambioBanguat _lectorBanguat;
    private readonly ICollectionView _vista;

    public ObservableCollection<TipoCambio> TiposCambio { get; } = [];

    [ObservableProperty]
    private string? mensaje;

    [ObservableProperty]
    private string? textoBusqueda;

    public TiposCambioViewModel(ITipoCambioRepositorio repositorio, LectorTipoCambioBanguat lectorBanguat)
    {
        _repositorio = repositorio;
        _lectorBanguat = lectorBanguat;
        _vista = CollectionViewSource.GetDefaultView(TiposCambio);
        _vista.Filter = FiltrarTipoCambio;
        _ = CargarAsync();
    }

    partial void OnTextoBusquedaChanged(string? value) => _vista.Refresh();

    private bool FiltrarTipoCambio(object obj)
    {
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return true;
        if (obj is not TipoCambio tipoCambio) return false;

        return tipoCambio.Fecha.ToString("dd/MM/yyyy").Contains(TextoBusqueda.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        TiposCambio.Clear();
        foreach (var tc in await _repositorio.ObtenerTodosAsync())
        {
            TiposCambio.Add(tc);
        }
    }

    [RelayCommand]
    private async Task AgregarAsync()
    {
        var nuevo = new TipoCambio { Fecha = DateTime.Today };
        var dialogo = new TipoCambioDialogo(nuevo, esNuevo: true) { Owner = Application.Current.MainWindow };

        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(nuevo);
        Mensaje = $"Tipo de cambio del {nuevo.Fecha:dd/MM/yyyy} guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync(TipoCambio? tipoCambio)
    {
        if (tipoCambio is null) return;

        var dialogo = new TipoCambioDialogo(tipoCambio, esNuevo: false) { Owner = Application.Current.MainWindow };
        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(tipoCambio);
        Mensaje = $"Tipo de cambio del {tipoCambio.Fecha:dd/MM/yyyy} guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync(TipoCambio? tipoCambio)
    {
        if (tipoCambio is null) return;

        var resultado = MessageBox.Show(
            $"¿Eliminar el tipo de cambio del {tipoCambio.Fecha:dd/MM/yyyy}?", "Confirmar eliminación",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (resultado != MessageBoxResult.Yes) return;

        if (tipoCambio.Id != 0)
        {
            await _repositorio.EliminarAsync(tipoCambio.Id);
        }

        TiposCambio.Remove(tipoCambio);
        Mensaje = "Tipo de cambio eliminado.";
    }

    [RelayCommand]
    private async Task ImportarArchivoAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "CSV del Banguat (*.csv)|*.csv",
            Title = "Seleccionar archivo de tipo de cambio del Banguat",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            var resultado = _lectorBanguat.Leer(dialogo.FileName);

            if (resultado.Valores.Count == 0)
            {
                Mensaje = "No se encontraron filas de tipo de cambio válidas en el archivo.";
                return;
            }

            var (insertados, actualizados) = await _repositorio.ImportarUpsertAsync(resultado.Valores);
            await CargarAsync();
            Mensaje = $"Importación completa: {insertados} insertados, {actualizados} actualizados, {resultado.FilasIgnoradas} filas ignoradas.";
        }
        catch (Exception ex)
        {
            Mensaje = $"No se pudo importar el archivo: {ex.Message}";
        }
    }
}
