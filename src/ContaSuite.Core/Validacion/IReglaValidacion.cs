namespace ContaSuite.Core.Validacion;

/// <summary>
/// Una regla de validación sobre un lote de documentos de tipo <typeparamref name="TContexto"/>.
/// Cada módulo (Compras, y en el futuro Ventas) implementa las suyas y las registra
/// en el contenedor de DI; el <see cref="MotorValidaciones{TContexto}"/> las ejecuta todas.
/// </summary>
public interface IReglaValidacion<in TContexto>
{
    IEnumerable<HallazgoValidacion> Validar(TContexto contexto);
}
