namespace ContaSuite.Wpf.Servicios;

/// <summary>Carga y guarda las preferencias del usuario en un archivo local (no es un dato de factura).</summary>
public interface IPreferenciasService
{
    PreferenciasApp Cargar();
    void Guardar(PreferenciasApp preferencias);
}
