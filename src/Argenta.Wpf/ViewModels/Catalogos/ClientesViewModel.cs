using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Data.Entidades;
using Argenta.Data.Repositorios;
using Argenta.Wpf.Views.Dialogos;

namespace Argenta.Wpf.ViewModels.Catalogos;

/// <summary>CRUD del catálogo de Clientes, compartido por todos los módulos.</summary>
public partial class ClientesViewModel : ObservableObject
{
    private readonly IClienteRepositorio _repositorio;
    private readonly ICollectionView _vista;

    public ObservableCollection<Cliente> Clientes { get; } = [];

    [ObservableProperty]
    private string? mensaje;

    [ObservableProperty]
    private string? textoBusqueda;

    public ClientesViewModel(IClienteRepositorio repositorio)
    {
        _repositorio = repositorio;
        _vista = CollectionViewSource.GetDefaultView(Clientes);
        _vista.Filter = FiltrarCliente;
        _ = CargarAsync();
    }

    partial void OnTextoBusquedaChanged(string? value) => _vista.Refresh();

    private bool FiltrarCliente(object obj)
    {
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return true;
        if (obj is not Cliente cliente) return false;

        var texto = TextoBusqueda.Trim();
        return cliente.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || cliente.Nit.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || cliente.Establecimientos.Any(e => e.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        Clientes.Clear();
        foreach (var cliente in await _repositorio.ObtenerTodosAsync())
        {
            Clientes.Add(cliente);
        }
    }

    [RelayCommand]
    private async Task AgregarAsync()
    {
        var nuevo = new Cliente { Activo = true };
        var dialogo = new ClienteDialogo(nuevo, esNuevo: true) { Owner = Application.Current.MainWindow };

        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(nuevo);
        Mensaje = $"Cliente \"{nuevo.Nombre}\" guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync(Cliente? cliente)
    {
        if (cliente is null) return;

        var dialogo = new ClienteDialogo(cliente, esNuevo: false) { Owner = Application.Current.MainWindow };
        if (dialogo.ShowDialog() != true) return;

        await _repositorio.GuardarAsync(cliente);
        Mensaje = $"Cliente \"{cliente.Nombre}\" guardado.";
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync(Cliente? cliente)
    {
        if (cliente is null) return;

        var resultado = MessageBox.Show(
            $"¿Eliminar al cliente \"{cliente.Nombre}\"?", "Confirmar eliminación",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (resultado != MessageBoxResult.Yes) return;

        if (cliente.Id != 0)
        {
            await _repositorio.EliminarAsync(cliente.Id);
        }

        Clientes.Remove(cliente);
        Mensaje = "Cliente eliminado.";
    }
}
