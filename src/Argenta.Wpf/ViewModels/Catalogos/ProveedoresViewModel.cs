using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Data.Entidades;
using Argenta.Data.Repositorios;
using Argenta.Wpf.Modelos;
using Argenta.Wpf.Views.Dialogos;

namespace Argenta.Wpf.ViewModels.Catalogos;

/// <summary>CRUD del catálogo de Proveedores (Compra/Servicio, marca de gasolinera).</summary>
public partial class ProveedoresViewModel : ObservableObject
{
    private readonly IProveedorRepositorio _repositorio;
    private readonly ICollectionView _vista;

    public ObservableCollection<Proveedor> Proveedores { get; } = [];

    [ObservableProperty]
    private string? mensaje;

    [ObservableProperty]
    private string? textoBusqueda;

    public ProveedoresViewModel(IProveedorRepositorio repositorio)
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
        if (obj is not Proveedor proveedor) return false;

        var texto = TextoBusqueda.Trim();
        return proveedor.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || proveedor.Nit.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || OpcionTipoProveedor.ATexto(proveedor.Tipo).Contains(texto, StringComparison.OrdinalIgnoreCase);
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
        var nuevo = new Proveedor();
        var dialogo = new ProveedorDialogo(nuevo, esNuevo: true, Proveedores) { Owner = Application.Current.MainWindow };

        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(nuevo);
        Mensaje = $"Proveedor \"{nuevo.Nombre}\" guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync(Proveedor? proveedor)
    {
        if (proveedor is null) return;

        var dialogo = new ProveedorDialogo(proveedor, esNuevo: false, Proveedores) { Owner = Application.Current.MainWindow };
        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(proveedor);
        Mensaje = $"Proveedor \"{proveedor.Nombre}\" guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync(Proveedor? proveedor)
    {
        if (proveedor is null) return;

        var resultado = MessageBox.Show(
            $"¿Eliminar al proveedor \"{proveedor.Nombre}\"?", "Confirmar eliminación",
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
