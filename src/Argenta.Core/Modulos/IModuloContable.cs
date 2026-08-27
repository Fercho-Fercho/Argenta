namespace Argenta.Core.Modulos;

/// <summary>
/// Un elemento del menú "Operaciones" que aporta un módulo. Al seleccionarlo,
/// el shell resuelve <see cref="TipoViewModel"/> mediante inyección de dependencias
/// y lo muestra usando el DataTemplate registrado para ese tipo de ViewModel.
/// </summary>
public sealed class ElementoMenuModulo
{
    public required string Nombre { get; init; }
    public required Type TipoViewModel { get; init; }
}

/// <summary>
/// Abstracción que debe implementar cada módulo contable (Libro de Compras,
/// y en el futuro Libro de Ventas, etc.) para aparecer en el shell.
///
/// Agregar un módulo nuevo NO requiere modificar los módulos existentes: solo
/// se crea el proyecto del módulo, se implementa esta interfaz y se registra
/// la implementación en el contenedor de DI del shell (ver README, sección
/// "Cómo agregar un módulo nuevo").
/// </summary>
public interface IModuloContable
{
    /// <summary>Identificador estable del módulo (para logs/telemetría futura).</summary>
    string Id { get; }

    /// <summary>Nombre visible en el menú de Operaciones.</summary>
    string Nombre { get; }

    /// <summary>Glifo o emoji que representa al módulo en la navegación.</summary>
    string Icono { get; }

    /// <summary>Orden relativo dentro del menú de Operaciones (menor = primero).</summary>
    int Orden { get; }

    /// <summary>Entradas de menú (operaciones) que aporta este módulo.</summary>
    IReadOnlyList<ElementoMenuModulo> ObtenerElementosMenu();
}
