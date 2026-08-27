namespace ContaSuite.Data.Entidades;

/// <summary>
/// Tipo de cliente (vendedor) para efectos del libro de ventas: determina qué
/// puede vender (<see cref="Profesional"/> solo Servicios) y qué columnas
/// extra aparecen en el libro (INGUAT para <see cref="Hotel"/>).
/// </summary>
public enum TipoCliente
{
    Profesional = 0,
    Comercial = 1,
    Hotel = 2,
}
