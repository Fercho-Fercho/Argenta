using System.Windows;

namespace Argenta.Wpf.Views;

/// <summary>
/// Hace que una ventana quepa en el área útil de la pantalla (sin la barra de
/// tareas). Sin esto, en pantallas chicas o con escala alta de Windows (p. ej.
/// 1920×1080 al 150 % = 1280×720 efectivos) una ventana más grande que la
/// pantalla se centra con el borde superior FUERA de la pantalla, y la barra
/// de título (cerrar/minimizar/maximizar) queda oculta.
/// Se llama en el constructor de cada ventana, después de InitializeComponent.
/// </summary>
public static class AjustePantalla
{
    /// <param name="maximizarSiNoCabe">
    /// true para la ventana principal: si su tamaño de diseño no cabe, abre
    /// maximizada (y al restaurarla queda de un tamaño que sí cabe).
    /// </param>
    public static void Ajustar(Window ventana, bool maximizarSiNoCabe = false)
    {
        var area = SystemParameters.WorkArea;

        var noCabe = (!double.IsNaN(ventana.Width) && ventana.Width > area.Width)
            || (!double.IsNaN(ventana.Height) && ventana.Height > area.Height);

        ventana.MinWidth = Math.Min(ventana.MinWidth, area.Width);
        ventana.MinHeight = Math.Min(ventana.MinHeight, area.Height);

        if (!double.IsNaN(ventana.Width)) ventana.Width = Math.Min(ventana.Width, area.Width);
        if (!double.IsNaN(ventana.Height)) ventana.Height = Math.Min(ventana.Height, area.Height);

        // Diálogos que se dimensionan solos por su contenido (SizeToContent):
        // su alto final no se conoce aquí, así que se topa con MaxHeight.
        if (ventana.SizeToContent is SizeToContent.Height or SizeToContent.WidthAndHeight)
        {
            ventana.MaxHeight = Math.Min(ventana.MaxHeight, area.Height);
        }

        if (noCabe && maximizarSiNoCabe)
        {
            ventana.WindowState = WindowState.Maximized;
        }
    }
}
