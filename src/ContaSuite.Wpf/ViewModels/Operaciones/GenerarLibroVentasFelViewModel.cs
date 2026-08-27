using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContaSuite.Core.Utilidades;
using ContaSuite.Core.Validacion;
using ContaSuite.Data.Entidades;
using ContaSuite.Data.Repositorios;
using ContaSuite.Modules.LibroCompras.Modelos;
using ContaSuite.Modules.LibroCompras.Servicios;
using ContaSuite.Wpf.Modelos;
using ContaSuite.Wpf.Views.Dialogos;
using Microsoft.Win32;

namespace ContaSuite.Wpf.ViewModels.Operaciones;

/// <summary>
/// ViewModel de la operación "Libro de Ventas (XML)": a diferencia de
/// compras, necesita DOS archivos — el .zip de facturas electrónicas (FEL) Y
/// el .xls "Consulta de documentos" del SAT, porque el ZIP no trae si una
/// factura quedó anulada (ver <see cref="LectorEstadoDocumentosSat"/>). El
/// cliente del libro es el EMISOR (el contador gestiona a sus clientes, que
/// aquí son quienes venden), y el Nit/Nombre que se muestran en cada fila son
/// los del receptor (comprador). No hay checkbox de incluir/excluir ni
/// catálogo de revisión: en ventas no se descarta nada (sección 5.5 del pedido).
///
/// El libro se genera POR ESTABLECIMIENTO, pero en UN SOLO ARCHIVO: el pool
/// se agrupa por el código de establecimiento del emisor (ver
/// <see cref="LibroVentasFelService"/>), y "Generar" produce un .xlsx con UNA
/// HOJA POR CADA establecimiento REGISTRADO del cliente — todas, aunque
/// alguna no tenga facturas en este pool (queda en 0, con su encabezado
/// igual). Los códigos del pool que no están registrados en el catálogo no
/// tienen hoja (no hay Tipo/Exporta con qué clasificarlos) y se avisan
/// aparte; ver <see cref="ResumenLibros"/> y <see cref="Generar"/>.
/// </summary>
public partial class GenerarLibroVentasFelViewModel : ObservableObject
{
    private readonly LectorZipXmlFel _lectorZip;
    private readonly LectorEstadoDocumentosSat _lectorEstados;
    private readonly LibroVentasFelService _servicio;
    private readonly GeneradorLibroVentasXlsx _generador;
    private readonly GeneradorLibroVentasPdf _generadorPdf;
    private readonly IClienteRepositorio _clienteRepositorio;

    private IReadOnlyList<DteFel> _facturas = [];
    private IReadOnlyDictionary<string, EstadoDocumentoSat> _estados = new Dictionary<string, EstadoDocumentoSat>();
    private List<Cliente> _catalogoClientes = [];
    private ResultadoProcesamientoVentasMultiple? _ultimoResultado;

    public ObservableCollection<FilaLibroVentas> Filas { get; } = [];
    public ObservableCollection<FilaLibroVentas> FilasAparte { get; } = [];
    public ObservableCollection<HallazgoValidacion> Hallazgos { get; } = [];

    [ObservableProperty]
    private string? rutaArchivoZip;

    [ObservableProperty]
    private string? rutaArchivoEstados;

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

    /// <summary>Resumen tipo "Se generará 1 archivo con 2 hojas: Est. 1 (45 facturas), Est. 2 (233 facturas)".</summary>
    [ObservableProperty]
    private string? resumenLibros;

    // ---------- Cliente detectado (el EMISOR de las facturas: es quien vende) ----------

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
        ? "(cargue los dos archivos para detectar el cliente)"
        : $"{ClienteNombreMostrado} — Nit: {ClienteNitMostrado}";

    public int TotalFacturas => Filas.Count;

    // ---------- Resumen en vivo (sobre TODOS los establecimientos juntos) ----------

    [ObservableProperty]
    private decimal resumenVentas;

    [ObservableProperty]
    private decimal resumenServicios;

    [ObservableProperty]
    private decimal resumenIva;

    [ObservableProperty]
    private decimal resumenTotal;

    /// <summary>Facturas con descuadre de IVA (amarillo): único resaltado que aplica en ventas.</summary>
    [ObservableProperty]
    private int resumenDescuadre;

    /// <summary>Facturas anuladas (según el .xls de consulta): se quedan en el libro, en 0.</summary>
    [ObservableProperty]
    private int resumenAnuladas;

    public GenerarLibroVentasFelViewModel(
        LectorZipXmlFel lectorZip,
        LectorEstadoDocumentosSat lectorEstados,
        LibroVentasFelService servicio,
        GeneradorLibroVentasXlsx generador,
        GeneradorLibroVentasPdf generadorPdf,
        IClienteRepositorio clienteRepositorio)
    {
        _lectorZip = lectorZip;
        _lectorEstados = lectorEstados;
        _servicio = servicio;
        _generador = generador;
        _generadorPdf = generadorPdf;
        _clienteRepositorio = clienteRepositorio;

        _ = CargarCatalogoClientesAsync();
    }

    private async Task CargarCatalogoClientesAsync()
    {
        _catalogoClientes = await _clienteRepositorio.ObtenerTodosAsync();
    }

    [RelayCommand]
    private async Task SeleccionarArchivoZipAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "ZIP de facturas electrónicas (*.zip)|*.zip",
            Title = "Seleccionar ZIP con facturas electrónicas (FEL) en XML",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            using (var flujo = File.OpenRead(dialogo.FileName))
            {
                _facturas = _lectorZip.Leer(flujo);
            }

            RutaArchivoZip = dialogo.FileName;
            MensajeEstado = $"{_facturas.Count} facturas XML leídas del ZIP.";
            DetectarCliente();
            await ValidarYClasificarAsync();
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo leer el ZIP: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SeleccionarArchivoEstadosAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Consulta de documentos SAT (*.xls)|*.xls",
            Title = "Seleccionar el Excel \"Consulta de documentos\" del SAT (para saber cuáles están anuladas)",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            using (var flujo = File.OpenRead(dialogo.FileName))
            {
                _estados = _lectorEstados.Leer(flujo);
            }

            RutaArchivoEstados = dialogo.FileName;
            MensajeEstado = $"{_estados.Count} documentos leídos del Excel de consulta.";
            await ValidarYClasificarAsync();
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo leer el Excel de consulta: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Limpiar()
    {
        _facturas = [];
        _estados = new Dictionary<string, EstadoDocumentoSat>();
        _ultimoResultado = null;
        RutaArchivoZip = null;
        RutaArchivoEstados = null;
        Filas.Clear();
        FilasAparte.Clear();
        Hallazgos.Clear();
        TieneBloqueantes = false;
        TieneAdvertencias = false;
        AdvertenciasReconocidas = false;
        ListoParaGenerar = false;
        ResumenLibros = null;
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
    /// Detecta el cliente del libro a partir del NIT del EMISOR de las
    /// facturas (todas deben ser del mismo contribuyente: es quien vende). A
    /// diferencia de compras, aquí el "cliente" del catálogo es el vendedor,
    /// no el comprador.
    /// </summary>
    private void DetectarCliente()
    {
        LimpiarClienteDetectado();

        if (_facturas.Count == 0) return;

        var nitsEmisor = _facturas.Select(f => f.NitEmisor).Distinct().ToList();

        if (nitsEmisor.Count > 1)
        {
            ClienteAdvertencia =
                "El ZIP contiene facturas de más de un emisor (NIT): " +
                string.Join(", ", nitsEmisor) +
                ". Un libro de ventas debe corresponder a un solo contribuyente (el que vende); verifique el archivo.";
            return;
        }

        var nitEmisor = nitsEmisor[0];
        var nitNormalizado = NitUtil.Normalizar(nitEmisor);
        var coincidencia = _catalogoClientes.FirstOrDefault(c => NitUtil.Normalizar(c.Nit) == nitNormalizado);

        if (coincidencia is not null)
        {
            ClienteDetectado = coincidencia;
            ClienteNombreMostrado = coincidencia.Nombre;
            ClienteNitMostrado = coincidencia.Nit;
            return;
        }

        var nombreEmisorArchivo = _facturas.FirstOrDefault()?.NombreEmisor;
        ClienteNombreMostrado = string.IsNullOrWhiteSpace(nombreEmisorArchivo)
            ? "(nombre no disponible en el archivo)"
            : nombreEmisorArchivo;
        ClienteNitMostrado = nitEmisor;
        ClienteNoRegistrado = true;
        ClienteAdvertencia =
            $"Cliente no registrado en catálogo (NIT: {nitEmisor}). Se usará el nombre del archivo para generar " +
            "el libro; puede agregarlo al catálogo con el botón de al lado. Agréguele al menos un establecimiento " +
            "con su Tipo y Exporta para poder generar sus libros.";

        ClienteDetectado = new Cliente
        {
            Nombre = ClienteNombreMostrado,
            Nit = nitEmisor ?? string.Empty,
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
        await ValidarYClasificarAsync();
    }

    [RelayCommand]
    private Task ValidarYClasificarAsync()
    {
        if (_facturas.Count == 0 || _estados.Count == 0)
        {
            MensajeEstado = _facturas.Count == 0
                ? "Primero seleccione el ZIP con las facturas electrónicas (XML)."
                : "Ahora seleccione el Excel \"Consulta de documentos\" del SAT (para saber cuáles facturas están anuladas).";
            return Task.CompletedTask;
        }

        if (ClienteDetectado is null)
        {
            MensajeEstado = "No se pudo determinar el cliente del libro (revise la advertencia sobre el cliente).";
            return Task.CompletedTask;
        }

        var resultado = _servicio.Procesar(_facturas, ClienteDetectado, _estados);
        _ultimoResultado = resultado;

        // Preview combinado: todas las filas de todos los establecimientos
        // juntas (la columna Establecimiento, en la vista, distingue de
        // cuál viene cada una — ver OrigenFel.CodigoEstablecimiento).
        Filas.Clear();
        foreach (var libro in resultado.Libros)
        {
            foreach (var fila in libro.Filas) Filas.Add(fila);
        }

        FilasAparte.Clear();
        foreach (var libro in resultado.Libros)
        {
            foreach (var fila in libro.FilasAparte) FilasAparte.Add(fila);
        }

        Hallazgos.Clear();
        foreach (var hallazgo in resultado.HallazgosGenerales) Hallazgos.Add(hallazgo);
        foreach (var libro in resultado.Libros)
        {
            foreach (var hallazgo in libro.Hallazgos) Hallazgos.Add(hallazgo);
        }

        TieneBloqueantes = resultado.TieneBloqueantesGenerales;
        TieneAdvertencias = Hallazgos.Any(h => h.Severidad == SeveridadValidacion.Advertencia);
        ListoParaGenerar = !TieneBloqueantes;

        ResumenLibros = ArmarResumenLibros(resultado);

        var conProblemas = resultado.Libros.Count(l => l.TieneBloqueantes);
        MensajeEstado = TieneBloqueantes
            ? "Hay problemas que impiden generar el libro. Revise los mensajes en rojo."
            : resultado.Libros.Count == 0
                ? "El cliente no tiene ningún establecimiento registrado en el catálogo. Agréguele al menos uno."
                : $"Listo para generar: 1 archivo con {resultado.Libros.Count} hoja(s) (una por establecimiento)" +
                  (conProblemas > 0 ? $", {conProblemas} con problemas (quedarán en 0)" : "") + ".";

        NotificarContadores();
        GenerarCommand.NotifyCanExecuteChanged();
        GenerarPdfCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    private static string? ArmarResumenLibros(ResultadoProcesamientoVentasMultiple resultado)
    {
        if (resultado.Libros.Count == 0) return null;

        var partes = resultado.Libros
            .OrderBy(l => l.Establecimiento.Numero)
            .Select(l => $"Est. {l.Establecimiento.Numero} ({l.Filas.Count} factura{(l.Filas.Count == 1 ? "" : "s")}{(l.TieneBloqueantes ? ", con problemas" : "")})");

        var palabraHoja = resultado.Libros.Count == 1 ? "hoja" : "hojas";
        return $"Se generará 1 archivo con {resultado.Libros.Count} {palabraHoja}: {string.Join(", ", partes)}.";
    }

    /// <summary>Se llama cada vez que cambia el conjunto de filas.</summary>
    private void NotificarContadores()
    {
        OnPropertyChanged(nameof(TotalFacturas));
        RecalcularResumen();
    }

    /// <summary>Totales en vivo sobre TODAS las filas (no hay incluir/excluir en ventas).</summary>
    private void RecalcularResumen()
    {
        ResumenVentas = RedondeoUtil.Redondear(Filas.Sum(f => f.Ventas));
        ResumenServicios = RedondeoUtil.Redondear(Filas.Sum(f => f.Servicios));
        ResumenIva = RedondeoUtil.Redondear(Filas.Sum(f => f.Iva));
        ResumenTotal = RedondeoUtil.Redondear(Filas.Sum(f => f.Total));
        ResumenDescuadre = Filas.Count(f => f.DescuadreIva);
        ResumenAnuladas = Filas.Count(f => f.Anulada);
    }

    [RelayCommand]
    private void MostrarInfoColores()
    {
        var dialogo = new LeyendaColoresVentasFelDialogo { Owner = Application.Current.MainWindow };
        dialogo.ShowDialog();
    }

    [RelayCommand]
    private void VerDetalle(FilaLibroVentas? fila)
    {
        if (fila is null) return;

        var dialogo = new DetalleFacturaFelDialogo(fila.OrigenFel) { Owner = Application.Current.MainWindow };
        dialogo.ShowDialog();
    }

    /// <summary>
    /// Genera UN SOLO ARCHIVO con UNA HOJA POR CADA establecimiento
    /// registrado del cliente (todas, aunque alguna quede en 0 por no tener
    /// facturas en este pool o por tener un bloqueante propio — ver
    /// <see cref="LibroVentasFelService"/>).
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeGenerar))]
    private void Generar()
    {
        if (ClienteDetectado is null)
        {
            MensajeEstado = "No se pudo determinar el cliente del libro (revise la advertencia sobre el cliente).";
            return;
        }

        if (_ultimoResultado is null || _ultimoResultado.Libros.Count == 0)
        {
            MensajeEstado = "El cliente no tiene ningún establecimiento registrado en el catálogo. Agréguele al menos uno.";
            return;
        }

        var (mes, anio) = ObtenerMesAnioPredominante(Filas);
        var periodo = $"{ParseoUtil.AbreviaturaMesEspanol(mes)} {anio}";

        var dialogo = new SaveFileDialog
        {
            Filter = "Libro de Excel (*.xlsx)|*.xlsx",
            FileName = $"Libro de ventas - {ClienteDetectado.Nombre} - {periodo}.xlsx",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            _generador.Generar(dialogo.FileName, ClienteDetectado, _ultimoResultado.Libros);

            var conProblemas = _ultimoResultado.Libros.Count(l => l.TieneBloqueantes);
            MensajeEstado = $"Libro generado correctamente en: {dialogo.FileName} " +
                $"({_ultimoResultado.Libros.Count} hoja(s), una por establecimiento)." +
                (conProblemas > 0 ? $" {conProblemas} hoja(s) quedaron en 0 por tener problemas (revise los mensajes en rojo)." : "");

            AbrirArchivoGenerado(dialogo.FileName);
        }
        catch (Exception ex)
        {
            MensajeEstado = $"No se pudo generar el archivo: {ex.Message}";
        }
    }

    /// <summary>Versión PDF, lista para imprimir: un solo archivo con una SECCIÓN por establecimiento (ver <see cref="GeneradorLibroVentasPdf"/>).</summary>
    [RelayCommand(CanExecute = nameof(PuedeGenerar))]
    private void GenerarPdf()
    {
        if (ClienteDetectado is null)
        {
            MensajeEstado = "No se pudo determinar el cliente del libro (revise la advertencia sobre el cliente).";
            return;
        }

        if (_ultimoResultado is null || _ultimoResultado.Libros.Count == 0)
        {
            MensajeEstado = "El cliente no tiene ningún establecimiento registrado en el catálogo. Agréguele al menos uno.";
            return;
        }

        // Un renglón de folio inicial por establecimiento: cada uno es un
        // "libro habilitado" físico aparte, así que su numeración no
        // necesariamente continúa la del establecimiento anterior.
        var filasFolio = _ultimoResultado.Libros
            .OrderBy(l => l.Establecimiento.Numero)
            .Select(l => new FolioInicialFila
            {
                Etiqueta = string.IsNullOrWhiteSpace(l.Establecimiento.Nombre)
                    ? $"Folio inicial — Establecimiento {l.Establecimiento.Numero}"
                    : $"Folio inicial — Establecimiento {l.Establecimiento.Numero}: {l.Establecimiento.Nombre}",
                NumeroEstablecimiento = l.Establecimiento.Numero,
            })
            .ToList();

        var dialogoFolio = new FolioInicialDialogo(filasFolio) { Owner = Application.Current.MainWindow };
        if (dialogoFolio.ShowDialog() != true) return;

        var foliosIniciales = filasFolio.ToDictionary(f => f.NumeroEstablecimiento, f => f.FolioInicial);

        var (mes, anio) = ObtenerMesAnioPredominante(Filas);
        var periodo = $"{ParseoUtil.AbreviaturaMesEspanol(mes)} {anio}";

        var dialogo = new SaveFileDialog
        {
            Filter = "Documento PDF (*.pdf)|*.pdf",
            FileName = $"Libro de ventas - {ClienteDetectado.Nombre} - {periodo}.pdf",
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            _generadorPdf.Generar(dialogo.FileName, ClienteDetectado, _ultimoResultado.Libros, foliosIniciales);
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

    private static (int Mes, int Anio) ObtenerMesAnioPredominante(IReadOnlyList<FilaLibroVentas> filas)
    {
        if (filas.Count == 0)
        {
            var hoy = DateTime.Today;
            return (hoy.Month, hoy.Year);
        }

        var grupo = filas.GroupBy(f => new { f.Fecha.Year, f.Fecha.Month }).OrderByDescending(g => g.Count()).First();
        return (grupo.Key.Month, grupo.Key.Year);
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
