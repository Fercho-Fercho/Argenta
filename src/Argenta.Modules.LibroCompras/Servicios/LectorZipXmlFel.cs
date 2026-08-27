using System.IO.Compression;
using System.Xml.Linq;
using Argenta.Core.Utilidades;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Lee un .zip con facturas electrónicas (FEL) en XML, esquema
/// http://www.sat.gob.gt/dte/fel/0.2.0, y las convierte a <see cref="DteFel"/>.
/// Procesa cada .xml del ZIP de forma independiente; si alguno no se puede
/// leer, se reporta con su nombre de archivo sin detener la lectura del resto.
/// </summary>
public sealed class LectorZipXmlFel
{
    private static readonly XNamespace Dte = "http://www.sat.gob.gt/dte/fel/0.2.0";

    public IReadOnlyList<DteFel> Leer(Stream contenidoZip)
    {
        using var zip = new ZipArchive(contenidoZip, ZipArchiveMode.Read);
        var facturas = new List<DteFel>();
        var errores = new List<string>();

        foreach (var entrada in zip.Entries.Where(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                using var flujo = entrada.Open();
                var documento = XDocument.Load(flujo);
                facturas.Add(MapearDocumento(documento, entrada.Name));
            }
            catch (Exception ex)
            {
                errores.Add($"{entrada.Name}: {ex.Message}");
            }
        }

        if (facturas.Count == 0 && errores.Count == 0)
        {
            throw new InvalidDataException("El archivo .zip no contiene ningún archivo .xml.");
        }

        if (errores.Count > 0)
        {
            throw new InvalidDataException(
                $"No se pudieron leer {errores.Count} de {facturas.Count + errores.Count} archivos XML del ZIP:\n" +
                string.Join("\n", errores));
        }

        return facturas;
    }

    private static DteFel MapearDocumento(XDocument documento, string nombreArchivo)
    {
        var datosEmision = documento.Descendants(Dte + "DatosEmision").FirstOrDefault()
            ?? throw new InvalidDataException("No se encontró el nodo DatosEmision.");

        var datosGenerales = datosEmision.Element(Dte + "DatosGenerales")
            ?? throw new InvalidDataException("No se encontró el nodo DatosGenerales.");

        var emisor = datosEmision.Element(Dte + "Emisor")
            ?? throw new InvalidDataException("No se encontró el nodo Emisor.");

        var receptor = datosEmision.Element(Dte + "Receptor")
            ?? throw new InvalidDataException("No se encontró el nodo Receptor.");

        var totales = datosEmision.Element(Dte + "Totales")
            ?? throw new InvalidDataException("No se encontró el nodo Totales.");

        var certificacion = documento.Descendants(Dte + "Certificacion").FirstOrDefault()
            ?? throw new InvalidDataException("No se encontró el nodo Certificacion.");

        var numeroAutorizacion = certificacion.Element(Dte + "NumeroAutorizacion")
            ?? throw new InvalidDataException("No se encontró el nodo NumeroAutorizacion.");

        var direccionEmisor = emisor.Element(Dte + "DireccionEmisor")?.Element(Dte + "Direccion")?.Value ?? string.Empty;

        var items = (datosEmision.Element(Dte + "Items")?.Elements(Dte + "Item") ?? Enumerable.Empty<XElement>())
            .Select(MapearItem)
            .ToList();

        var totalImpuestos = (totales.Element(Dte + "TotalImpuestos")?.Elements(Dte + "TotalImpuesto") ?? Enumerable.Empty<XElement>())
            .GroupBy(x => ((string?)x.Attribute("NombreCorto") ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => ParseoUtil.ParsearDecimal((string?)x.Attribute("TotalMontoImpuesto"))),
                StringComparer.OrdinalIgnoreCase);

        return new DteFel
        {
            TipoDte = ((string?)datosGenerales.Attribute("Tipo") ?? string.Empty).Trim(),
            CodigoMoneda = ((string?)datosGenerales.Attribute("CodigoMoneda") ?? "GTQ").Trim(),
            FechaEmision = ParseoUtil.ParsearFechaSat((string?)datosGenerales.Attribute("FechaHoraEmision") ?? string.Empty),
            FechaCertificacion = ParseoUtil.ParsearFechaSat(certificacion.Element(Dte + "FechaHoraCertificacion")?.Value ?? string.Empty),
            NitEmisor = ((string?)emisor.Attribute("NITEmisor") ?? string.Empty).Trim(),
            NombreEmisor = ((string?)emisor.Attribute("NombreEmisor") ?? string.Empty).Trim(),
            NombreComercial = ((string?)emisor.Attribute("NombreComercial") ?? string.Empty).Trim(),
            DireccionEmisor = direccionEmisor.Trim(),
            CodigoEstablecimiento = ((string?)emisor.Attribute("CodigoEstablecimiento") ?? string.Empty).Trim(),
            IdReceptor = ((string?)receptor.Attribute("IDReceptor") ?? string.Empty).Trim(),
            NombreReceptor = ((string?)receptor.Attribute("NombreReceptor") ?? string.Empty).Trim(),
            Serie = ((string?)numeroAutorizacion.Attribute("Serie") ?? string.Empty).Trim(),
            NumeroDte = ((string?)numeroAutorizacion.Attribute("Numero") ?? string.Empty).Trim(),
            NumeroAutorizacion = numeroAutorizacion.Value.Trim(),
            GranTotal = ParseoUtil.ParsearDecimal(totales.Element(Dte + "GranTotal")?.Value),
            EsExportacion = string.Equals(((string?)datosGenerales.Attribute("Exp"))?.Trim(), "SI", StringComparison.OrdinalIgnoreCase),
            Items = items,
            TotalImpuestos = totalImpuestos,
            NombreArchivo = nombreArchivo,
        };
    }

    private static ItemFel MapearItem(XElement item)
    {
        var impuestos = (item.Element(Dte + "Impuestos")?.Elements(Dte + "Impuesto") ?? Enumerable.Empty<XElement>())
            .Select(imp => new ImpuestoItemFel
            {
                NombreCorto = (imp.Element(Dte + "NombreCorto")?.Value ?? string.Empty).Trim(),
                MontoGravable = ParseoUtil.ParsearDecimal(imp.Element(Dte + "MontoGravable")?.Value),
                MontoImpuesto = ParseoUtil.ParsearDecimal(imp.Element(Dte + "MontoImpuesto")?.Value),
            })
            .ToList();

        return new ItemFel
        {
            NumeroLinea = (int?)item.Attribute("NumeroLinea") ?? 0,
            BienOServicio = ((string?)item.Attribute("BienOServicio") ?? string.Empty).Trim().ToUpperInvariant(),
            Cantidad = ParseoUtil.ParsearDecimal(item.Element(Dte + "Cantidad")?.Value),
            Descripcion = (item.Element(Dte + "Descripcion")?.Value ?? string.Empty).Trim(),
            PrecioUnitario = ParseoUtil.ParsearDecimal(item.Element(Dte + "PrecioUnitario")?.Value),
            Total = ParseoUtil.ParsearDecimal(item.Element(Dte + "Total")?.Value),
            Impuestos = impuestos,
        };
    }
}
