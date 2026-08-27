namespace ContaSuite.Wpf.Servicios;

/// <summary>Aplica el tema de color (claro/oscuro) intercambiando el diccionario de recursos activo.</summary>
public interface ITemaService
{
    TemaApp TemaActual { get; }

    /// <summary>Se dispara después de <see cref="AplicarTema"/>, para que otras pantallas (p. ej. el botón ☀/🌙 del topbar) se mantengan sincronizadas sin importar desde dónde se cambió el tema.</summary>
    event Action<TemaApp>? TemaCambiado;

    /// <summary>Cambia el tema activo al instante (sin reiniciar la app); no lo persiste, ver <see cref="IPreferenciasService"/>.</summary>
    void AplicarTema(TemaApp tema);
}
