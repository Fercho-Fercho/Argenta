namespace ContaSuite.Core.Utilidades;

/// <summary>
/// Redondeo estándar usado en toda la suite: 2 decimales, con la regla
/// "si queda .5 redondea hacia arriba" (AwayFromZero), tal como lo exige
/// la normativa contable de Guatemala para cuadrar los libros con el SAT.
/// </summary>
public static class RedondeoUtil
{
    public const int Decimales = 2;

    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, Decimales, MidpointRounding.AwayFromZero);
}
