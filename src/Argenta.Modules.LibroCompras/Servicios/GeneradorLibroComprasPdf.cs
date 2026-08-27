using Argenta.Core.Utilidades;
using Argenta.Data.Entidades;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Genera una versión PDF, lista para imprimir, de la tabla de facturas del
/// libro de compras (las mismas columnas y totales del .xlsx). A diferencia
/// del Excel: el título del libro, el cliente/NIT, el mes y los títulos de
/// columna se repiten arriba de CADA hoja, cada hoja trae su folio ("Folio N",
/// a partir del <c>folioInicial</c> que indique el usuario) arriba a la
/// derecha, y si el libro no cabe en una sola hoja, cada hoja "extra" cierra
/// con una fila "Van" (el acumulado hasta ahí) y la siguiente abre con "Viene"
/// (el mismo acumulado) — solo la ÚLTIMA hoja dice "Total". Ver
/// <see cref="UtilPdfLibro.Paginar{T}"/> y <see cref="UtilPdfLibro.EscribirFilaResumen"/>.
/// </summary>
public sealed class GeneradorLibroComprasPdf
{
    /// <summary>
    /// Ancho "base" orientativo de cada columna (no en centímetros reales):
    /// se normaliza contra el ancho disponible de la página en
    /// <see cref="UtilPdfLibro.NormalizarAnchos"/>, así que solo importan las
    /// proporciones relativas entre columnas.
    /// </summary>
    private static readonly (string Clave, string Titulo, double AnchoBase)[] ColumnasBase =
    [
        ("No", "No.", 1.0),
        ("Fecha", "Fecha", 2.0),
        ("Docto", "Docto", 1.6),
        ("Serie", "Serie", 1.6),
        ("NoDoc", "No. Doc", 2.2),
        ("Nit", "Nit", 2.1),
        ("Proveedor", "Proveedor", 6.0),
        ("Compras", "Compras", 2.1),
        ("Servicios", "Servicios", 2.0),
        ("Exento", "Exento", 1.9),
        ("Iva", "Iva", 1.9),
        ("Total", "Total", 2.1),
    ];

    // Mismos colores que GeneradorLibroComprasXlsx / Styles/Colores.xaml.
    private static readonly Color ColorRevision = new(255, 201, 102); // #FFC966
    private static readonly Color ColorDescuadreIva = new(255, 245, 157); // #FFF59D

    public void Generar(
        string rutaSalida,
        Cliente cliente,
        IReadOnlyList<FilaLibroCompras> filas,
        IReadOnlyList<FilaLibroCompras> filasAparte,
        int folioInicial)
    {
        var filasIncluidas = filas.Where(f => f.Incluida).ToList();
        var filasApartadasIncluidas = filasAparte.Where(f => f.Incluida).ToList();
        LibroComprasService.NumerarFilas(filasIncluidas);

        var documento = UtilPdfLibro.CrearDocumento();
        // Compras: encabezado sin línea de establecimiento y con títulos de
        // columna cortos (no envuelven a 2 líneas) — más angosto que Ventas.
        var paginas = UtilPdfLibro.Paginar(filasIncluidas, UtilPdfLibro.CapacidadFilasPorPagina(conLineaExtra: false, titulosColumnaEnvueltos: false));
        var mesTexto = ObtenerMesPredominante(filasIncluidas);

        List<(string Clave, string Titulo, double AnchoCm)>? columnas = null;
        Dictionary<string, int>? indice = null;
        Section ultimaSeccion = null!;
        var acumulado = new SumasCompras();

        for (var i = 0; i < paginas.Count; i++)
        {
            var seccion = UtilPdfLibro.AgregarSeccion(documento, conLineaExtra: false, titulosColumnaEnvueltos: false);
            columnas ??= UtilPdfLibro.NormalizarAnchos(ColumnasBase, UtilPdfLibro.AnchoDisponibleCm(seccion));
            indice ??= columnas.Select((c, idx) => (c.Clave, Columna: idx)).ToDictionary(x => x.Clave, x => x.Columna);

            UtilPdfLibro.EscribirEncabezadoPagina(
                seccion,
                folioInicial + i,
                "LIBRO DE COMPRAS DE BIENES Y SERVICIOS ADQUIRIDOS",
                cliente.Nombre,
                cliente.Nit,
                lineaExtra: null,
                mesTexto,
                columnas);

            var tabla = UtilPdfLibro.CrearTablaDatos(seccion, columnas);
            var esPrimera = i == 0;
            var esUltima = i == paginas.Count - 1;

            if (!esPrimera)
            {
                UtilPdfLibro.EscribirFilaResumen(tabla, "Viene", "Proveedor", indice, acumulado.ARedondeado());
            }

            var anchoProveedorCm = columnas.First(c => c.Clave == "Proveedor").AnchoCm;
            foreach (var item in paginas[i]) EscribirFila(tabla, item, indice, anchoProveedorCm);
            acumulado = acumulado.Mas(paginas[i]);

            var etiquetaCierre = esUltima ? (paginas.Count > 1 ? "Total" : "TOTALES") : "Van";
            UtilPdfLibro.EscribirFilaResumen(tabla, etiquetaCierre, "Proveedor", indice, acumulado.ARedondeado());

            ultimaSeccion = seccion;
        }

        if (filasApartadasIncluidas.Count > 0)
        {
            EscribirSeccionAparte(ultimaSeccion, filasApartadasIncluidas, columnas!, indice!);
        }

        UtilPdfLibro.GuardarPdf(documento, rutaSalida);
    }

    /// <summary>
    /// Acumulado (sin redondear, para que el "Total" final de la última hoja
    /// coincida centavo a centavo con sumar todo de una vez) de los montos
    /// que se muestran en las filas "Van"/"Viene"/"Total".
    /// </summary>
    private readonly record struct SumasCompras(decimal Compras, decimal Servicios, decimal Exento, decimal Iva, decimal Total)
    {
        public SumasCompras Mas(IEnumerable<FilaLibroCompras> bloque)
        {
            var lista = bloque as ICollection<FilaLibroCompras> ?? bloque.ToList();
            return new SumasCompras(
                Compras + lista.Sum(f => f.Compras),
                Servicios + lista.Sum(f => f.Servicios),
                Exento + lista.Sum(f => f.Exento),
                Iva + lista.Sum(f => f.Iva),
                Total + lista.Sum(f => f.Total));
        }

        public Dictionary<string, decimal> ARedondeado() => new()
        {
            ["Compras"] = RedondeoUtil.Redondear(Compras),
            ["Servicios"] = RedondeoUtil.Redondear(Servicios),
            ["Exento"] = RedondeoUtil.Redondear(Exento),
            ["Iva"] = RedondeoUtil.Redondear(Iva),
            ["Total"] = RedondeoUtil.Redondear(Total),
        };
    }

    private static string ObtenerMesPredominante(IReadOnlyList<FilaLibroCompras> filas)
    {
        var (mes, anio) = PeriodoUtil.ObtenerMesAnioPredominante(filas);
        return $"{Capitalizar(ParseoUtil.NombreMesEspanol(mes))} de {anio}";
    }

    private static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpperInvariant(texto[0]) + texto[1..];

    private static void EscribirFila(Table tabla, FilaLibroCompras item, Dictionary<string, int> indice, double anchoProveedorCm)
    {
        var fila = tabla.AddRow();

        if (item.Numero.HasValue) fila.Cells[indice["No"]].AddParagraph(item.Numero.Value.ToString());
        fila.Cells[indice["Fecha"]].AddParagraph(item.Fecha.ToString("dd/MM/yyyy"));
        fila.Cells[indice["Docto"]].AddParagraph(item.Docto);
        fila.Cells[indice["Serie"]].AddParagraph(item.Serie);
        fila.Cells[indice["NoDoc"]].AddParagraph(item.NoDoc);
        fila.Cells[indice["Nit"]].AddParagraph(item.Nit);
        fila.Cells[indice["Proveedor"]].AddParagraph(UtilPdfLibro.TextoSinEnvolver(item.Proveedor, anchoProveedorCm));

        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Compras"]], item.Compras);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Servicios"]], item.Servicios);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Exento"]], item.Exento);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Iva"]], item.Iva);
        UtilPdfLibro.EscribirMonto(fila.Cells[indice["Total"]], item.Total);

        // A diferencia del Excel (que parte la fila en dos colores cuando
        // aplican ambos casos a la vez), aquí se prioriza el resaltado de
        // revisión de proveedor por ser el más urgente de los dos.
        var marcadoNaranja = item.ProveedorNoEncontrado || item.MarcadoParaRevisar;
        if (marcadoNaranja) fila.Shading.Color = ColorRevision;
        else if (item.DescuadreIva) fila.Shading.Color = ColorDescuadreIva;
    }

    /// <summary>
    /// Sección aparte (CIVA/NABN), igual que en el Excel: no forma parte de
    /// la lista principal ni de sus totales (ni de "Van"/"Viene"). Su propia
    /// fila de títulos NO se repite si esta sub-tabla llega a saltar de hoja
    /// (solo el encabezado de página se repite); es una limitación aceptada
    /// porque esta sección suele ser corta.
    /// </summary>
    private static void EscribirSeccionAparte(
        Section seccion, List<FilaLibroCompras> filasAparte,
        List<(string Clave, string Titulo, double AnchoCm)> columnas, Dictionary<string, int> indice)
    {
        var titulo = seccion.AddParagraph();
        titulo.Format.SpaceBefore = "0.6cm";
        titulo.Format.SpaceAfter = "0.15cm";
        titulo.Format.Font.Bold = true;
        titulo.Format.Font.Size = 8;
        titulo.AddText("FACTURAS TIPO CIVA Y NABN — no forman parte del libro de compras ni de sus totales");

        var tabla = UtilPdfLibro.CrearTablaDatos(seccion, columnas);
        var filaTitulos = tabla.AddRow();
        filaTitulos.Format.Font.Bold = true;
        for (int i = 0; i < columnas.Count; i++) filaTitulos.Cells[i].AddParagraph(columnas[i].Titulo);

        var anchoProveedorCm = columnas.First(c => c.Clave == "Proveedor").AnchoCm;
        int numero = 1;
        foreach (var item in filasAparte)
        {
            item.Numero = numero++;
            EscribirFila(tabla, item, indice, anchoProveedorCm);
        }
    }
}
