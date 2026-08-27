namespace Argenta.Core.Validacion;

/// <summary>
/// Ejecuta todas las reglas de validación registradas para un módulo y agrupa
/// los hallazgos. Genérico y reutilizable por cualquier módulo contable.
/// </summary>
public sealed class MotorValidaciones<TContexto>(IEnumerable<IReglaValidacion<TContexto>> reglas)
{
    public IReadOnlyList<HallazgoValidacion> Evaluar(TContexto contexto) =>
        reglas.SelectMany(regla => regla.Validar(contexto)).ToList();
}
