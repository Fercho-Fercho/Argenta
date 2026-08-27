using System.Windows;

namespace ContaSuite.Wpf.Servicios;

/// <summary>
/// Intercambia Application.Resources.MergedDictionaries[0] (el diccionario
/// de tema, ver App.xaml) por TemaClaro.xaml o TemaOscuro.xaml. Como todas
/// las vistas referencian los colores por DynamicResource (no
/// StaticResource), el cambio se ve al instante en toda la app, sin
/// reiniciar.
/// </summary>
public class TemaService : ITemaService
{
    private const int IndiceDiccionarioTema = 0;

    public TemaApp TemaActual { get; private set; } = TemaApp.Claro;

    public event Action<TemaApp>? TemaCambiado;

    public void AplicarTema(TemaApp tema)
    {
        var uri = new Uri(
            tema == TemaApp.Oscuro ? "Styles/Temas/TemaOscuro.xaml" : "Styles/Temas/TemaClaro.xaml",
            UriKind.Relative);

        var nuevoDiccionario = new ResourceDictionary { Source = uri };

        Application.Current.Resources.MergedDictionaries[IndiceDiccionarioTema] = nuevoDiccionario;
        TemaActual = tema;
        TemaCambiado?.Invoke(tema);
    }
}
