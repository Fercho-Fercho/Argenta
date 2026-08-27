using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContaSuite.Data.Entidades;
using ContaSuite.Data.Repositorios;
using ContaSuite.Wpf.Modelos;
using ContaSuite.Wpf.Views.Dialogos;

namespace ContaSuite.Wpf.ViewModels.Catalogos;

/// <summary>
/// CRUD del catálogo "Proveedores a revisar" (Revisar / Excluir siempre),
/// usado solo por la pestaña "Libro de Compras (XML)" para resaltar o
/// preexcluir facturas por NIT.
/// </summary>
public partial class ProveedoresRevisarViewModel : ObservableObject
{
    private readonly IProveedorRevisarRepositorio _repositorio;
    private readonly ICollectionView _vista;

    public ObservableCollection<ProveedorRevisar> Proveedores { get; } = [];

    [ObservableProperty]
    private string? mensaje;

    [ObservableProperty]
    private string? textoBusqueda;

    public ProveedoresRevisarViewModel(IProveedorRevisarRepositorio repositorio)
    {
        _repositorio = repositorio;
        _vista = CollectionViewSource.GetDefaultView(Proveedores);
        _vista.Filter = FiltrarProveedor;
        _ = CargarAsync();
    }

    partial void OnTextoBusquedaChanged(string? value) => _vista.Refresh();

    private bool FiltrarProveedor(object obj)
    {
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return true;
        if (obj is not ProveedorRevisar proveedor) return false;

        var texto = TextoBusqueda.Trim();
        return proveedor.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || proveedor.Nit.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || OpcionAccionRevisar.ATexto(proveedor.Accion).Contains(texto, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        Proveedores.Clear();
        foreach (var proveedor in await _repositorio.ObtenerTodosAsync())
        {
            Proveedores.Add(proveedor);
        }
    }

    [RelayCommand]
    private async Task AgregarAsync()
    {
        var nuevo = new ProveedorRevisar();
        var dialogo = new ProveedorRevisarDialogo(nuevo, esNuevo: true, Proveedores) { Owner = Application.Current.MainWindow };

        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(nuevo);
        Mensaje = $"Proveedor \"{nuevo.Nombre}\" guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync(ProveedorRevisar? proveedor)
    {
        if (proveedor is null) return;

        var dialogo = new ProveedorRevisarDialogo(proveedor, esNuevo: false, Proveedores) { Owner = Application.Current.MainWindow };
        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(proveedor);
        Mensaje = $"Proveedor \"{proveedor.Nombre}\" guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync(ProveedorRevisar? proveedor)
    {
        if (proveedor is null) return;

        var resultado = MessageBox.Show(
            $"¿Eliminar al proveedor \"{proveedor.Nombre}\" del catálogo de revisión?", "Confirmar eliminación",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (resultado != MessageBoxResult.Yes) return;

        if (proveedor.Id != 0)
        {
            await _repositorio.EliminarAsync(proveedor.Id);
        }

        Proveedores.Remove(proveedor);
        Mensaje = "Proveedor eliminado.";
    }
}
