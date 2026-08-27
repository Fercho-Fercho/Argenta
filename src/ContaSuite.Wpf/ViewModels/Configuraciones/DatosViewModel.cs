using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContaSuite.Data.Servicios;
using Microsoft.Win32;

namespace ContaSuite.Wpf.ViewModels.Configuraciones;

/// <summary>
/// Pantalla "Configuraciones → Datos": importar catálogos desde un .json, o
/// descargar la plantilla vacía para llenarla a mano. Por privacidad, esta
/// pantalla NUNCA ofrece "exportar datos reales" — ver <see cref="IImportacionDatosService"/>.
/// </summary>
public partial class DatosViewModel(IImportacionDatosService servicio) : ObservableObject
{
    [ObservableProperty]
    private string? mensajeEstado;

    [RelayCommand]
    private async Task ImportarDatosAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Archivo de datos ContaSuite (*.json)|*.json",
            Title = "Seleccionar archivo de datos a importar",
        };
        if (dialogo.ShowDialog() != true) return;

        DatosContaSuiteJson datos;
        try
        {
            var contenido = await File.ReadAllTextAsync(dialogo.FileName);
            datos = servicio.ParsearJson(contenido);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo leer el archivo: {ex.Message}", "ContaSuite", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ResumenImportacion resumen;
        try
        {
            resumen = await servicio.PrevisualizarAsync(datos);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo revisar el archivo: {ex.Message}", "ContaSuite", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (resumen.TotalNuevos == 0 && resumen.TotalActualizados == 0)
        {
            MessageBox.Show(
                $"No hay nada para importar de este archivo.\n\n{ArmarTextoResumen(resumen)}",
                "ContaSuite", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmacion = MessageBox.Show(
            $"{ArmarTextoResumen(resumen)}\n\n¿Aplicar esta importación?",
            "Confirmar importación", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmacion != MessageBoxResult.Yes) return;

        var resultado = await servicio.ImportarAsync(datos);

        MensajeEstado =
            $"Importación completada: {resultado.TotalNuevos} nuevos, {resultado.TotalActualizados} actualizados" +
            (resultado.FilasIgnoradas > 0 ? $", {resultado.FilasIgnoradas} filas de ejemplo/en blanco ignoradas" : "") +
            (resultado.Errores.Count > 0 ? $", {resultado.Errores.Count} filas con errores (omitidas)" : "") + ".";

        if (resultado.Errores.Count > 0)
        {
            MessageBox.Show(
                "Se importó lo válido, pero estas filas tenían errores y se omitieron:\n\n" + string.Join("\n", resultado.Errores),
                "Errores en la importación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void DescargarPlantilla()
    {
        var dialogo = new SaveFileDialog
        {
            Filter = "Archivo de datos ContaSuite (*.json)|*.json",
            FileName = "ContaSuite - plantilla de datos.json",
        };
        if (dialogo.ShowDialog() != true) return;

        try
        {
            // GenerarPlantillaVacia() nunca lee la base de datos: es siempre
            // la misma estructura fija con una fila de ejemplo marcada.
            File.WriteAllText(dialogo.FileName, servicio.GenerarPlantillaVacia());
            MensajeEstado = $"Plantilla vacía guardada en: {dialogo.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo guardar la plantilla: {ex.Message}", "ContaSuite", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string ArmarTextoResumen(ResumenImportacion resumen)
    {
        var lineas = new List<string>();

        void Agregar(string etiqueta, int nuevos, int actualizados)
        {
            if (nuevos > 0 || actualizados > 0) lineas.Add($"{etiqueta}: {nuevos} nuevos, {actualizados} actualizados");
        }

        Agregar("Clientes", resumen.ClientesNuevos, resumen.ClientesActualizados);
        Agregar("Establecimientos", resumen.EstablecimientosNuevos, resumen.EstablecimientosActualizados);
        Agregar("Proveedores", resumen.ProveedoresNuevos, resumen.ProveedoresActualizados);
        Agregar("Proveedores a revisar", resumen.ProveedoresRevisarNuevos, resumen.ProveedoresRevisarActualizados);
        Agregar("Tipos de cambio", resumen.TiposCambioNuevos, resumen.TiposCambioActualizados);
        Agregar("Selecciones (memoria de inclusión/exclusión)", resumen.SeleccionesNuevas, resumen.SeleccionesActualizadas);

        if (lineas.Count == 0) lineas.Add("(sin cambios: todo ya está igual en la base de datos)");
        if (resumen.FilasIgnoradas > 0) lineas.Add($"Filas de ejemplo/en blanco ignoradas: {resumen.FilasIgnoradas}");
        if (resumen.Errores.Count > 0) lineas.Add($"Filas con errores (se omitirán): {resumen.Errores.Count}");

        return "Se importará lo siguiente:\n\n" + string.Join("\n", lineas);
    }
}
