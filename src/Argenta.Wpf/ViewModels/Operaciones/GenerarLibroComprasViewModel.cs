using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Argenta.Core.Utilidades;
using Argenta.Core.Validacion;
using Argenta.Data.Entidades;
using Argenta.Data.Repositorios;
using Argenta.Modules.LibroCompras.Modelos;
using Argenta.Modules.LibroCompras.Servicios;
using Argenta.Wpf.Views.Dialogos;
using Microsoft.Win32;

namespace Argenta.Wpf.ViewModels.Operaciones;

/// <summary>
/// ViewModel de la operación "Generar libro de compras": subir el Excel de la
/// SAT, validar, previsualizar (con filtro de IVA e inclusión/exclusión por
/// fila) y exportar el .xlsx. El cliente del libro se detecta solo a partir
/// del archivo (Función 4): no hay selección manual.
/// </summary>
public partial class GenerarLibroComprasViewModel : ObservableObject
{
    private readonly LectorFacturasSat _lector;
    private readonly LibroComprasService _servicio;
    private readonly GeneradorLibroComprasXlsx _generador;
    private readonly IClienteRepositorio _clienteRepositorio;

    private IReadOnlyList<FacturaSat> _facturas = [];
    private List<Cliente> _catalogoClientes = [];

    public ObservableCollection<FilaLibroCompras> Filas { get; } = [];
    public ObservableCollection<FilaLibroCompras> FilasAparte { get; } = [];
    public ObservableCollection<HallazgoValidacion> Hallazgos { get; } = [];

    [ObservableProperty]
    private string? rutaArchivo;

    [ObservableProperty]
    private string? mensajeEstado;

    [ObservableProperty]
    private bool advertenciasReconocidas;

    [ObservableProperty]
    private bool tieneBloqueantes;

    [ObservableProperty]
    private bool tieneAdvertencias;

    [ObservableProperty]
    private bool listoParaGenerar;

    // ---------- Función 4: cliente detectado (solo lectura) ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTextoMostrado))]
    private string? clienteNombreMostrado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTextoMostrado))]
    private string? clienteNitMostrado;

    [ObservableProperty]
    private string? clienteAdvertencia;

    /// <summary>True solo cuando el NIT detectado no existe en el catálogo (habilita "Agregar cliente").</summary>
    [ObservableProperty]
    private bool clienteNoRegistrado;

    [ObservableProperty]
    private Cliente? clienteDetectado;

    public string ClienteTextoMostrado => ClienteNombreMostrado is null
        ? "(cargue un archivo para detectar el cliente)"
        : $"{ClienteNombreMostrado} — Nit: {ClienteNitMostrado}";

    // ---------- Función 2: contador de incluidas ----------

    public int TotalFacturas => Filas.Count;
    public int FacturasIncluidas => Filas.Count(f => f.Incluida);

    public GenerarLibroComprasViewModel(
        LectorFacturasSat lector,
        LibroComprasService servicio,
        GeneradorLibroComprasXlsx generador,
        IClienteRepositorio clienteRepositorio)
    {
        _lector = lector;
        _servicio = servicio;
        _generador = generador;
        _clienteRepositorio = clienteRepositorio;
        _ = CargarCatalogoClientesAsync();
    }

    private async Task CargarCatalogoClientesAsync()
    {
        _catalogoClientes = await _clienteRepositorio.ObtenerTodosAsync();
    }

    [RelayCommand]
    private async Task SeleccionarArchivoAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Excel de facturas de la SAT (*.xls)|*.xls",
            Title = "Seleccionar Excel de facturas recibidas de la SAT",
        };

        if (dialogo.ShowDialog() != true) return;

        RutaArchivo = dialogo.FileName;
        Filas.Clear();
        FilasAparte.Clear();
        Hallazgos.Clear();
        ListoParaGenerar = false;
        AdvertenciasReconocidas = false;
        LimpiarClienteDetectado();

        try
        {
            using (var flujo = File.OpenRead(RutaArchivo))
            {
                _facturas = _lector.Leer(flujo);
            }

            MensajeEstado = $"{_facturas.Count} facturas leídas del archivo.";
            DetectarCliente();
            await ValidarYClasificarAsync();
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo leer el archivo: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Limpiar()
    {
        _facturas = [];
        RutaArchivo = null;
        DesuscribirFilas();
        Filas.Clear();
        FilasAparte.Clear();
        Hallazgos.Clear();
        TieneBloqueantes = false;
        TieneAdvertencias = false;
        AdvertenciasReconocidas = false;
        ListoParaGenerar = false;
        MensajeEstado = null;
        LimpiarClienteDetectado();
        NotificarContadores();
        GenerarCommand.NotifyCanExecuteChanged();
    }

    private void LimpiarClienteDetectado()
    {
        ClienteDetectado = null;
        ClienteNombreMostrado = null;
        ClienteNitMostrado = null;
        ClienteAdvertencia = null;
        ClienteNoRegistrado = false;
    }

    /// <summary>
    /// Función 4: detecta el cliente del libro a partir del "ID del receptor"
    /// de las facturas (todas deben ser del mismo contribuyente). El campo de
    /// cliente en la vista es de solo lectura: nunca se selecciona a mano.
    /// </summary>
    private void DetectarCliente()
    {
        LimpiarClienteDetectado();

        if (_facturas.Count == 0) return;

        var idsReceptor = _facturas.Select(f => f.IdReceptor).Distinct().ToList();

        if (idsReceptor.Count > 1)
        {
            ClienteAdvertencia =
                "El archivo contiene facturas de más de un receptor (NIT): " +
                string.Join(", ", idsReceptor) +
                ". Un libro de compras debe corresponder a un solo contribuyente; verifique el archivo.";
            return;
        }

        var idReceptor = idsReceptor[0];
        var nitNormalizado = NitUtil.Normalizar(idReceptor);
        var coincidencia = _catalogoClientes.FirstOrDefault(c => NitUtil.Normalizar(c.Nit) == nitNormalizado);

        if (coincidencia is not null)
        {
            ClienteDetectado = coincidencia;
            ClienteNombreMostrado = coincidencia.Nombre;
            ClienteNitMostrado = coincidencia.Nit;
            return;
        }

        var nombreReceptorArchivo = _facturas.FirstOrDefault()?.NombreReceptor;
        ClienteNombreMostrado = string.IsNullOrWhiteSpace(nombreReceptorArchivo)
            ? "(nombre no disponible en el archivo)"
            : nombreReceptorArchivo;
        ClienteNitMostrado = idReceptor;
        ClienteNoRegistrado = true;
        ClienteAdvertencia =
            $"Cliente no registrado en catálogo (NIT: {idReceptor}). Se usará el nombre del archivo para generar " +
            "el libro; puede agregarlo al catálogo con el botón de al lado.";

        // Cliente transitorio solo para el encabezado del archivo generado;
        // no se guarda en la base de datos a menos que el usuario lo agregue.
        ClienteDetectado = new Cliente
        {
            Nombre = ClienteNombreMostrado,
            Nit = idReceptor ?? string.Empty,
        };
    }

    [RelayCommand]
    private async Task AgregarClienteDetectadoAsync()
    {
        var nuevo = new Cliente
        {
            Nombre = ClienteNombreMostrado ?? string.Empty,
            Nit = ClienteNitMostrado ?? string.Empty,
            Activo = true,
        };

        var dialogo = new ClienteDialogo(nuevo, esNuevo: true) { Owner = Application.Current.MainWindow };
        if (dialogo.ShowDialog() != true) return;

        await _clienteRepositorio.GuardarAsync(nuevo);
        await CargarCatalogoClientesAsync();
        DetectarCliente();
        MensajeEstado = $"Cliente \"{nuevo.Nombre}\" agregado al catálogo.";
    }

    [RelayCommand]
    private async Task ValidarYClasificarAsync()
    {
        if (_facturas.Count == 0)
        {
            MensajeEstado = "Primero seleccione el Excel de facturas de la SAT.";
            return;
        }

        var resultado = await _servicio.ProcesarAsync(_facturas);

        DesuscribirFilas();
        Filas.Clear();
        foreach (var fila in resultado.Filas) Filas.Add(fila);
        SuscribirFilas();

        FilasAparte.Clear();
        foreach (var fila in resultado.FilasAparte) FilasAparte.Add(fila);

        Hallazgos.Clear();
        foreach (var hallazgo in resultado.Hallazgos) Hallazgos.Add(hallazgo);

        TieneBloqueantes = resultado.TieneBloqueantes;
        TieneAdvertencias = resultado.Advertencias.Any();
        ListoParaGenerar = !TieneBloqueantes;

        var revisar = Filas.Count(f => f.ProveedorNoEncontrado || f.DescuadreIva);
        MensajeEstado = TieneBloqueantes
            ? "Hay problemas que impiden generar el libro. Revise los mensajes en rojo."
            : $"Listo para generar: {Filas.Count} filas ({revisar} para revisar, resaltadas en naranja/amarillo).";

        NotificarContadores();
        GenerarCommand.NotifyCanExecuteChanged();
    }

    private void SuscribirFilas()
    {
        foreach (var fila in Filas) fila.PropertyChanged += Fila_PropertyChanged;
    }

    private void DesuscribirFilas()
    {
        foreach (var fila in Filas) fila.PropertyChanged -= Fila_PropertyChanged;
    }

    private void Fila_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilaLibroCompras.Incluida)) NotificarContadores();
    }

    private void NotificarContadores()
    {
        OnPropertyChanged(nameof(TotalFacturas));
        OnPropertyChanged(nameof(FacturasIncluidas));
    }

    [RelayCommand]
    private void MostrarInfoColores()
    {
        var dialogo = new LeyendaColoresDialogo { Owner = Application.Current.MainWindow };
        dialogo.ShowDialog();
    }

    [RelayCommand(CanExecute = nameof(PuedeGenerar))]
    private void Generar()
    {
        if (ClienteDetectado is null)
        {
            MensajeEstado = "No se pudo determinar el cliente del libro (revise la advertencia sobre el cliente).";
            return;
        }

        var (mes, anio) = PeriodoUtil.ObtenerMesAnioPredominante(Filas.Where(f => f.Incluida).ToList());
        var periodo = $"{ParseoUtil.AbreviaturaMesEspanol(mes)} {anio}";

        var dialogo = new SaveFileDialog
        {
            Filter = "Libro de Excel (*.xlsx)|*.xlsx",
            FileName = $"Libro de compras - {ClienteDetectado.Nombre} - {periodo}.xlsx",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            // GeneradorLibroComprasXlsx.Generar ya se encarga de omitir las
            // filas marcadas "Excluir" y de recalcular el correlativo.
            _generador.Generar(dialogo.FileName, ClienteDetectado, Filas.ToList(), FilasAparte.ToList());
            MensajeEstado = $"Libro generado correctamente en: {dialogo.FileName}";
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo generar el archivo: {ex.Message}";
        }
    }

    private bool PuedeGenerar() => ListoParaGenerar && (!TieneAdvertencias || AdvertenciasReconocidas) && ClienteDetectado is not null;

    partial void OnListoParaGenerarChanged(bool value) => GenerarCommand.NotifyCanExecuteChanged();
    partial void OnAdvertenciasReconocidasChanged(bool value) => GenerarCommand.NotifyCanExecuteChanged();
    partial void OnTieneAdvertenciasChanged(bool value) => GenerarCommand.NotifyCanExecuteChanged();
    partial void OnClienteDetectadoChanged(Cliente? value) => GenerarCommand.NotifyCanExecuteChanged();
}
