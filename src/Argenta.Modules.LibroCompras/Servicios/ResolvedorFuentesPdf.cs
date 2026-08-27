using PdfSharp.Fonts;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// PDFsharp (versión multiplataforma, sin GDI+) no trae ningún resolvedor de
/// fuentes por defecto: hay que decirle explícitamente de dónde leer los
/// archivos .ttf de cada variante (normal/negrita/cursiva). Se usa Arial
/// directamente de la carpeta de fuentes de Windows — la app solo corre en
/// Windows, así que siempre está disponible.
/// </summary>
public sealed class ResolvedorFuentesPdf : IFontResolver
{
    public const string NombreFuente = "Arial";

    public byte[] GetFont(string faceName)
    {
        var ruta = faceName switch
        {
            "Arial#b" => @"C:\Windows\Fonts\arialbd.ttf",
            "Arial#i" => @"C:\Windows\Fonts\ariali.ttf",
            "Arial#bi" => @"C:\Windows\Fonts\arialbi.ttf",
            _ => @"C:\Windows\Fonts\arial.ttf",
        };

        return File.ReadAllBytes(ruta);
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var face = (isBold, isItalic) switch
        {
            (true, true) => "Arial#bi",
            (true, false) => "Arial#b",
            (false, true) => "Arial#i",
            _ => "Arial#",
        };

        return new FontResolverInfo(face);
    }

    /// <summary>Registra el resolvedor una sola vez para todo el proceso (idempotente).</summary>
    public static void Asegurar()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new ResolvedorFuentesPdf();
        }
    }
}
