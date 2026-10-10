using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Repara las tildes y eñes que vienen como "?" en los XML (FEL) que descarga
/// el portal de la SAT. No es un error de lectura de Argenta: el byte que trae
/// el archivo es literalmente un "?" (p. ej. <c>SOCIEDAD AN?NIMA</c>), así que
/// la letra original ya no está en el XML y hay que reconstruirla. En orden:
/// <list type="number">
/// <item>Nombre exacto del Excel "Consulta de documentos" de la SAT (que sí
/// trae las tildes), cruzado por Número de Autorización, cuando se cargó.</item>
/// <item>Diccionario de palabras: las que traen tildes bien escritas en otros
/// XML del mismo ZIP o en el Excel de la SAT, más una lista fija de palabras
/// comunes (ANÓNIMA, COMPAÑÍA, apellidos, etc.).</item>
/// <item>Reglas de contexto para lo que no esté en el diccionario
/// (…CI?N → …CIÓN, vocal?vocal → Ñ, consonante?vocal → Í, final → É).</item>
/// </list>
/// Si nada aplica, el "?" se deja tal cual.
/// </summary>
public static class ReparadorTildesFel
{
    private static readonly Regex Palabra = new(@"[\p{L}?]+", RegexOptions.Compiled);

    private static readonly string[] PalabrasComunes =
    [
        // Razones sociales y giros comunes
        "ANÓNIMA", "ANÓNIMAS", "COMPAÑÍA", "COMPAÑÍAS", "CÍA", "ASOCIACIÓN", "CORPORACIÓN", "FUNDACIÓN",
        "ESPAÑOLA", "ESPAÑOL", "ESPAÑA", "NIÑO", "NIÑOS", "NIÑA", "NIÑAS", "AÑO", "AÑOS", "CAFÉ", "SEÑOR", "SEÑORA",
        "DISEÑO", "DISEÑOS", "PEQUEÑO", "PEQUEÑA", "MONTAÑA", "MONTAÑAS", "CABAÑA", "CABAÑAS", "BAÑO", "BAÑOS",
        "CAMPAÑA", "PIÑA", "CAÑA", "SUEÑO", "SUEÑOS", "DUEÑO", "LEÑA",
        "PANADERÍA", "FERRETERÍA", "LIBRERÍA", "PAPELERÍA", "CARNICERÍA", "JOYERÍA", "CAFETERÍA", "PIZZERÍA",
        "LAVANDERÍA", "PELUQUERÍA", "HOTELERÍA", "INGENIERÍA", "TECNOLOGÍA", "TECNOLOGÍAS", "ENERGÍA", "TORTILLERÍA",
        "PASTELERÍA", "ZAPATERÍA", "LLANTERÍA", "VIDRIERÍA", "TAPICERÍA", "MUEBLERÍA", "BATERÍA",
        "BATERÍAS", "MERCANCÍA", "MERCANCÍAS", "GASOLINERÍA", "FARMACÉUTICA", "FARMACÉUTICOS", "FARMACÉUTICO",
        "ELÉCTRICA", "ELÉCTRICAS", "ELÉCTRICO", "ELÉCTRICOS", "ELECTRÓNICA", "ELECTRÓNICOS", "TECNOLÓGICA",
        "TECNOLÓGICO", "ECONÓMICA", "ECONÓMICO", "MÉDICA", "MÉDICO", "MÉDICOS", "CLÍNICA", "CLÍNICAS", "QUÍMICA",
        "QUÍMICOS", "PÚBLICO", "PÚBLICA", "CRÉDITO", "CRÉDITOS", "LÍNEA", "ÚNICA", "ÚNICO", "AUTOMÓVIL",
        "AUTOMÓVILES", "PETRÓLEO", "ÓPTICA", "MECÁNICA", "MECÁNICO", "GRÁFICA", "GRÁFICOS", "LOGÍSTICA",
        "TURÍSTICO", "TURÍSTICA", "DEPÓSITO", "TÉCNICA", "TÉCNICO", "TÉCNICOS", "ESTÉTICA", "MÚSICA", "ÁREA",
        "CÓDIGO", "NÚMERO", "BÁSICO", "BÁSICA", "ARTÍCULOS", "PLÁSTICO", "PLÁSTICOS", "METÁLICA", "METÁLICAS",
        "HIDRÁULICA", "HIDRÁULICOS", "ACÚSTICA", "CERÁMICA", "CERÁMICAS", "AGRÍCOLA", "AGRÍCOLAS", "PRÁCTICA",
        "INFORMÁTICA", "ATLÁNTICO", "PACÍFICO", "AMÉRICA", "MÉXICO", "CENTROAMÉRICA",
        "COMUNICACIÓN", "DISTRIBUCIÓN", "CONSTRUCCIÓN", "ADMINISTRACIÓN", "INFORMACIÓN", "EDUCACIÓN",
        "ALIMENTACIÓN", "INVERSIÓN", "PRODUCCIÓN", "ATENCIÓN", "COMERCIALIZACIÓN", "IMPORTACIÓN", "EXPORTACIÓN",
        "ACCIÓN", "UNIÓN", "NACIÓN", "REGIÓN", "MISIÓN", "VISIÓN", "TELEVISIÓN", "PENSIÓN", "GESTIÓN", "INNOVACIÓN",
        "SOLUCIÓN", "CAMIÓN", "AVIÓN", "JABÓN", "CARBÓN", "SALÓN", "BOTÓN", "BALÓN", "LIMÓN", "ALGODÓN",
        "MAÍZ", "PAÍS", "RAÍZ", "TRÁNSITO", "VEHÍCULO", "VEHÍCULOS", "ENVÍO", "ENVÍOS", "GUÍA", "DÍA", "DÍAS",
        "VÍA", "TÍO", "RÍO", "RÍOS", "MARÍTIMO", "AÉREO", "AÉREA", "PÓLIZA", "ESTACIÓN",
        // Nombres y apellidos frecuentes
        "MARÍA", "JOSÉ", "JESÚS", "RAÚL", "ÁNGEL", "ÁNGELA", "ÁLVAREZ", "PÉREZ", "GÓMEZ", "LÓPEZ", "MARTÍNEZ",
        "RODRÍGUEZ", "GONZÁLEZ", "HERNÁNDEZ", "SÁNCHEZ", "RAMÍREZ", "FERNÁNDEZ", "GARCÍA", "DÍAZ", "JIMÉNEZ",
        "GUTIÉRREZ", "VÁSQUEZ", "VÁZQUEZ", "VELÁSQUEZ", "MÉNDEZ", "CHÁVEZ", "MÁRQUEZ", "DOMÍNGUEZ", "SUÁREZ",
        "BENÍTEZ", "RAMÍREZ", "ORDÓÑEZ", "MUÑOZ", "NÚÑEZ", "IBÁÑEZ", "YÁÑEZ", "PEÑA", "ACUÑA", "CASTAÑEDA",
        "CATALÁN", "CORDÓN", "ROLDÁN", "GUZMÁN", "BELTRÁN", "MILIÁN", "AGUSTÍN", "MARTÍN", "JOAQUÍN", "RUBÉN",
        "ANDRÉS", "TOMÁS", "NICOLÁS", "LUCÍA", "SOFÍA", "ROCÍO", "INÉS", "VERÓNICA", "MÓNICA", "ANGÉLICA",
        "HÉCTOR", "VÍCTOR", "ÓSCAR", "CÉSAR", "EFRAÍN", "ADÁN", "ARÉVALO", "CALDERÓN", "CHACÓN", "COLÓN", "LEÓN",
        "CONCEPCIÓN", "ASUNCIÓN", "ELÍAS", "MATÍAS", "ÉDGAR", "FÉLIX", "GERMÁN", "JULIÁN",
        "SEBASTIÁN", "FABIÁN", "DAMIÁN", "CRISTIÁN", "ADRIÁN", "IVÁN", "ROMÁN", "SIMÓN", "RAMÓN", "ENCARNACIÓN",
        "ESTÉVEZ", "LÓPEZ", "QUIÑÓNEZ", "ZÚÑIGA", "MONTÚFAR", "CÁCERES", "GÁLVEZ",
        "MEJÍA",
        // Lugares de Guatemala
        "PETÉN", "QUICHÉ", "SACATEPÉQUEZ", "SUCHITEPÉQUEZ", "TOTONICAPÁN", "SOLOLÁ", "ATITLÁN", "COBÁN", "TECPÁN",
        "GUALÁN", "SALAMÁ", "SAYAXCHÉ", "POPTÚN", "JOCOTÁN", "CAMOTÁN", "AMATITLÁN", "CONCOHÁ",
        // Más apellidos terminados en -ÁN / -ÓN (la regla de contexto no los adivina)
        "MORÁN", "GIRÓN", "CHIQUÍN", "MONZÓN", "MARROQUÍN", "BOLAÑOS", "ARAGÓN", "GARZÓN", "RINCÓN", "VALDÉS",
        "DUBÓN", "PINZÓN", "CHAJÓN", "CUÉLLAR", "SOLÍS", "ÁVILA", "GODÍNEZ", "CÓRDOVA", "CÓRDOBA", "ÁLVARO",
        "BELTETÓN", "COJULÚN", "MAGAÑA", "ZECEÑA", "COFIÑO", "CAÑAS", "PATZÁN", "SICÁN", "MONTÉS",
        "ÁLVAREZ", "MARÍN", "GALDÁMEZ", "ALARCÓN", "CASTAÑÓN", "CHÁVEZ",
    ];

    /// <summary>
    /// Repara en el lugar los textos de las facturas (nombres, dirección y
    /// descripciones de ítems). <paramref name="consultaSat"/> es opcional: el
    /// Excel "Consulta de documentos" ya leído, si el usuario lo cargó.
    /// </summary>
    public static void Reparar(
        IReadOnlyList<DteFel> facturas,
        IReadOnlyDictionary<string, EstadoDocumentoSat>? consultaSat = null)
    {
        // 1) Nombre exacto desde el Excel de la SAT.
        if (consultaSat is { Count: > 0 })
        {
            foreach (var factura in facturas)
            {
                if (!consultaSat.TryGetValue(factura.NumeroAutorizacion, out var fila)) continue;

                if (Coincide(factura.NombreEmisor, fila.NombreEmisor)) factura.NombreEmisor = fila.NombreEmisor;
                if (Coincide(factura.NombreReceptor, fila.NombreReceptor)) factura.NombreReceptor = fila.NombreReceptor;
            }
        }

        if (!facturas.Any(f => Textos(f).Any(t => t.Contains('?')))) return;

        // 2) Diccionario: primero lo aprendido de los propios archivos, luego la lista fija.
        var aprendidas = new List<string>();
        foreach (var texto in facturas.SelectMany(Textos)
                     .Concat(consultaSat?.Values.SelectMany(e => new[] { e.NombreEmisor, e.NombreReceptor }) ?? []))
        {
            foreach (Match m in Palabra.Matches(texto))
            {
                if (!m.Value.Contains('?') && m.Value.Any(c => c > 127)) aprendidas.Add(m.Value.ToUpperInvariant());
            }
        }

        var diccionario = aprendidas.Concat(PalabrasComunes)
            .Distinct()
            .GroupBy(p => p.Length)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var factura in facturas)
        {
            factura.NombreEmisor = RepararTexto(factura.NombreEmisor, diccionario);
            factura.NombreComercial = RepararTexto(factura.NombreComercial, diccionario);
            factura.NombreReceptor = RepararTexto(factura.NombreReceptor, diccionario);
            factura.DireccionEmisor = RepararTexto(factura.DireccionEmisor, diccionario);
            foreach (var item in factura.Items)
            {
                item.Descripcion = RepararTexto(item.Descripcion, diccionario);
            }
        }
    }

    private static IEnumerable<string> Textos(DteFel f) =>
        new[] { f.NombreEmisor, f.NombreComercial, f.NombreReceptor, f.DireccionEmisor }
            .Concat(f.Items.Select(i => i.Descripcion));

    /// <summary>
    /// true si <paramref name="candidato"/> (del Excel SAT) es el mismo texto
    /// que <paramref name="actual"/> salvo tildes/eñes o "?" — para no
    /// reemplazar un nombre por otro distinto si la SAT lo formatea diferente.
    /// </summary>
    private static bool Coincide(string actual, string candidato)
    {
        if (string.IsNullOrWhiteSpace(candidato) || actual.Length != candidato.Length) return false;
        if (actual == candidato) return false;

        var a = QuitarTildes(actual);
        var c = QuitarTildes(candidato);
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != '?' && a[i] != c[i]) return false;
        }

        return true;
    }

    private static string QuitarTildes(string texto)
    {
        var sb = new StringBuilder(texto.Length);
        foreach (var ch in texto.ToUpperInvariant())
        {
            var basico = ch.ToString().Normalize(NormalizationForm.FormD)[0];
            sb.Append(basico);
        }

        return sb.ToString();
    }

    private static string RepararTexto(string texto, Dictionary<int, List<string>> diccionario)
    {
        if (!texto.Contains('?')) return texto;

        return Palabra.Replace(texto, m =>
        {
            var palabra = m.Value;
            if (!palabra.Contains('?') || !palabra.Any(char.IsLetter)) return palabra;

            var desdeDiccionario = BuscarEnDiccionario(palabra, diccionario);
            return AplicarMayusculas(palabra, desdeDiccionario ?? AplicarReglas(palabra.ToUpperInvariant()));
        });
    }

    private static string? BuscarEnDiccionario(string palabra, Dictionary<int, List<string>> diccionario)
    {
        if (!diccionario.TryGetValue(palabra.Length, out var candidatas)) return null;

        var mayus = palabra.ToUpperInvariant();
        foreach (var candidata in candidatas)
        {
            var encaja = true;
            for (int i = 0; i < mayus.Length && encaja; i++)
            {
                encaja = mayus[i] == '?' ? candidata[i] > 127 : mayus[i] == candidata[i];
            }

            if (encaja) return candidata;
        }

        return null;
    }

    private static string AplicarReglas(string palabra)
    {
        var letras = palabra.ToCharArray();
        for (int i = 0; i < letras.Length; i++)
        {
            if (letras[i] != '?') continue;

            char? previa = i > 0 && palabra[i - 1] != '?' ? palabra[i - 1] : null;
            char? siguiente = i < palabra.Length - 1 && palabra[i + 1] != '?' ? palabra[i + 1] : null;
            var esFinal = i == palabra.Length - 1;

            if (previa == 'I' && siguiente == 'N' && i == palabra.Length - 2)
                letras[i] = 'Ó';                                        // ...CI?N → ...CIÓN
            else if (previa is { } p1 && EsVocal(p1) && siguiente is { } s1 && EsVocal(s1))
                letras[i] = 'Ñ';                                        // ESPA?OLA, NI?O
            else if (previa is { } p2 && !EsVocal(p2) && siguiente is { } s2 && EsVocal(s2))
                letras[i] = 'Í';                                        // MAR?A, D?AZ
            else if (esFinal && previa is { } p3 && !EsVocal(p3) && i >= 2)
                letras[i] = 'É';                                        // JOS?, CAF?
        }

        return new string(letras);
    }

    private static bool EsVocal(char c) => "AEIOU".Contains(c);

    /// <summary>Copia mayúsculas/minúsculas de la palabra original a la reparada (que viene en mayúsculas).</summary>
    private static string AplicarMayusculas(string original, string reparada)
    {
        if (!original.Any(char.IsLower)) return reparada;

        var sb = new StringBuilder(reparada.Length);
        for (int i = 0; i < reparada.Length; i++)
        {
            sb.Append(char.IsLower(LetraReferencia(original, i)) ? char.ToLower(reparada[i], CultureInfo.InvariantCulture) : reparada[i]);
        }

        return sb.ToString();
    }

    /// <summary>La letra original en <paramref name="i"/> o, si es "?", la letra más cercana (primero hacia atrás).</summary>
    private static char LetraReferencia(string original, int i)
    {
        for (int j = i; j >= 0; j--)
        {
            if (original[j] != '?') return original[j];
        }

        for (int j = i + 1; j < original.Length; j++)
        {
            if (original[j] != '?') return original[j];
        }

        return 'A';
    }
}
