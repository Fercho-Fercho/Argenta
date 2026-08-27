using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>
/// Genera una versión PDF, lista para imprimir, del libro de Ventas: UN SOLO
/// PDF con una o más SECCIONES por establecimiento (cada una arranca en hoja
/// nueva). Cada establecimiento es un libro aparte a efectos de folio: el
/// folio de la primera hoja de cada uno lo indica el usuario (no continúa el
/// del establecimiento anterior — ver <paramref name="foliosIniciales"/> de
/// <see cref="Generar"/>). Si un establecimiento no cabe en una sola hoja,
/// cada hoja "extra" cierra con una fila "Van" (el acumulado hasta ahí) y la
/// siguiente abre con "Viene" — solo la ÚLTIMA hoja de ESE establecimiento
/// dice "Total". El título del libro, cliente/NIT, establecimiento, mes y
/// títulos de columna se repiten arriba de cada hoja — ver
/// <see cref="UtilPdfLibro.EscribirEncabezadoPagina"/>.
/// </summary>
public sealed class GeneradorLibroVentasPdf
{
    private static readonly Color ColorDescuadreIva = new(255, 245, 157); // #FFF59D, igual que el Excel.

    /// <param name="foliosIniciales">
    /// Folio de la primera hoja de cada establecimiento, por
    /// <see cref="Establecimiento.Numero"/>. Si a alguno no le llega valor
    /// (no debería pasar: la pantalla pide uno por cada establecimiento del
    /// resultado), se usa 1 como respaldo.
    /// </param>
    public void Generar(
        string rutaSalida, Cliente cliente, IReadOnlyList<ResultadoLibroEstablecimiento> libros,
        IReadOnlyDictionary<int, int> foliosIniciales)
    {
        var documento = UtilPdfLibro.CrearDocumento();

        foreach (var resultado in libros.OrderBy(l => l.Establecimiento.Numero))
        {
            var folioInicial = foliosIniciales.GetValueOrDefault(resultado.Establecimiento.Numero, 1);
            GenerarPaginasEstablecimiento(documento, cliente, resultado.Establecimiento, resultado.Filas, resultado.FilasAparte, folioInicial);
        }

        UtilPdfLibro.GuardarPdf(documento, rutaSalida);
    }

    private static void GenerarPaginasEstablecimiento(
        Document documento, Cliente cliente, Establecimiento establecimiento,
        IReadOnlyList<FilaLibroVentas> filas, IReadOnlyList<FilaLibroVentas> filasAparte, int folioInicial)
    {
        var columnasBase = ArmarColumnasBase(establecimiento);
        var mesTexto = ObtenerMesPredominante(filas);
        var lineaEstablecimiento = string.IsNullOrWhiteSpace(establecimiento.Nombre)
            ? $"Establecimiento No. {establecimiento.Numero}"
            : $"Establecimiento No. {establecimiento.Numero}: {establecimiento.Nombre}";

        // Ventas: siempre trae la línea de establecimiento, y sus títulos de
        // columna más largos ("Marca de Anulado", "IVA Soportado"...) envuelven
        // a 2 líneas en columnas angostas — más alto que Compras.
        var paginas = UtilPdfLibro.Paginar(filas, UtilPdfLibro.CapacidadFilasPorPagina(conLineaExtra: true, titulosColumnaEnvueltos: true));

        List<(string Clave, string Titulo, double AnchoCm)>? columnas = null;
        Dictionary<string, int>? indice = null;
        Section ultimaSeccion = null!;
        var acumulado = new SumasVentas();

        for (var i = 0; i < paginas.Count; i++)
        {
            var seccion = UtilPdfLibro.AgregarSeccion(documento, conLineaExtra: true, titulosColumnaEnvueltos: true);
            columnas ??= UtilPdfLibro.NormalizarAnchos(columnasBase, UtilPdfLibro.AnchoDisponibleCm(seccion));
            indice ??= columnas.Select((c, idx) => (c.Clave, Columna: idx)).ToDictionary(x => x.Clave, x => x.Columna);

            UtilPdfLibro.EscribirEncabezadoPagina(
                seccion,
                folioInicial + i,
                "LIBRO DE VENTAS DE BIENES Y SERVICIOS PRESTADOS",
                cliente.Nombre,
                cliente.Nit,
                lineaEstablecimiento,
                mesTexto,
                columnas);

            var tabla = UtilPdfLibro.CrearTablaDatos(seccion, columnas);
            var esPrimera = i == 0;
            var esUltima = i == paginas.Count - 1;

            if (!esPrimera)
            {
                UtilPdfLibro.EscribirFilaResumen(tabla, "Viene", "Nombre", indice, acumulado.ARedondeado(indice));
            }

            var anchoNombreCm = columnas.First(c => c.Clave == "Nombre").AnchoCm;
            foreach (var item in paginas[i]) EscribirFila(tabla, item, indice, anchoNombreCm);
            acumulado = acumulado.Mas(paginas[i], indice);

            var etiquetaCierre = esUltima ? (paginas.Count > 1 ? "Total" : "Totales") : "Van";
            UtilPdfLibro.EscribirFilaResumen(tabla, etiquetaCierre, "Nombre", indice, acumulado.ARedondeado(indice));

            ultimaSeccion = seccion;
        }

        EscribirCajaResumen(ultimaSeccion, filas);

        if (filasAparte.Count > 0)
        {
            EscribirSeccionAparte(ultimaSeccion, filasAparte, columnas!, indice!);
        }
    }

    /// <summary>
    /// Acumulado (sin redondear, para que el "Total" final de la última hoja
    /// coincida centavo a centavo con sumar todo de una vez) de los montos
    /// que se muestran en las filas "Van"/"Viene"/"Total". Exportaciones e
    /// INGUAT son condicionales por establecimiento, así que se guardan en un
    /// diccionario en vez de campos fijos.
    /// </summary>
    private sealed class SumasVentas
    {
        private readonly Dictionary<string, decimal> _valores = [];

        public SumasVentas Mas(IEnumerable<FilaLibroVentas> bloque, Dictionary<string, int> indice)
        {
            var lista = bloque as ICollection<FilaLibroVentas> ?? bloque.ToList();
            var resultado = new SumasVentas();
            foreach (var (clave, valor) in _valores) resultado._valores[clave] = valor;

            void Sumar(string clave, Func<FilaLibroVentas, decimal> selector) =>
                resultado._valores[clave] = resultado._valores.GetValueOrDefault(clave) + lista.Sum(selector);

            Sumar("Ventas", f => f.Ventas);
            Sumar("Servicios", f => f.Servicios);
            if (indice.ContainsKey("Exportaciones")) Sumar("Exportaciones", f => f.Exportaciones);
            if (indice.ContainsKey("Inguat")) Sumar("Inguat", f => f.Inguat);
            Sumar("Iva", f => f.Iva);
            Sumar("Total", f => f.Total);

            return resultado;
        }

        public Dictionary<string, decimal> ARedondeado(Dictionary<string, int> indice) =>
            _valores
                .Where(kv => indice.ContainsKey(kv.Key))
                .ToDictionary(kv => kv.Key, kv => RedondeoUtil.Redondear(kv.Value));
    }

    /// <summary>Igual orden/condiciones que <c>GeneradorLibroVentasXlsx.ArmarColumnas</c>.</summary>
    private static (string Clave, string Titulo, double AnchoBase)[] ArmarColumnasBase(Establecimiento establecimiento)
    {
        var columnas = new List<(string Clave, string Titulo, double AnchoBase)>
        {
            ("Fecha", "Fecha", 2.1),
            ("Tipo", "Tipo", 1.3),
            ("Serie", "Serie", 1.5),
            ("Numero", "Número", 2.1),
            ("Nit", "Nit", 2.1),
            ("Nombre", "NOMBRE", 6.0),
            ("Anulado", "Marca de Anulado", 1.8),
            ("Ventas", "Ventas", 2.1),
            ("Servicios", "Servicios", 2.0),
        };

        if (establecimiento.Exporta) columnas.Add(("Exportaciones", "Exportaciones", 2.0));
        if (establecimiento.Tipo == TipoCliente.Hotel) columnas.Add(("Inguat", "INGUAT", 1.8));

        columnas.Add(("Iva", "IVA Soportado", 2.0));
        columnas.Add(("Total", "Total Facturado", 2.1));

        return [.. columnas];
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

    private static void EscribirFila(Table tabla, FilaLibroVentas item, Dictionary<string, int> indice, double anchoNombreCm)
    {
        var fila = tabla.AddRow();

        fila.Cells[indice["Fecha"]].AddParagraph(item.Fecha.ToString("dd/MM/yyyy"));
        fila.Cells[indice["Tipo"]].AddParagraph(item.Docto);
        fila.Cells[indice["Serie"]].AddParagraph(item.Serie);
        fila.Cells[indice["Numero"]].AddParagraph(item.NoDoc);
        fila.Cells[indice["Nit"]].AddParagraph(item.Nit);
        fila.Cells[indice["Nombre"]].AddParagraph(UtilPdfLibro.TextoSinEnvolver(item.Nombre, anchoNombreCm));
        fila.Cells[indice["Anulado"]].AddParagraph(item.Anulada ? "Si" : "No");

        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Ventas"]], item.Ventas);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Servicios"]], item.Servicios);
        if (indice.TryGetValue("Exportaciones", out var colExportaciones)) UtilPdfLibro.EscribirMonto(fila.Cells[colExportaciones], item.Exportaciones);
        if (indice.TryGetValue("Inguat", out var colInguat)) UtilPdfLibro.EscribirMonto(fila.Cells[colInguat], item.Inguat);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Iva"]], item.Iva);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Total"]], item.Total);

        if (item.DescuadreIva) fila.Shading.Color = ColorDescuadreIva;
    }

    /// <summary>Caja con "Total de Facturas Emitidas y Anuladas" / "Total de Notas de Credito", igual que el pie del Excel — solo en la última hoja del establecimiento.</summary>
    private static void EscribirCajaResumen(Section seccion, IReadOnlyList<FilaLibroVentas> filas)
    {
        var tabla = seccion.AddTable();
        tabla.Format.SpaceBefore = "0.3cm";
        tabla.Borders.Width = 0.4;
        tabla.AddColumn("6cm");
        tabla.AddColumn("2.5cm");

        var filaFacturas = tabla.AddRow();
        filaFacturas.Cells[0].AddParagraph("Total de Facturas Emitidas y Anuladas:");
        filaFacturas.Cells[1].AddParagraph(filas.Count.ToString());
        filaFacturas.Cells[1].Format.Alignment = ParagraphAlignment.Right;

        var totalNotasCredito = filas.Count(f => f.EsNotaCredito);
        var filaNotas = tabla.AddRow();
        filaNotas.Cells[0].AddParagraph("Total de Notas de Credito:");
        filaNotas.Cells[1].AddParagraph(totalNotasCredito > 0 ? totalNotasCredito.ToString() : "-");
        filaNotas.Cells[1].Format.Alignment = ParagraphAlignment.Right;
    }

    /// <summary>
    /// Sección aparte (RANT/RECI/CIVA/NABN), igual que en el Excel: no forma
    /// parte de la lista principal ni de sus totales (ni de "Van"/"Viene").
    /// </summary>
    private static void EscribirSeccionAparte(
        Section seccion, IReadOnlyList<FilaLibroVentas> filasAparte,
        List<(string Clave, string Titulo, double AnchoCm)> columnas, Dictionary<string, int> indice)
    {
        var titulo = seccion.AddParagraph();
        titulo.Format.SpaceBefore = "0.6cm";
        titulo.Format.SpaceAfter = "0.15cm";
        titulo.Format.Font.Bold = true;
        titulo.Format.Font.Size = 8;
        titulo.AddText("DOCUMENTOS TIPO RANT, RECI, CIVA Y NABN — no forman parte del libro de ventas ni de sus totales");

        var tabla = UtilPdfLibro.CrearTablaDatos(seccion, columnas);
        var filaTitulos = tabla.AddRow();
        filaTitulos.Format.Font.Bold = true;
        for (int i = 0; i < columnas.Count; i++) filaTitulos.Cells[i].AddParagraph(columnas[i].Titulo);

        var anchoNombreCm = columnas.First(c => c.Clave == "Nombre").AnchoCm;
        foreach (var item in filasAparte) EscribirFila(tabla, item, indice, anchoNombreCm);
    }
}
