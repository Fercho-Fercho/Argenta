using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using Argenta.Wpf.Modelos;
using Argenta.Wpf.Views.Dialogos;
using Microsoft.Win32;

namespace Argenta.Wpf.ViewModels.Operaciones;

/// <summary>
/// ViewModel de la operación "Libro de Compras (XML)": subir un .zip con
/// facturas electrónicas (FEL), validar, previsualizar y exportar el mismo
/// .xlsx que la pestaña basada en el Excel de la SAT. A diferencia de esa
/// pestaña, aquí NO se usa el catálogo de Proveedores (cada factura ya trae
/// su propia clasificación Bien/Servicio por ítem), así que no hay marcado
/// naranja de "proveedor no encontrado".
/// </summary>
public partial class GenerarLibroComprasFelViewModel : ObservableObject
{
    private readonly LectorZipXmlFel _lector;
    private readonly LibroComprasFelService _servicio;
    private readonly GeneradorLibroComprasXlsx _generador;
    private readonly GeneradorLibroComprasPdf _generadorPdf;
    private readonly IClienteRepositorio _clienteRepositorio;
    private readonly IProveedorRevisarRepositorio _proveedorRevisarRepositorio;

    private IReadOnlyList<DteFel> _facturas = [];
    private List<Cliente> _catalogoClientes = [];

    /// <summary>
    /// Inclusión/exclusión que el usuario marcó a mano en el checkbox
    /// "Incluir" de cada factura (identificada por su <see cref="DteFel"/>
    /// de origen, que no cambia mientras no se cargue un ZIP nuevo). Se
    /// reaplica después de cada reclasificación (p. ej. al guardar un
    /// proveedor nuevo en "Proveedores a revisar" desde esta misma
    /// pantalla), para que no se pierda lo que el usuario ya había
    /// desmarcado/marcado manualmente en otras filas.
    /// </summary>
    private readonly Dictionary<DteFel, bool> _inclusionManual = [];

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

    // ---------- Cliente detectado (solo lectura, igual que la otra pestaña) ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTextoMostrado))]
    private string? clienteNombreMostrado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClienteTextoMostrado))]
    private string? clienteNitMostrado;

    [ObservableProperty]
    private string? clienteAdvertencia;

    [ObservableProperty]
    private bool clienteNoRegistrado;

    [ObservableProperty]
    private Cliente? clienteDetectado;

    public string ClienteTextoMostrado => ClienteNombreMostrado is null
        ? "(cargue un .zip para detectar el cliente)"
        : $"{ClienteNombreMostrado} — Nit: {ClienteNitMostrado}";

    public int TotalFacturas => Filas.Count;
    public int FacturasIncluidas => Filas.Count(f => f.Incluida);

    // ---------- Mejora 1: resumen / totales en vivo (siempre sobre TODAS las incluidas) ----------

    [ObservableProperty]
    private decimal resumenTotalCompras;

    [ObservableProperty]
    private decimal resumenTotalServicios;

    [ObservableProperty]
    private decimal resumenTotalExento;

    [ObservableProperty]
    private decimal resumenTotalIva;

    [ObservableProperty]
    private decimal resumenGranTotal;

    /// <summary>Facturas marcadas para revisión (naranja), sin importar si están filtradas o excluidas.</summary>
    [ObservableProperty]
    private int resumenNaranja;

    /// <summary>Facturas excluidas por el catálogo "Proveedores a revisar" (rojo suave).</summary>
    [ObservableProperty]
    private int resumenRojo;

    /// <summary>Facturas con descuadre de IVA (amarillo).</summary>
    [ObservableProperty]
    private int resumenAmarillo;

    public GenerarLibroComprasFelViewModel(
        LectorZipXmlFel lector,
        LibroComprasFelService servicio,
        GeneradorLibroComprasXlsx generador,
        GeneradorLibroComprasPdf generadorPdf,
        IClienteRepositorio clienteRepositorio,
        IProveedorRevisarRepositorio proveedorRevisarRepositorio)
    {
        _lector = lector;
        _servicio = servicio;
        _generador = generador;
        _generadorPdf = generadorPdf;
        _clienteRepositorio = clienteRepositorio;
        _proveedorRevisarRepositorio = proveedorRevisarRepositorio;

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
            Filter = "ZIP de facturas electrónicas (*.zip)|*.zip",
            Title = "Seleccionar ZIP con facturas electrónicas (FEL) en XML",
        };

        if (dialogo.ShowDialog() != true) return;

        RutaArchivo = dialogo.FileName;
        Filas.Clear();
        FilasAparte.Clear();
        Hallazgos.Clear();
        ListoParaGenerar = false;
        AdvertenciasReconocidas = false;
        _inclusionManual.Clear();
        LimpiarClienteDetectado();

        try
        {
            using (var flujo = File.OpenRead(RutaArchivo))
            {
                _facturas = _lector.Leer(flujo);
            }

            MensajeEstado = $"{_facturas.Count} facturas XML leídas del ZIP.";
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
        _inclusionManual.Clear();
        TieneBloqueantes = false;
        TieneAdvertencias = false;
        AdvertenciasReconocidas = false;
        ListoParaGenerar = false;
        MensajeEstado = null;
        LimpiarClienteDetectado();
        NotificarContadores();
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
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
    /// Detecta el cliente del libro a partir del "IDReceptor" de las
    /// facturas (todas deben ser del mismo contribuyente). Igual que en la
    /// otra pestaña: el campo es de solo lectura, nunca se selecciona a mano.
    /// </summary>
    private void DetectarCliente()
    {
        LimpiarClienteDetectado();

        if (_facturas.Count == 0) return;

        var idsReceptor = _facturas.Select(f => f.IdReceptor).Distinct().ToList();

        if (idsReceptor.Count > 1)
        {
            ClienteAdvertencia =
                "El ZIP contiene facturas de más de un receptor (NIT): " +
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
            MensajeEstado = "Primero seleccione el ZIP con las facturas electrónicas (XML).";
            return;
        }

        var resultado = await _servicio.ProcesarAsync(_facturas);

        // Reaplica cualquier inclusión/exclusión que el usuario ya había
        // marcado a mano, para que reclasificar (p. ej. al guardar un
        // proveedor nuevo en el catálogo) no la resetee al valor por
        // defecto. Se hace ANTES de agregar las filas a la colección y de
        // volver a suscribirse, para no registrar esta restauración como si
        // fuera un cambio manual nuevo.
        foreach (var fila in resultado.Filas.Concat(resultado.FilasAparte))
        {
            if (fila.OrigenFel is not null && _inclusionManual.TryGetValue(fila.OrigenFel, out var incluidaManual))
            {
                fila.Incluida = incluidaManual;
            }
        }

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

        var paraRevisar = Filas.Count(f => f.DescuadreIva || f.MarcadoParaRevisar);
        var excluidas = Filas.Count(f => f.ExcluidoPorCatalogo);
        MensajeEstado = TieneBloqueantes
            ? "Hay problemas que impiden generar el libro. Revise los mensajes en rojo."
            : $"Listo para generar: {Filas.Count} filas ({paraRevisar} para revisar, resaltadas en naranja/amarillo; " +
              $"{excluidas} excluidas automáticamente en rojo suave).";

        NotificarContadores();
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
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
        if (e.PropertyName != nameof(FilaLibroCompras.Incluida)) return;

        // Solo llegamos aquí por un cambio real del usuario en el checkbox
        // (la restauración en ValidarYClasificarAsync ocurre antes de
        // suscribirse): se recuerda para sobrevivir a la próxima reclasificación.
        if (sender is FilaLibroCompras { OrigenFel: not null } fila)
        {
            _inclusionManual[fila.OrigenFel] = fila.Incluida;
        }

        NotificarContadores();
    }

    /// <summary>Se llama cada vez que cambia el conjunto de filas o el estado incluida/excluida de alguna.</summary>
    private void NotificarContadores()
    {
        OnPropertyChanged(nameof(TotalFacturas));
        OnPropertyChanged(nameof(FacturasIncluidas));
        RecalcularResumen();
    }

    /// <summary>Mejora 1: totales en vivo, siempre sobre TODAS las facturas incluidas (no solo las filtradas/visibles).</summary>
    private void RecalcularResumen()
    {
        var incluidas = Filas.Where(f => f.Incluida).ToList();

        ResumenTotalCompras = RedondeoUtil.Redondear(incluidas.Sum(f => f.Compras));
        ResumenTotalServicios = RedondeoUtil.Redondear(incluidas.Sum(f => f.Servicios));
        ResumenTotalExento = RedondeoUtil.Redondear(incluidas.Sum(f => f.Exento));
        ResumenTotalIva = RedondeoUtil.Redondear(incluidas.Sum(f => f.Iva));
        ResumenGranTotal = RedondeoUtil.Redondear(incluidas.Sum(f => f.Total));

        ResumenNaranja = Filas.Count(f => f.MarcadoParaRevisar);
        ResumenRojo = Filas.Count(f => f.ExcluidoPorCatalogo);
        ResumenAmarillo = Filas.Count(f => f.DescuadreIva);
    }

    [RelayCommand]
    private void MostrarInfoColores()
    {
        var dialogo = new LeyendaColoresFelDialogo { Owner = Application.Current.MainWindow };
        dialogo.ShowDialog();
    }

    /// <summary>Abre el detalle visual de la factura (Cambio 6): solo disponible para filas con origen FEL.</summary>
    [RelayCommand]
    private void VerDetalle(FilaLibroCompras? fila)
    {
        if (fila?.OrigenFel is null) return;

        var dialogo = new DetalleFacturaFelDialogo(fila.OrigenFel) { Owner = Application.Current.MainWindow };
        dialogo.ShowDialog();
    }

    /// <summary>
    /// Guardado rápido al catálogo "Proveedores a revisar" sin salir de esta
    /// pantalla: el NIT y el Nombre ya se conocen (vienen de la factura), así
    /// que solo hace falta decidir la Acción. Si el NIT ya estaba en el
    /// catálogo, se actualiza (upsert) en vez de duplicarlo. Después de
    /// guardar, se vuelve a clasificar para que el resaltado y el estado de
    /// inclusión se reflejen de inmediato en el preview.
    /// </summary>
    [RelayCommand]
    private async Task MarcarParaRevisarAsync(FilaLibroCompras? fila) =>
        await GuardarEnCatalogoRevisarAsync(fila, AccionRevisar.Revisar);

    [RelayCommand]
    private async Task ExcluirSiempreAsync(FilaLibroCompras? fila) =>
        await GuardarEnCatalogoRevisarAsync(fila, AccionRevisar.ExcluirSiempre);

    private async Task GuardarEnCatalogoRevisarAsync(FilaLibroCompras? fila, AccionRevisar accion)
    {
        if (fila is null) return;

        var nitNormalizado = NitUtil.Normalizar(fila.Nit);
        var existentes = await _proveedorRevisarRepositorio.ObtenerDiccionarioPorNitAsync();
        var proveedor = existentes.TryGetValue(nitNormalizado, out var encontrado)
            ? encontrado
            : new ProveedorRevisar { Nit = nitNormalizado };

        proveedor.Nombre = fila.Proveedor;
        proveedor.Accion = accion;

        await _proveedorRevisarRepositorio.GuardarAsync(proveedor);

        // La acción recién elegida para ESTA fila manda sobre cualquier
        // marca manual anterior que tuviera (si el usuario ya la había
        // marcado/desmarcado a mano antes de usar este botón); las demás
        // filas conservan su marca manual sin tocarse.
        if (fila.OrigenFel is not null) _inclusionManual.Remove(fila.OrigenFel);

        var textoAccion = accion == AccionRevisar.Revisar ? "Revisar (naranja)" : "Excluir siempre (rojo suave)";
        MensajeEstado = $"\"{fila.Proveedor}\" (NIT {fila.Nit}) guardado en \"Proveedores a revisar\" con acción {textoAccion}.";

        await ValidarYClasificarAsync();
    }

    [RelayCommand(CanExecute = nameof(PuedeGenerar))]
    private async Task GenerarAsync()
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
            _generador.Generar(dialogo.FileName, ClienteDetectado, Filas.ToList(), FilasAparte.ToList());

            // Recuerda qué facturas quedaron incluidas/excluidas, por cliente y
            // periodo, para la próxima vez que se suba el mismo pool.
            await _servicio.GuardarSeleccionAsync(_facturas, Filas.ToList());

            MensajeEstado = $"Libro generado correctamente en: {dialogo.FileName}. Selección guardada para \"{ClienteDetectado.Nombre}\" ({periodo}).";

            AbrirArchivoGenerado(dialogo.FileName);
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo generar el archivo: {ex.Message}";
        }
    }

    /// <summary>Versión PDF, lista para imprimir, de la misma tabla de facturas del Excel (no guarda selección: es solo un extra para imprimir/repasar).</summary>
    [RelayCommand(CanExecute = nameof(PuedeGenerar))]
    private void GenerarPdf()
    {
        if (ClienteDetectado is null)
        {
            MensajeEstado = "No se pudo determinar el cliente del libro (revise la advertencia sobre el cliente).";
            return;
        }

        var filaFolio = new FolioInicialFila { Etiqueta = "Folio inicial", NumeroEstablecimiento = 0 };
        var dialogoFolio = new FolioInicialDialogo([filaFolio]) { Owner = Application.Current.MainWindow };
        if (dialogoFolio.ShowDialog() != true) return;

        var (mes, anio) = PeriodoUtil.ObtenerMesAnioPredominante(Filas.Where(f => f.Incluida).ToList());
        var periodo = $"{ParseoUtil.AbreviaturaMesEspanol(mes)} {anio}";

        var dialogo = new SaveFileDialog
        {
            Filter = "Documento PDF (*.pdf)|*.pdf",
            FileName = $"Libro de compras - {ClienteDetectado.Nombre} - {periodo}.pdf",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            _generadorPdf.Generar(dialogo.FileName, ClienteDetectado, Filas.ToList(), FilasAparte.ToList(), filaFolio.FolioInicial);
            MensajeEstado = $"PDF generado correctamente en: {dialogo.FileName}.";
            AbrirArchivoGenerado(dialogo.FileName);
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo generar el PDF: {ex.Message}";
        }
    }

    private void AbrirArchivoGenerado(string ruta)
    {
        try
        {
            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MensajeEstado += $" (No se pudo abrir el archivo automáticamente: {ex.Message})";
        }
    }

    private bool PuedeGenerar() => ListoParaGenerar && (!TieneAdvertencias || AdvertenciasReconocidas) && ClienteDetectado is not null;

    partial void OnListoParaGenerarChanged(bool value)
    {
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnAdvertenciasReconocidasChanged(bool value)
    {
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnTieneAdvertenciasChanged(bool value)
    {
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnClienteDetectadoChanged(Cliente? value)
    {
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
    }
}
