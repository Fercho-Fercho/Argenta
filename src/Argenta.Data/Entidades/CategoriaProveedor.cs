namespace Argenta.Data.Entidades;

/// <summary>
/// Categoría especial del proveedor: determina si alguno de sus impuestos
/// específicos (Petróleo, Tasa Municipal, etc.) va a la columna Exento en
/// lugar de formar parte de la base neta. Ver <c>MotorClasificacion.ObtenerImpuestoEspecial</c>.
/// </summary>
public enum CategoriaProveedor
{
    Normal = 0,
    Gasolinera = 1,
    EmpresaElectrica = 2,
}
