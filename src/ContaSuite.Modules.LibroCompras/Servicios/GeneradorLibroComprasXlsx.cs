using ClosedXML.Excel;
using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using ContaSuite.Modules.LibroCompras.Modelos;

namespace ContaSuite.Modules.LibroCompras.Servicios;

/// <summary>
/// Genera el archivo .xlsx del libro de compras con el formato del modelo
/// (columnas A a L únicamente; el bloque de resúmenes de IVA queda para fase 2).
/// </summary>
public sealed class GeneradorLibroComprasXlsx
{
    private const string FormatoMoneda = "#,##0.00;(#,##0.00)";
    private const string FormatoFecha = "dd/mm/yyyy";

    // Naranja: proveedor no encontrado en el catálogo. Amarillo: descuadre de
    // IVA. Deben coincidir con los brushes de Styles/Colores.xaml en el
    // proyecto Wpf (FilaRevisionBrush / FilaDescuadreIvaBrush) para que la
    // vista previa y el Excel de salida se vean iguales.
    private static readonly XLColor ColorRevision = XLColor.FromHtml("#FFC966");
    private static readonly XLColor ColorDescuadreIva = XLColor.FromHtml("#FFF59D");

    public void Generar(
        string rutaSalida,
        Cliente cliente,
        IReadOnlyList<FilaLibroCompras> filas,
        IReadOnlyList<FilaLibroCompras> filasAparte)
    {
        // Función 2: se omiten por completo las facturas marcadas como
        // "Excluir" — no aparecen en el Excel ni suman en los totales — y el
        // correlativo se recalcula solo sobre las que quedan incluidas.
        var filasIncluidas = filas.Where(f => f.Incluida).ToList();
        var filasApartadasIncluidas = filasAparte.Where(f => f.Incluida).ToList();
        LibroComprasService.NumerarFilas(filasIncluidas);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Libro de Compras");

        EscribirEncabezado(hoja, cliente, filasIncluidas);
        int filaSiguiente = EscribirFilasDatos(hoja, filasIncluidas, filaInicial: 7);
        filaSiguiente = EscribirTotales(hoja, filasIncluidas, filaSiguiente);

        if (filasApartadasIncluidas.Count > 0)
        {
            filaSiguiente = EscribirSeccionAparte(hoja, filasApartadasIncluidas, filaSiguiente);
        }

        EscribirResumenIva(hoja, filasIncluidas, filasApartadasIncluidas, filaSiguiente);

        hoja.Columns(1, 12).AdjustToContents();
        hoja.Column(7).Width = Math.Max(hoja.Column(7).Width, 45); // Proveedor legible

        // Columna A: ya no carga texto de título (van combinados sobre B:L o
        // similar, ver EscribirEncabezado); solo lleva el correlativo "No."
        // (pocos dígitos), así que se fuerza angosta en vez de dejar que
        // AdjustToContents la deje desproporcionada.
        hoja.Column(1).Width = 8;

        var carpeta = Path.GetDirectoryName(rutaSalida);
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

        libro.SaveAs(rutaSalida);
    }

    private static void EscribirEncabezado(IXLWorksheet hoja, Cliente cliente, IReadOnlyList<FilaLibroCompras> filas)
    {
        hoja.Cell("A1").Value = "LIBRO DE COMPRAS DE BIENES Y SERVICIOS ADQUIRIDOS";
        hoja.Range("A1:L1").Merge();
        hoja.Cell("A1").Style.Font.Bold = true;
        hoja.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Combinadas sobre varias columnas (no solo A) para que el texto no se
        // corte y la columna A quede libre para ser angosta: es la misma
        // columna del correlativo "No." de la tabla (pocos dígitos).
        hoja.Range("A2:H2").Merge();
        hoja.Cell("A2").Value = cliente.Nombre;
        hoja.Cell("A2").Style.Font.Bold = true;

        hoja.Range("I2:L2").Merge();
        hoja.Cell("I2").Value = $"Nit: {cliente.Nit}";
        hoja.Cell("I2").Style.Font.Bold = true;

        hoja.Range("A4:F4").Merge();
        hoja.Cell("A4").Value = "Correspondiente al Mes de :";
        hoja.Cell("A4").Style.Font.Bold = true;

        hoja.Range("G4:L4").Merge();
        hoja.Cell("G4").Value = ObtenerMesPredominante(filas);
        hoja.Cell("G4").Style.Font.Bold = true;

        // Fila 5-6: encabezado de columnas.
        void Titulo(string celda, string texto)
        {
            hoja.Cell(celda).Value = texto;
            hoja.Cell(celda).Style.Font.Bold = false;
            hoja.Cell(celda).Style.Alignment.WrapText = true;
            hoja.Cell(celda).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        Titulo("A5", "No. ");
        Titulo("B5", "Fecha");
        Titulo("C5", "Docto");
        Titulo("D5", "Serie");
        Titulo("E5", "No. Doc");
        Titulo("F5", "Nit");
        Titulo("G5", "Proveedor");
        Titulo("H5", "Valores Netos");
        Titulo("J5", "Exento");
        Titulo("K5", "Iva");
        Titulo("L5", "Total");
        Titulo("H6", "Compras");
        Titulo("I6", "Servicios");

        foreach (var columna in new[] { "A", "B", "C", "D", "E", "F", "G", "J", "K", "L" })
        {
            hoja.Range($"{columna}5:{columna}6").Merge();
        }

        hoja.Range("H5:I5").Merge();

        var rangoEncabezado = hoja.Range("A5:L6");
        rangoEncabezado.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rangoEncabezado.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        rangoEncabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
    }

    private static string ObtenerMesPredominante(IReadOnlyList<FilaLibroCompras> filas)
    {
        var (mes, anio) = PeriodoUtil.ObtenerMesAnioPredominante(filas);
        return $"{Capitalizar(ParseoUtil.NombreMesEspanol(mes))} de {anio}";
    }

    private static string Capitalizar(string texto) =>
        texto.Length == 0 ? texto : char.ToUpperInvariant(texto[0]) + texto[1..];

    private static int EscribirFilasDatos(IXLWorksheet hoja, IReadOnlyList<FilaLibroCompras> filas, int filaInicial)
    {
        int fila = filaInicial;
        foreach (var item in filas)
        {
            EscribirFila(hoja, fila, item);
            fila++;
        }

        return fila;
    }

    private static void EscribirFila(IXLWorksheet hoja, int fila, FilaLibroCompras item)
    {
        if (item.Numero.HasValue) hoja.Cell(fila, 1).Value = item.Numero.Value;

        var celdaFecha = hoja.Cell(fila, 2);
        celdaFecha.Value = item.Fecha;
        celdaFecha.Style.NumberFormat.Format = FormatoFecha;

        hoja.Cell(fila, 3).Value = item.Docto;
        hoja.Cell(fila, 4).Value = item.Serie;
        hoja.Cell(fila, 5).Value = item.NoDoc;
        hoja.Cell(fila, 6).Value = item.Nit;
        hoja.Cell(fila, 7).Value = item.Proveedor;

        EscribirMonto(hoja, fila, 8, item.Compras);
        EscribirMonto(hoja, fila, 9, item.Servicios);
        EscribirMonto(hoja, fila, 10, item.Exento);
        EscribirMonto(hoja, fila, 11, item.Iva);
        EscribirMonto(hoja, fila, 12, item.Total);

        // Resaltado de revisión: naranja (proveedor no encontrado en el
        // catálogo normal, o marcado en el catálogo "Proveedores a
        // revisar"), amarillo (descuadre de IVA), o las dos cosas a la vez.
        // Una celda de Excel no admite dos colores, así que cuando aplican
        // ambos casos se parte la fila en dos mitades: columnas A–F naranja
        // y G–L amarillo.
        var marcadoNaranja = item.ProveedorNoEncontrado || item.MarcadoParaRevisar;

        if (marcadoNaranja && item.DescuadreIva)
        {
            hoja.Range(fila, 1, fila, 6).Style.Fill.BackgroundColor = ColorRevision;
            hoja.Range(fila, 7, fila, 12).Style.Fill.BackgroundColor = ColorDescuadreIva;
        }
        else if (marcadoNaranja)
        {
            hoja.Range(fila, 1, fila, 12).Style.Fill.BackgroundColor = ColorRevision;
        }
        else if (item.DescuadreIva)
        {
            hoja.Range(fila, 1, fila, 12).Style.Fill.BackgroundColor = ColorDescuadreIva;
        }
    }

    private static void EscribirMonto(IXLWorksheet hoja, int fila, int columna, decimal valor)
    {
        var celda = hoja.Cell(fila, columna);
        celda.Value = valor;
        celda.Style.NumberFormat.Format = FormatoMoneda;
    }

    private static int EscribirTotales(IXLWorksheet hoja, IReadOnlyList<FilaLibroCompras> filas, int filaSiguiente)
    {
        int filaTotales = filaSiguiente + 1;

        var totalCompras = RedondeoUtil.Redondear(filas.Sum(f => f.Compras));
        var totalServicios = RedondeoUtil.Redondear(filas.Sum(f => f.Servicios));
        var totalExento = RedondeoUtil.Redondear(filas.Sum(f => f.Exento));
        var totalIva = RedondeoUtil.Redondear(filas.Sum(f => f.Iva));
        var totalGeneral = RedondeoUtil.Redondear(filas.Sum(f => f.Total));

        hoja.Cell(filaTotales, 7).Value = "TOTALES";
        hoja.Cell(filaTotales, 7).Style.Font.Bold = true;
        EscribirMonto(hoja, filaTotales, 8, totalCompras);
        EscribirMonto(hoja, filaTotales, 9, totalServicios);
        EscribirMonto(hoja, filaTotales, 10, totalExento);
        EscribirMonto(hoja, filaTotales, 11, totalIva);
        EscribirMonto(hoja, filaTotales, 12, totalGeneral);
        hoja.Range(filaTotales, 8, filaTotales, 12).Style.Font.Bold = true;
        hoja.Range(filaTotales, 7, filaTotales, 12).Style.Border.TopBorder = XLBorderStyleValues.Thin;

        int filaNoCredito = filaTotales + 2;
        int filaSiCredito = filaNoCredito + 2;

        EscribirLineaCreditoFiscal(hoja, filaNoCredito, "Compra y servicios de los que no procede a crédito fiscal", totalExento);
        EscribirLineaCreditoFiscal(hoja, filaSiCredito, "Compra y servicios de los que procede a crédito fiscal", RedondeoUtil.Redondear(totalCompras + totalServicios));

        // Recuadro negro que enmarca las dos líneas (texto D:G fusionado + monto en H),
        // igual que en el libro modelo: un solo rectángulo de borde que abarca ambas filas.
        var recuadro = hoja.Range(filaNoCredito, 4, filaSiCredito, 8);
        recuadro.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        recuadro.Style.Border.OutsideBorderColor = XLColor.Black;

        return filaSiCredito + 2;
    }

    /// <summary>
    /// Escribe una línea del resumen de crédito fiscal: el texto (largo) va
    /// fusionado en D:G para que no ensanche ninguna columna del libro, y el
    /// monto en H — igual posición que usa el modelo original.
    /// </summary>
    private static void EscribirLineaCreditoFiscal(IXLWorksheet hoja, int fila, string texto, decimal monto)
    {
        hoja.Range(fila, 4, fila, 7).Merge();
        var celdaTexto = hoja.Cell(fila, 4);
        celdaTexto.Value = texto;
        celdaTexto.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        EscribirMonto(hoja, fila, 8, monto);
    }

    /// <summary>
    /// Sección aparte para documentos tipo CIVA y NABN (misma regla para los
    /// dos): se listan al final del libro, claramente rotulados, sin afectar
    /// la lista principal ni sus totales.
    /// </summary>
    private static int EscribirSeccionAparte(IXLWorksheet hoja, IReadOnlyList<FilaLibroCompras> filasAparte, int filaInicial)
    {
        int filaTitulo = filaInicial + 1;
        hoja.Cell(filaTitulo, 1).Value = "FACTURAS TIPO CIVA Y NABN — no forman parte del libro de compras ni de sus totales";
        hoja.Cell(filaTitulo, 1).Style.Font.Bold = true;
        hoja.Range(filaTitulo, 1, filaTitulo, 12).Merge();

        int filaEncabezado = filaTitulo + 1;
        string[] titulos = ["No.", "Fecha", "Docto", "Serie", "No. Doc", "Nit", "Proveedor", "Compras", "Servicios", "Exento", "Iva", "Total"];
        for (int c = 0; c < titulos.Length; c++)
        {
            var celda = hoja.Cell(filaEncabezado, c + 1);
            celda.Value = titulos[c];
            celda.Style.Font.Bold = true;
        }

        int fila = filaEncabezado + 1;
        int numero = 1;
        foreach (var item in filasAparte)
        {
            item.Numero = numero++;
            EscribirFila(hoja, fila, item);
            fila++;
        }

        return fila;
    }

    /// <summary>
    /// Bloque resumen "IVA Débitos / IVA Créditos" (Fase 2), reproducido con el
    /// mismo diseño del libro modelo. Solo se completan las celdas cuya fórmula
    /// original hace referencia a datos DENTRO de este mismo libro de compras
    /// (montos, IVA de combustibles/servicios/CIVA, conteos de documentos).
    /// Las celdas cuya fórmula original apunta a OTRO archivo de Excel o a otra
    /// hoja que no existe aquí (Libro de Ventas, el libro del mes anterior,
    /// constancias de retención consultadas aparte) se dejan en blanco a
    /// propósito: no tenemos esos datos y no hay forma de calcularlos sin
    /// inventarlos.
    /// </summary>
    private static readonly XLColor ColorFondoBloqueResumen = XLColor.FromHtml("#F2F2F2");
    private static readonly XLColor ColorTituloSeccion = XLColor.FromHtml("#D9D9D9");

    private static void EscribirResumenIva(
        IXLWorksheet hoja,
        IReadOnlyList<FilaLibroCompras> filasIncluidas,
        IReadOnlyList<FilaLibroCompras> filasApartadasIncluidas,
        int filaInicial)
    {
        int filaBloqueInicial = filaInicial + 2;
        int f = filaBloqueInicial;

        // Fondo gris claro base de toda la fila (G:K), para diferenciar el
        // bloque del resto de la hoja. Se aplica primero; los colores más
        // específicos (título, "Redondeo") se escriben encima después.
        void FondoFila(int fila) => hoja.Range(fila, 7, fila, 11).Style.Fill.BackgroundColor = ColorFondoBloqueResumen;

        // Título de sección ("IVA DÉBITOS" / "IVA CRÉDITOS") Y los encabezados
        // de columna (Valor Neto/Montos, Redondeo, IVA) van en LA MISMA fila,
        // cada uno en su propia celda SIN fusionar — igual que el modelo. (Si
        // se fusiona G:K no se puede escribir por separado en H/I/K después.)
        void TituloYEncabezados(int fila, string titulo, string h, string i, string k)
        {
            var rango = hoja.Range(fila, 7, fila, 11);
            rango.Style.Fill.BackgroundColor = ColorTituloSeccion;
            rango.Style.Font.Bold = true;

            hoja.Cell(fila, 7).Value = titulo;
            hoja.Cell(fila, 8).Value = h;
            hoja.Cell(fila, 9).Value = i;
            hoja.Cell(fila, 11).Value = k;
        }

        // Escribe una línea "Neto / Redondeo / IVA" con las tres celdas VACÍAS
        // (dato que no tenemos, o que no sabemos calcular de forma confiable
        // todavía: Libro de Ventas, meses anteriores, constancias de retención).
        void LineaSinDato(int fila, string etiqueta)
        {
            FondoFila(fila);
            hoja.Cell(fila, 7).Value = etiqueta;
        }

        // Escribe una línea "Neto / Redondeo / IVA" con un monto que SÍ
        // calculamos a partir de nuestras propias facturas de este libro.
        decimal LineaConDato(int fila, string etiqueta, decimal monto)
        {
            FondoFila(fila);
            hoja.Cell(fila, 7).Value = etiqueta;
            var netoCelda = hoja.Cell(fila, 8);
            netoCelda.Value = monto;
            netoCelda.Style.NumberFormat.Format = FormatoMoneda;

            // "Redondeo" e "IVA" van a número entero en este bloque, igual que
            // en el modelo (ROUND(valor,0)) — a diferencia del resto del
            // libro, que usa 2 decimales.
            var redondeo = Math.Round(monto, 0, MidpointRounding.AwayFromZero);
            var redondeoCelda = hoja.Cell(fila, 9);
            redondeoCelda.Value = redondeo;
            redondeoCelda.Style.NumberFormat.Format = "#,##0";
            redondeoCelda.Style.Fill.BackgroundColor = XLColor.Yellow; // pisa el gris base a propósito
            redondeoCelda.Style.Font.Bold = true;

            var iva = Math.Round(redondeo * 0.12m, 0, MidpointRounding.AwayFromZero);
            var ivaCelda = hoja.Cell(fila, 11);
            ivaCelda.Value = iva;
            ivaCelda.Style.NumberFormat.Format = "#,##0";
            return iva;
        }

        // ----- IVA DÉBITOS (datos del Libro de Ventas: no los tenemos) -----
        TituloYEncabezados(f++, "IVA DÉBITOS", "Valor Neto", "Redondeo", "IVA");
        LineaSinDato(f++, "Servicios Locales");
        LineaSinDato(f++, "Servicios Exentos");
        FondoFila(f++);

        // ----- IVA CRÉDITOS (lo que sí calculamos de este libro) -----
        TituloYEncabezados(f++, "IVA CRÉDITOS", "Montos", "Redondeo", "IVA");
        LineaSinDato(f++, "Compra de Inmueble");
        LineaSinDato(f++, "Compra de Vehículo");
        LineaSinDato(f++, "Compras a pequeños contribuyentes");

        var totalCompras = RedondeoUtil.Redondear(filasIncluidas.Sum(x => x.Compras));
        var totalServicios = RedondeoUtil.Redondear(filasIncluidas.Sum(x => x.Servicios));
        var comprasCombustible = RedondeoUtil.Redondear(filasIncluidas.Where(x => x.EsGasolina).Sum(x => x.Compras));
        var otrasCompras = RedondeoUtil.Redondear(totalCompras - comprasCombustible);

        var ivaAcumulado = 0m;
        ivaAcumulado += LineaConDato(f++, "Compra de combustibles", comprasCombustible);
        ivaAcumulado += LineaConDato(f++, "Otras Compras", otrasCompras);
        ivaAcumulado += LineaConDato(f++, "Servicios", totalServicios);

        // "Exenciones de IVA (IVA DOCTO TIPO CIVA)": suma del Iva de las
        // facturas CIVA que sí están en la sección aparte de este mismo libro.
        var ivaCiva = RedondeoUtil.Redondear(filasApartadasIncluidas.Where(x => x.Docto.Trim().Equals("CIVA", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Iva));
        FondoFila(f);
        hoja.Cell(f, 7).Value = "Exenciones de IVA (IVA DOCTO TIPO CIVA)";
        var celdaIvaCiva = hoja.Cell(f, 11);
        celdaIvaCiva.Value = ivaCiva;
        celdaIvaCiva.Style.NumberFormat.Format = "#,##0.00";
        ivaAcumulado += ivaCiva;
        f++;

        LineaSinDato(f++, "Crédito anterior"); // libro del mes anterior: no lo tenemos

        FondoFila(f);
        hoja.Cell(f, 7).Value = "Total IVA CRÉDITO";
        hoja.Cell(f, 7).Style.Font.Bold = true;
        var celdaTotalCredito = hoja.Cell(f, 11);
        celdaTotalCredito.Value = RedondeoUtil.Redondear(ivaAcumulado);
        celdaTotalCredito.Style.NumberFormat.Format = "#,##0.00";
        celdaTotalCredito.Style.Font.Bold = true;
        int filaTotalCredito = f;
        f++;

        // "Crédito para el siguiente mes" = Total IVA Crédito − IVA Débitos.
        // Como no tenemos IVA Débitos (Ventas), queda igual al total de crédito.
        FondoFila(f);
        hoja.Cell(f, 7).Value = "Crédito PARA EL SIGUIENTE MES";
        hoja.Cell(f, 7).Style.Font.Bold = true;
        var celdaCreditoSiguiente = hoja.Cell(f, 11);
        celdaCreditoSiguiente.Value = hoja.Cell(filaTotalCredito, 11).GetValue<decimal>();
        celdaCreditoSiguiente.Style.NumberFormat.Format = "#,##0.00";
        celdaCreditoSiguiente.Style.Font.Bold = true;
        f++;
        FondoFila(f++); // línea en blanco

        LineaSinDato(f++, "Crédito Anterior de Ret. IVA"); // libro del mes anterior
        LineaSinDato(f++, "Retención de IVA"); // constancias de retención (sistema aparte de la SAT)
        LineaSinDato(f++, "Nuevo crédito de ret IVA"); // depende de las dos anteriores
        LineaSinDato(f++, "FAC EMITIDAS"); // Libro de Ventas: no lo tenemos

        // "FACTURAS REC" = correlativo más alto del libro (cantidad de
        // facturas recibidas numeradas, sin contar RECI/FPEQ).
        var facturasRecibidas = filasIncluidas.Where(x => x.Numero.HasValue).Select(x => x.Numero!.Value).DefaultIfEmpty(0).Max();
        FondoFila(f);
        hoja.Cell(f, 7).Value = "FACTURAS REC";
        hoja.Cell(f++, 11).Value = facturasRecibidas;

        // "EXEN IVA", "NC REC" y "VALOR NC": todavía no hay una forma confiable
        // de calcularlos a partir de nuestros datos, así que se dejan en
        // blanco a propósito (mismo criterio que las celdas que dependen de
        // otro archivo/mes) hasta confirmar la fórmula correcta.
        LineaSinDato(f++, "EXEN IVA");
        LineaSinDato(f++, "RET IVA"); // constancias de retención: no lo tenemos
        LineaSinDato(f++, "NC REC");
        LineaSinDato(f++, "VALOR NC");

        int filaBloqueFinal = f - 1;

        // Marco del bloque completo: borde negro alrededor de todo el cuadro
        // (el fondo gris claro ya se aplicó línea por línea arriba).
        var bloqueCompleto = hoja.Range(filaBloqueInicial, 7, filaBloqueFinal, 11);
        bloqueCompleto.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        bloqueCompleto.Style.Border.OutsideBorderColor = XLColor.Black;
        bloqueCompleto.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        bloqueCompleto.Style.Border.InsideBorderColor = XLColor.FromHtml("#BFBFBF");
    }
}
