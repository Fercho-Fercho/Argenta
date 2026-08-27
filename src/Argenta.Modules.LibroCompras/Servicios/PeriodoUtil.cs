using Argenta.Modules.LibroCompras.Modelos;

namespace Argenta.Modules.LibroCompras.Servicios;

/// <summary>
/// Determina el mes/año "predominante" de un lote de facturas ya clasificadas
/// (el que tiene más filas). Se usa tanto para el encabezado del libro
/// ("Correspondiente al Mes de :") como para sugerir el nombre del archivo al
/// generar.
/// </summary>
public static class PeriodoUtil
{
    public static (int Mes, int Anio) ObtenerMesAnioPredominante(IReadOnlyList<FilaLibroCompras> filas)
    {
        if (filas.Count == 0)
        {
            var hoy = DateTime.Today;
            return (hoy.Month, hoy.Year);
        }

        var grupo = filas
            .GroupBy(f => new { f.Fecha.Year, f.Fecha.Month })
            .OrderByDescending(g => g.Count())
            .First();

        return (grupo.Key.Month, grupo.Key.Year);
    }
}
