using System.Globalization;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Utilidades comunes a los generadores de PDF de libro de compras y de
/// ventas: creación del documento (tamaño/orientación/fuente), cálculo de
/// anchos de columna proporcionales al ancho disponible de la página, y el
/// encabezado de página que se repite en cada hoja (folio, título,
/// cliente/NIT y títulos de columna) — ver <see cref="EscribirEncabezadoPagina"/>.
/// </summary>
internal static class UtilPdfLibro
{
    private static readonly Color ColorEncabezado = new(217, 225, 242); // #D9E1F2, igual que el Excel.

    // Geometría de página: constantes únicas (no leídas desde un Section ya
    // creado) para que CapacidadFilasPorPagina() se pueda calcular ANTES de
    // crear ninguna sección — hace falta saber cuántas filas caben por hoja
    // para poder repartir los datos en bloques (uno por sección/página) antes
    // de escribir nada.
    //
    // HeaderDistance (el "borde" antes del título) y BottomMargin (el borde
    // después de la última fila) son un estilo fijo, igual para los dos
    // libros. TopMargin, en cambio, YA NO es un valor compartido: se calcula
    // por libro con CalcularTopMarginCm(), a partir de cuántas líneas de
    // texto trae SU encabezado — Ventas trae una línea más (el
    // "Establecimiento No. N") y sus títulos de columna más largos
    // ("Marca de Anulado", "IVA Soportado"...) envuelven a 2 líneas, así que
    // necesita más alto que Compras. Antes se usaba el mismo TopMargin
    // "peor caso" (el de Ventas) para los dos libros, y eso le dejaba a
    // Compras casi 0.7cm de espacio en blanco de más entre los títulos de
    // columna y la primera fila, además de una hoja menos aprovechada.
    private static readonly Orientation OrientacionPagina = Orientation.Landscape;
    private static readonly PageFormat FormatoPagina = PageFormat.Letter;
    private const double BottomMarginCm = 0.1;
    private const double LeftMarginCm = 1.0;
    private const double RightMarginCm = 1.0;
    private const double HeaderDistanceCm = 1.1;

    // Alturas de línea (7.5pt/9pt/11pt con line-height ~1.2, más el
    // SpaceAfter que de verdad se le puso a cada párrafo en
    // EscribirEncabezadoPagina) usadas para calcular el TopMargin exacto que
    // necesita cada libro — ver CalcularTopMarginCm().
    private const double AlturaLineaFolioCm = 0.381;    // 9pt, sin espacio extra
    private const double AlturaLineaTituloCm = 0.566;   // 11pt + 0.1cm de SpaceAfter
    private const double AlturaLineaClienteCm = 0.381;  // 9pt
    private const double AlturaLineaExtraCm = 0.381;    // 9pt ("Establecimiento No. N"), solo Ventas
    private const double AlturaLineaMesCm = 0.481;      // 9pt + 0.1cm de SpaceAfter
    private const double AlturaTitulosColumna1LineaCm = 0.42;
    private const double AlturaTitulosColumna2LineasCm = 0.78;
    private const double ColchonHeaderCm = 0.15; // margen de seguridad chico, por si el cálculo se queda corto

    /// <summary>
    /// Alto real (en cm) del contenido del header más un colchón chico, dado
    /// si el libro trae la línea de establecimiento y si sus títulos de
    /// columna necesitan 2 líneas para no cortarse. Se usa para calcular
    /// tanto <see cref="AgregarSeccion"/> como <see cref="CapacidadFilasPorPagina"/> —
    /// deben coincidir SIEMPRE, así que este es el único lugar que hace la suma.
    /// </summary>
    private static double CalcularTopMarginCm(bool conLineaExtra, bool titulosColumnaEnvueltos)
    {
        var alturaContenido = AlturaLineaFolioCm + AlturaLineaTituloCm + AlturaLineaClienteCm
            + (conLineaExtra ? AlturaLineaExtraCm : 0)
            + AlturaLineaMesCm
            + (titulosColumnaEnvueltos ? AlturaTitulosColumna2LineasCm : AlturaTitulosColumna1LineaCm)
            + ColchonHeaderCm;

        return HeaderDistanceCm + alturaContenido;
    }

    /// <summary>
    /// Altura "segura" de una fila de una sola línea (7.5pt), con margen para
    /// que ocasionalmente una fila envuelva a 2 líneas (nombre de
    /// cliente/proveedor largo) sin que la página se desborde.
    ///
    /// Re-medido con un libro real de 120 facturas (Compras): con el valor
    /// anterior (0.36cm, ~50 filas presupuestadas por hoja) MigraDoc
    /// desbordaba cada sección a una SEGUNDA hoja física antes de llegar a
    /// escribir la fila "Van" — la hoja de verdad aguantaba ~41 filas, no 50
    /// (18.011cm de cuerpo disponible / 41 ≈ 0.439cm/fila real). Ahí es
    /// donde aparecía el folio repetido y el "Van"/"Viene" descolocado que
    /// reportó el usuario: cada "página" lógica de <see cref="Paginar{T}"/>
    /// terminaba ocupando 2 páginas físicas, así que el corte manual ya no
    /// coincidía con el corte real de MigraDoc. 0.5cm deja ~14% de colchón
    /// sobre ese mínimo real (mismo criterio que antes: suficiente para
    /// algún envuelto ocasional, sin desperdiciar tanto como para que la
    /// hoja se vea con mucho espacio en blanco).
    /// </summary>
    private const double AlturaFilaSeguraCm = 0.5;

    public static Document CrearDocumento()
    {
        ResolvedorFuentesPdf.Asegurar();

        var documento = new Document();
        documento.Styles.Normal.Font.Name = ResolvedorFuentesPdf.NombreFuente;
        documento.Styles.Normal.Font.Size = 7.5;

        return documento;
    }

    /// <summary>
    /// <c>Document.DefaultPageSetup</c> queda inmutable ("frozen") en cuanto
    /// se accede a él, así que el tamaño/orientación/márgenes se configuran
    /// directamente en el <c>PageSetup</c> de CADA sección (no se puede
    /// compartir un objeto <see cref="PageSetup"/> entre dos secciones).
    ///
    /// TopMargin se calcula con <see cref="CalcularTopMarginCm"/> a partir
    /// del contenido REAL del encabezado de este libro (<paramref name="conLineaExtra"/>,
    /// <paramref name="titulosColumnaEnvueltos"/>) — no un valor "peor caso"
    /// compartido — porque en MigraDoc el header de página vive en la franja
    /// entre HeaderDistance y TopMargin, y si el contenido no cabe ahí se
    /// dibuja ENCIMA de la primera fila de datos en vez de empujarla hacia
    /// abajo.
    /// </summary>
    public static Section AgregarSeccion(Document documento, bool conLineaExtra, bool titulosColumnaEnvueltos)
    {
        var seccion = documento.AddSection();
        seccion.PageSetup.Orientation = OrientacionPagina;
        seccion.PageSetup.PageFormat = FormatoPagina;
        seccion.PageSetup.HeaderDistance = Unit.FromCentimeter(HeaderDistanceCm);
        seccion.PageSetup.TopMargin = Unit.FromCentimeter(CalcularTopMarginCm(conLineaExtra, titulosColumnaEnvueltos));
        seccion.PageSetup.BottomMargin = Unit.FromCentimeter(BottomMarginCm);
        seccion.PageSetup.LeftMargin = Unit.FromCentimeter(LeftMarginCm);
        seccion.PageSetup.RightMargin = Unit.FromCentimeter(RightMarginCm);
        return seccion;
    }

    /// <summary>
    /// Cuántas filas de tabla (dato, "Van", "Viene" o el total final — todas
    /// de una sola línea) caben con margen de seguridad en el cuerpo de una
    /// página, dada la geometría que usaría <see cref="AgregarSeccion"/> con
    /// los MISMOS parámetros. Se usa para repartir los datos en bloques
    /// ANTES de crear las secciones (una por página) — ver <see cref="Paginar{T}"/>.
    /// </summary>
    public static int CapacidadFilasPorPagina(bool conLineaExtra, bool titulosColumnaEnvueltos)
    {
        PageSetup.GetPageSize(FormatoPagina, out var anchoVertical, out var altoVertical);
        var altoPagina = OrientacionPagina == Orientation.Landscape ? anchoVertical : altoVertical;
        var topMarginCm = CalcularTopMarginCm(conLineaExtra, titulosColumnaEnvueltos);
        var altoDisponible = altoPagina.Centimeter - topMarginCm - BottomMarginCm;
        return Math.Max(10, (int)(altoDisponible / AlturaFilaSeguraCm));
    }

    /// <summary>
    /// Reparte <paramref name="filas"/> en bloques, uno por página: cada
    /// bloque reserva 1 fila para el renglón de cierre ("Van" o el total
    /// final) y, si no es el primer bloque, otra más para "Viene" al inicio.
    /// Si <paramref name="filas"/> está vacío, igual devuelve UN bloque vacío
    /// (así la hoja del establecimiento sin facturas se sigue generando con
    /// su encabezado completo, en 0 — comportamiento ya existente).
    /// </summary>
    public static List<List<T>> Paginar<T>(IReadOnlyList<T> filas, int capacidadFilas)
    {
        var paginas = new List<List<T>>();
        var i = 0;
        var primera = true;

        while (i < filas.Count)
        {
            var capacidadDatos = Math.Max(1, capacidadFilas - (primera ? 1 : 2));
            var tomar = Math.Min(capacidadDatos, filas.Count - i);
            paginas.Add(filas.Skip(i).Take(tomar).ToList());
            i += tomar;
            primera = false;
        }

        return paginas.Count == 0 ? [[]] : paginas;
    }

    /// <summary>
    /// OJO: <c>PageSetup.PageWidth</c> solo trae un valor real cuando
    /// <c>PageFormat == Custom</c>; para formatos con nombre (Letter, A4...)
    /// hay que resolverlo con el método estático <c>GetPageSize</c> — leer
    /// <c>PageWidth</c> directamente da 0 y produce columnas invisiblemente
    /// angostas. También hay que invertir ancho/alto a mano en horizontal:
    /// <c>GetPageSize</c> siempre devuelve las medidas en vertical.
    /// </summary>
    public static double AnchoDisponibleCm(Section seccion)
    {
        var setup = seccion.PageSetup;
        PageSetup.GetPageSize(setup.PageFormat, out var anchoVertical, out var altoVertical);
        var anchoPagina = setup.Orientation == Orientation.Landscape ? altoVertical : anchoVertical;
        return anchoPagina.Centimeter - setup.LeftMargin.Centimeter - setup.RightMargin.Centimeter;
    }

    /// <summary>
    /// Reparte el ancho disponible entre las columnas guardando sus
    /// proporciones relativas (<paramref name="columnas"/> trae un "ancho
    /// base" orientativo por columna, no en centímetros reales), para que
    /// TODAS quepan siempre en una sola página de ancho, sea cual sea el
    /// tamaño/orientación configurados.
    /// </summary>
    public static List<(string Clave, string Titulo, double AnchoCm)> NormalizarAnchos(
        IEnumerable<(string Clave, string Titulo, double AnchoBase)> columnas, double anchoDisponibleCm)
    {
        var lista = columnas.ToList();
        var sumaBase = lista.Sum(c => c.AnchoBase);
        var factor = sumaBase <= 0 ? 1 : anchoDisponibleCm / sumaBase;
        return lista.Select(c => (c.Clave, c.Titulo, AnchoCm: c.AnchoBase * factor)).ToList();
    }

    /// <summary>
    /// Encabezado de PÁGINA (no de tabla): en MigraDoc, todo lo que se agrega
    /// a <c>seccion.Headers.Primary</c> se repite automáticamente arriba de
    /// CADA hoja de la sección — así nunca se pierde de vista el título del
    /// libro, el cliente ni los títulos de columna. Como cada página de datos
    /// es AHORA su propia sección (una por bloque de <see cref="Paginar{T}"/>,
    /// para poder controlar exactamente dónde caen "Van"/"Viene"/el total),
    /// el folio YA NO se resuelve con el <see cref="Paragraph.AddPageField"/>
    /// automático de MigraDoc (que cuenta hojas físicas de TODO el
    /// documento) — se recibe como número ya calculado por el llamador,
    /// porque el folio inicial de cada libro/establecimiento lo indica el
    /// usuario y puede no continuar el del anterior.
    /// </summary>
    public static void EscribirEncabezadoPagina(
        Section seccion,
        int numeroFolio,
        string tituloLibro,
        string clienteNombre,
        string clienteNit,
        string? lineaExtra,
        string mesTexto,
        List<(string Clave, string Titulo, double AnchoCm)> columnas)
    {
        var encabezado = seccion.Headers.Primary;

        var folio = encabezado.AddParagraph();
        folio.Format.Alignment = ParagraphAlignment.Right;
        folio.Format.Font.Size = 9;
        folio.AddFormattedText($"Folio {numeroFolio}", TextFormat.Bold);

        var tituloParrafo = encabezado.AddParagraph(tituloLibro);
        tituloParrafo.Format.Font.Bold = true;
        tituloParrafo.Format.Font.Size = 11;
        tituloParrafo.Format.Alignment = ParagraphAlignment.Center;
        tituloParrafo.Format.SpaceAfter = "0.1cm";

        var anchoTotal = columnas.Sum(c => c.AnchoCm);

        var lineaCliente = encabezado.AddParagraph();
        lineaCliente.Format.Font.Bold = true;
        lineaCliente.Format.Font.Size = 9;
        lineaCliente.Format.TabStops.AddTabStop(Unit.FromCentimeter(anchoTotal), TabAlignment.Right);
        lineaCliente.AddText(clienteNombre);
        lineaCliente.AddTab();
        lineaCliente.AddText($"Nit: {clienteNit}");

        if (!string.IsNullOrWhiteSpace(lineaExtra))
        {
            var lineaExtraParrafo = encabezado.AddParagraph(lineaExtra);
            lineaExtraParrafo.Format.Font.Bold = true;
            lineaExtraParrafo.Format.Font.Size = 9;
        }

        var lineaMes = encabezado.AddParagraph($"Correspondiente al mes de: {mesTexto}");
        lineaMes.Format.Font.Bold = true;
        lineaMes.Format.Font.Size = 9;
        lineaMes.Format.SpaceAfter = "0.1cm";

        var tablaTitulos = encabezado.AddTable();
        tablaTitulos.Borders.Width = 0.4;
        foreach (var columna in columnas) tablaTitulos.AddColumn(Unit.FromCentimeter(columna.AnchoCm));

        var filaTitulos = tablaTitulos.AddRow();
        filaTitulos.Shading.Color = ColorEncabezado;
        filaTitulos.Format.Font.Bold = true;
        filaTitulos.Format.Font.Size = 7.5;
        for (int i = 0; i < columnas.Count; i++)
        {
            var celda = filaTitulos.Cells[i];
            celda.AddParagraph(columnas[i].Titulo);
            celda.Format.Alignment = ParagraphAlignment.Center;
            celda.VerticalAlignment = VerticalAlignment.Center;
        }
    }

    /// <summary>
    /// La tabla de DATOS: usa los mismos anchos de columna que la fila de
    /// títulos del encabezado de página, para que ambas queden alineadas
    /// visualmente aunque sean tablas distintas (una vive en el header de
    /// MigraDoc, otra en el cuerpo).
    /// </summary>
    public static Table CrearTablaDatos(Section seccion, List<(string Clave, string Titulo, double AnchoCm)> columnas)
    {
        var tabla = seccion.AddTable();
        tabla.Borders.Width = 0.3;
        tabla.Format.Font.Size = 7.5;
        foreach (var columna in columnas) tabla.AddColumn(Unit.FromCentimeter(columna.AnchoCm));
        return tabla;
    }

    /// <summary>
    /// Trunca (con "…") el nombre de proveedor/cliente si no cabe en una
    /// sola línea dentro de su columna. Es literalmente lo que evita el bug
    /// de paginación manual: <see cref="CapacidadFilasPorPagina"/> asume
    /// filas de una sola línea; si un nombre largo envuelve a 2 líneas, la
    /// fila real mide más de lo presupuestado y, con suficientes filas así en
    /// una misma página, el bloque entero se desborda a una hoja física
    /// extra DENTRO de la misma sección — ahí es donde aparecía el folio
    /// duplicado y el "Van"/"Viene" descolocado que reportó el usuario.
    /// Forzar una sola línea (en vez de calcular con más margen de
    /// seguridad) es lo único que garantiza el presupuesto de filas por
    /// página sin importar qué tan largos sean los nombres reales.
    /// </summary>
    public static string TextoSinEnvolver(string texto, double anchoColumnaCm)
    {
        // Ancho promedio de un carácter en Arial 7.5pt ~ 0.5em (mezcla de
        // mayúsculas/minúsculas típica de nombres comerciales); 1em = 7.5pt
        // = 0.2646cm. Se reserva ~0.3cm de la columna para el padding de la
        // celda (no todo el ancho es área de texto útil).
        const double anchoPromedioCaracterCm = 0.5 * 0.2646;
        var anchoUtilCm = Math.Max(0, anchoColumnaCm - 0.3);
        var maxCaracteres = Math.Max(5, (int)(anchoUtilCm / anchoPromedioCaracterCm));

        if (texto.Length <= maxCaracteres) return texto;

        return texto[..(maxCaracteres - 1)].TrimEnd() + "…";
    }

    /// <summary>
    /// Escribe una fila de resumen ("Van"/"Viene"/el total final): la
    /// etiqueta va en la columna <paramref name="claveEtiqueta"/> y cada
    /// monto de <paramref name="sumas"/> en la columna cuya clave coincide
    /// (las claves que no existan en esta hoja —p. ej. Exportaciones/INGUAT
    /// cuando no aplican— se ignoran en silencio).
    /// </summary>
    public static void EscribirFilaResumen(
        Table tabla, string etiqueta, string claveEtiqueta, Dictionary<string, int> indice, IReadOnlyDictionary<string, decimal> sumas)
    {
        var fila = tabla.AddRow();
        fila.Format.Font.Bold = true;
        fila.Borders.Top.Width = 0.5;
        fila.Cells[indice[claveEtiqueta]].AddParagraph(etiqueta);

        foreach (var (clave, valor) in sumas)
        {
            if (indice.TryGetValue(clave, out var columna)) EscribirMonto(fila.Cells[columna], valor);
        }
    }

    public static void EscribirMonto(Cell celda, decimal valor)
    {
        var texto = valor < 0
            ? $"({Math.Abs(valor).ToString("#,##0.00", CultureInfo.InvariantCulture)})"
            : valor.ToString("#,##0.00", CultureInfo.InvariantCulture);
        celda.AddParagraph(texto);
        celda.Format.Alignment = ParagraphAlignment.Right;
    }

    public static void GuardarPdf(Document documento, string rutaSalida)
    {
        var carpeta = Path.GetDirectoryName(rutaSalida);
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

        var render = new PdfDocumentRenderer { Document = documento };
        render.RenderDocument();
        render.PdfDocument.Save(rutaSalida);
    }
}
