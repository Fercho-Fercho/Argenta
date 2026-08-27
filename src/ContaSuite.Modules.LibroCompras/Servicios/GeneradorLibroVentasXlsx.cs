using ClosedXML.Excel;
using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>
/// Genera el archivo .xlsx del libro de Ventas del cliente, con el formato
/// del modelo "Libro de ventas profesional". UN SOLO ARCHIVO trae UNA HOJA
/// POR CADA establecimiento (nombrada "Establecimiento N"), en vez de un
/// archivo separado por establecimiento — así el contador solo maneja un
/// Excel por cliente y mes, aunque tenga varios establecimientos. Las
/// columnas Exportaciones e INGUAT son condicionales por hoja: solo
/// aparecen si ESE establecimiento las necesita (Exporta = Sí / Tipo =
/// Hotel, respectivamente) — ver <see cref="ArmarColumnas"/>. Cada hoja trae
/// su propio encabezado con "Establecimiento No. N" para que quede claro a
/// cuál corresponde — ver <see cref="LibroVentasFelService"/>, que ya arma
/// una fila (posiblemente en 0) por CADA establecimiento registrado del
/// cliente, tenga o no facturas en el pool.
/// </summary>
public sealed class GeneradorLibroVentasXlsx
{
    private const string FormatoMoneda = "#,##0.00;(#,##0.00)";
    private const string FormatoFecha = "dd/mm/yyyy";

    // Amarillo: descuadre de IVA. Único resaltado que aplica en ventas (no hay
    // naranja/rojo: nada se descarta ni se marca para revisión por catálogo).
    // Debe coincidir con FilaDescuadreIvaBrush del proyecto Wpf.
    private static readonly XLColor ColorDescuadreIva = XLColor.FromHtml("#FFF59D");

    public void Generar(string rutaSalida, Cliente cliente, IReadOnlyList<ResultadoLibroEstablecimiento> libros)
    {
        using var libro = new XLWorkbook();

        foreach (var resultado in libros.OrderBy(l => l.Establecimiento.Numero))
        {
            var hoja = libro.Worksheets.Add(NombreHoja(resultado.Establecimiento));
            GenerarHoja(hoja, cliente, resultado.Establecimiento, resultado.Filas, resultado.FilasAparte);
        }

        var carpeta = Path.GetDirectoryName(rutaSalida);
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

        libro.SaveAs(rutaSalida);
    }

    /// <summary>Los nombres de hoja de Excel no admiten : \ / ? * [ ] y tienen un máximo de 31 caracteres.</summary>
    private static string NombreHoja(Establecimiento establecimiento)
    {
        var nombre = $"Establecimiento {establecimiento.Numero}";
        return nombre.Length > 31 ? nombre[..31] : nombre;
    }

    private static void GenerarHoja(
        IXLWorksheet hoja, Cliente cliente, Establecimiento establecimiento,
        IReadOnlyList<FilaLibroVentas> filas, IReadOnlyList<FilaLibroVentas> filasAparte)
    {
        var columnas = ArmarColumnas(establecimiento);
        var indice = columnas.Select((c, i) => (c.Clave, Columna: i + 1)).ToDictionary(x => x.Clave, x => x.Columna);
        var ultimaColumna = columnas.Count;

        EscribirEncabezado(hoja, cliente, establecimiento, filas, columnas, ultimaColumna);

        var filaSiguiente = 8;
        foreach (var item in filas)
        {
            EscribirFila(hoja, filaSiguiente, item, indice);
            filaSiguiente++;
        }

        EscribirTotales(hoja, filas, indice, ultimaColumna, filaSiguiente);

        if (filasAparte.Count > 0)
        {
            EscribirSeccionAparte(hoja, filasAparte, indice, columnas, ultimaColumna, filaSiguiente + 5);
        }

        AjustarAnchoColumnas(hoja, columnas, indice, ultimaColumna);
    }

    /// <summary>
    /// Ancho de cada columna según su contenido. AdjustToContents() por sí
    /// solo no alcanza: ClosedXML lo ignora en celdas fusionadas (todos los
    /// títulos de encabezado lo están, para que no se corten y no obliguen a
    /// la columna A a quedar ancha), así que aquí se refuerza un mínimo por
    /// columna a partir del largo de su título, más un mínimo especial para
    /// "Nombre" (columna F), donde también cae la etiqueta larga de los
    /// totales de pie de página.
    /// </summary>
    private static void AjustarAnchoColumnas(
        IXLWorksheet hoja, List<(string Clave, string Titulo)> columnas, Dictionary<string, int> indice, int ultimaColumna)
    {
        hoja.Columns(1, ultimaColumna).AdjustToContents();

        foreach (var (clave, titulo) in columnas)
        {
            var columna = hoja.Column(indice[clave]);
            var minimo = titulo.Length + 3;
            if (columna.Width < minimo) columna.Width = minimo;
        }

        var colNombre = hoja.Column(indice["Nombre"]);
        var anchoMinimoNombre = Math.Max(35, "Total de Facturas Emitidas y Anuladas:".Length + 2);
        if (colNombre.Width < anchoMinimoNombre) colNombre.Width = anchoMinimoNombre;
    }

    /// <summary>
    /// Columnas del libro, en orden. Exportaciones e INGUAT solo se agregan si
    /// el establecimiento las necesita, así que el resto de columnas (Iva,
    /// Total) se recorren para acomodarse — por eso todo el generador ubica
    /// columnas por CLAVE (diccionario), nunca por letra fija.
    /// </summary>
    private static List<(string Clave, string Titulo)> ArmarColumnas(Establecimiento establecimiento)
    {
        var columnas = new List<(string Clave, string Titulo)>
        {
            ("Fecha", "Fecha"),
            ("Tipo", "Tipo"),
            ("Serie", "Serie"),
            ("Numero", "Número"),
            ("Nit", "Nit"),
            ("Nombre", "NOMBRE"),
            ("Anulado", "Marca de Anulado"),
            ("Ventas", "Ventas"),
            ("Servicios", "Servicios"),
        };

        if (establecimiento.Exporta) columnas.Add(("Exportaciones", "Exportaciones"));
        if (establecimiento.Tipo == TipoCliente.Hotel) columnas.Add(("Inguat", "INGUAT"));

        columnas.Add(("Iva", "Soportado"));
        columnas.Add(("Total", "Facturado"));

        return columnas;
    }

    /// <summary>
    /// Filas 1-4: título, cliente/NIT, establecimiento y mes — todas con
    /// celdas COMBINADAS sobre varias columnas (en vez de una sola celda de
    /// la columna A) para que el texto no se corte y la columna A no quede
    /// forzada a ser ancha solo para acomodar estos títulos.
    /// </summary>
    private static void EscribirEncabezado(
        IXLWorksheet hoja, Cliente cliente, Establecimiento establecimiento, IReadOnlyList<FilaLibroVentas> filas,
        List<(string Clave, string Titulo)> columnas, int ultimaColumna)
    {
        // Punto de corte entre la mitad izquierda (etiqueta/nombre) y la
        // derecha (valor): deja ~3 columnas a la derecha para el NIT/mes.
        var corte = Math.Max(1, ultimaColumna - 3);

        EscribirTituloCombinado(hoja, 1, 1, ultimaColumna, "LIBRO DE VENTAS DE BIENES Y SERVICIOS PRESTADOS", XLAlignmentHorizontalValues.Center);

        EscribirTituloCombinado(hoja, 2, 1, corte, cliente.Nombre, XLAlignmentHorizontalValues.Left);
        EscribirTituloCombinado(hoja, 2, corte + 1, ultimaColumna, $"Nit: {cliente.Nit}", XLAlignmentHorizontalValues.Left);

        // Imprescindible cuando hay varios establecimientos: deja claro a
        // cuál corresponde ESTE archivo (puede haber varios .xlsx del mismo
        // cliente y mes, uno por establecimiento).
        var textoEstablecimiento = string.IsNullOrWhiteSpace(establecimiento.Nombre)
            ? $"Establecimiento No. {establecimiento.Numero}:"
            : $"Establecimiento No. {establecimiento.Numero}: {establecimiento.Nombre}";
        EscribirTituloCombinado(hoja, 3, 1, ultimaColumna, textoEstablecimiento, XLAlignmentHorizontalValues.Left);

        EscribirTituloCombinado(hoja, 4, 1, corte, "CORRESPONDE AL MES DE:", XLAlignmentHorizontalValues.Left);
        EscribirTituloCombinado(hoja, 4, corte + 1, ultimaColumna, ObtenerMesPredominante(filas), XLAlignmentHorizontalValues.Left);

        // Encabezado de columnas (filas 6-7): "Documento" y "Valores Netos"
        // son grupos que se fusionan arriba, con sus subcolumnas debajo;
        // el resto de columnas se fusiona verticalmente (una sola celda alta).
        void TituloSimple(string clave, string texto)
        {
            var columna = columnas.FindIndex(c => c.Clave == clave) + 1;
            var celda = hoja.Cell(6, columna);
            celda.Value = texto;
            celda.Style.Alignment.WrapText = true;
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Range(6, columna, 7, columna).Merge();
        }

        TituloSimple("Fecha", "Fecha");
        TituloSimple("Nit", "Nit");
        TituloSimple("Nombre", "NOMBRE");
        TituloSimple("Anulado", "Marca de Anulado");
        if (columnas.Any(c => c.Clave == "Exportaciones")) TituloSimple("Exportaciones", "Exportaciones");
        if (columnas.Any(c => c.Clave == "Inguat")) TituloSimple("Inguat", "INGUAT");

        var colTipo = columnas.FindIndex(c => c.Clave == "Tipo") + 1;
        var colNumero = columnas.FindIndex(c => c.Clave == "Numero") + 1;
        hoja.Cell(6, colTipo).Value = "Documento";
        hoja.Range(6, colTipo, 6, colNumero).Merge();
        hoja.Cell(6, colTipo).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(7, colTipo).Value = "Tipo";
        hoja.Cell(7, colTipo + 1).Value = "Serie";
        hoja.Cell(7, colNumero).Value = "Número";

        var colVentas = columnas.FindIndex(c => c.Clave == "Ventas") + 1;
        var colServicios = columnas.FindIndex(c => c.Clave == "Servicios") + 1;
        hoja.Cell(6, colVentas).Value = "Valores Netos";
        hoja.Range(6, colVentas, 6, colServicios).Merge();
        hoja.Cell(6, colVentas).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(7, colVentas).Value = "Ventas";
        hoja.Cell(7, colServicios).Value = "Servicios";

        // Iva/Total: dos filas SIN fusionar verticalmente (a diferencia de
        // TituloSimple) porque cada una necesita mostrar texto distinto en
        // la fila 6 ("IVA"/"Total") y en la 7 ("Soportado"/"Facturado");
        // fusionarlas y luego escribir en la fila 7 pisaba el valor en
        // silencio (ClosedXML no muestra el contenido de una celda no-ancla
        // de un rango combinado).
        var colIva = columnas.FindIndex(c => c.Clave == "Iva") + 1;
        hoja.Cell(6, colIva).Value = "IVA";
        hoja.Cell(6, colIva).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(7, colIva).Value = "Soportado";

        var colTotal = columnas.FindIndex(c => c.Clave == "Total") + 1;
        hoja.Cell(6, colTotal).Value = "Total";
        hoja.Cell(6, colTotal).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(7, colTotal).Value = "Facturado";

        var rangoEncabezado = hoja.Range(6, 1, 7, ultimaColumna);
        rangoEncabezado.Style.Font.Bold = true;
        rangoEncabezado.Style.Alignment.WrapText = true;
        rangoEncabezado.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rangoEncabezado.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        rangoEncabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
    }

    /// <summary>Escribe texto en negrita combinando el rango [colInicio, colFin] de la fila dada (o sin combinar, si son la misma columna).</summary>
    private static void EscribirTituloCombinado(
        IXLWorksheet hoja, int fila, int colInicio, int colFin, string texto, XLAlignmentHorizontalValues alineacion)
    {
        var celda = hoja.Cell(fila, colInicio);
        celda.Value = texto;
        celda.Style.Font.Bold = true;
        celda.Style.Alignment.Horizontal = alineacion;
        if (colFin > colInicio) hoja.Range(fila, colInicio, fila, colFin).Merge();
    }

    private static string ObtenerMesPredominante(IReadOnlyList<FilaLibroVentas> filas)
    {
        if (filas.Count == 0)
        {
            var hoy = DateTime.Today;
            return $"{Capitalizar(ParseoUtil.NombreMesEspanol(hoy.Month))} de {hoy.Year}";
        }

        var grupo = filas.GroupBy(f => new { f.Fecha.Year, f.Fecha.Month }).OrderByDescending(g => g.Count()).First();
        return $"{Capitalizar(ParseoUtil.NombreMesEspanol(grupo.Key.Month))} de {grupo.Key.Year}";
    }

    private static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpperInvariant(texto[0]) + texto[1..];

    private static void EscribirFila(IXLWorksheet hoja, int fila, FilaLibroVentas item, Dictionary<string, int> indice)
    {
        var celdaFecha = hoja.Cell(fila, indice["Fecha"]);
        celdaFecha.Value = item.Fecha;
        celdaFecha.Style.NumberFormat.Format = FormatoFecha;

        hoja.Cell(fila, indice["Tipo"]).Value = item.Docto;
        hoja.Cell(fila, indice["Serie"]).Value = item.Serie;
        hoja.Cell(fila, indice["Numero"]).Value = item.NoDoc;
        hoja.Cell(fila, indice["Nit"]).Value = item.Nit;
        hoja.Cell(fila, indice["Nombre"]).Value = item.Nombre;
        hoja.Cell(fila, indice["Anulado"]).Value = item.Anulada ? "Si" : "No";

        EscribirMonto(hoja, fila, indice["Ventas"], item.Ventas);
        EscribirMonto(hoja, fila, indice["Servicios"], item.Servicios);
        if (indice.TryGetValue("Exportaciones", out var colExportaciones)) EscribirMonto(hoja, fila, colExportaciones, item.Exportaciones);
        if (indice.TryGetValue("Inguat", out var colInguat)) EscribirMonto(hoja, fila, colInguat, item.Inguat);
        EscribirMonto(hoja, fila, indice["Iva"], item.Iva);
        EscribirMonto(hoja, fila, indice["Total"], item.Total);

        if (item.DescuadreIva)
        {
            hoja.Range(fila, 1, fila, indice["Total"]).Style.Fill.BackgroundColor = ColorDescuadreIva;
        }
    }

    private static void EscribirMonto(IXLWorksheet hoja, int fila, int columna, decimal valor)
    {
        var celda = hoja.Cell(fila, columna);
        celda.Value = valor;
        celda.Style.NumberFormat.Format = FormatoMoneda;
    }

    private static void EscribirTotales(
        IXLWorksheet hoja, IReadOnlyList<FilaLibroVentas> filas, Dictionary<string, int> indice, int ultimaColumna, int filaTotales)
    {
        hoja.Cell(filaTotales, indice["Nombre"]).Value = "Totales";
        hoja.Cell(filaTotales, indice["Nombre"]).Style.Font.Bold = true;

        EscribirMonto(hoja, filaTotales, indice["Ventas"], RedondeoUtil.Redondear(filas.Sum(f => f.Ventas)));
        EscribirMonto(hoja, filaTotales, indice["Servicios"], RedondeoUtil.Redondear(filas.Sum(f => f.Servicios)));
        if (indice.TryGetValue("Exportaciones", out var colExportaciones))
        {
            EscribirMonto(hoja, filaTotales, colExportaciones, RedondeoUtil.Redondear(filas.Sum(f => f.Exportaciones)));
        }
        if (indice.TryGetValue("Inguat", out var colInguat))
        {
            EscribirMonto(hoja, filaTotales, colInguat, RedondeoUtil.Redondear(filas.Sum(f => f.Inguat)));
        }
        EscribirMonto(hoja, filaTotales, indice["Iva"], RedondeoUtil.Redondear(filas.Sum(f => f.Iva)));
        EscribirMonto(hoja, filaTotales, indice["Total"], RedondeoUtil.Redondear(filas.Sum(f => f.Total)));

        var rangoTotales = hoja.Range(filaTotales, indice["Ventas"], filaTotales, indice["Total"]);
        rangoTotales.Style.Font.Bold = true;
        hoja.Range(filaTotales, 1, filaTotales, ultimaColumna).Style.Border.TopBorder = XLBorderStyleValues.Thin;

        // Etiqueta y cantidad juntas en dos columnas (F y G: las mismas de
        // Nombre y Anulado, que son fijas sin importar las columnas
        // condicionales de Exportaciones/INGUAT que van después).
        var filaFacturas = filaTotales + 2;
        hoja.Cell(filaFacturas, indice["Nombre"]).Value = "Total de Facturas Emitidas y Anuladas:";
        hoja.Cell(filaFacturas, indice["Anulado"]).Value = filas.Count;

        var filaNotasCredito = filaFacturas + 1;
        var totalNotasCredito = filas.Count(f => f.EsNotaCredito);
        hoja.Cell(filaNotasCredito, indice["Nombre"]).Value = "Total de Notas de Credito:";
        hoja.Cell(filaNotasCredito, indice["Anulado"]).Value = totalNotasCredito > 0 ? totalNotasCredito.ToString() : "-";

        var cajaPie = hoja.Range(filaFacturas, indice["Nombre"], filaNotasCredito, indice["Anulado"]);
        cajaPie.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cajaPie.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>
    /// Sección aparte para RANT, RECI, CIVA, NABN (y cualquier otro tipo "No
    /// afecta"): se listan al final del libro, claramente rotulados, sin
    /// afectar la lista principal ni sus totales.
    /// </summary>
    private static void EscribirSeccionAparte(
        IXLWorksheet hoja, IReadOnlyList<FilaLibroVentas> filasAparte, Dictionary<string, int> indice,
        List<(string Clave, string Titulo)> columnas, int ultimaColumna, int filaInicial)
    {
        hoja.Cell(filaInicial, 1).Value = "DOCUMENTOS TIPO RANT, RECI, CIVA Y NABN — no forman parte del libro de ventas ni de sus totales";
        hoja.Cell(filaInicial, 1).Style.Font.Bold = true;
        hoja.Range(filaInicial, 1, filaInicial, ultimaColumna).Merge();

        var filaEncabezado = filaInicial + 1;
        foreach (var (clave, titulo) in columnas)
        {
            var celda = hoja.Cell(filaEncabezado, indice[clave]);
            celda.Value = titulo;
            celda.Style.Font.Bold = true;
        }

        var fila = filaEncabezado + 1;
        foreach (var item in filasAparte)
        {
            EscribirFila(hoja, fila, item, indice);
            fila++;
        }
    }
}
