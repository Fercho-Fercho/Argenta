namespace ContaSuite.Wpf.Modelos;

/// <summary>
/// Una fila del diálogo "Folio inicial" (<see cref="Views.Dialogos.FolioInicialDialogo"/>):
/// pide el folio con el que debe empezar la primera hoja de un libro/establecimiento
/// dado, porque el número físico de "libro habilitado" de cada establecimiento
/// puede llevar su propio conteo (no continúa el del anterior).
/// </summary>
public sealed class FolioInicialFila
{
    public required string Etiqueta { get; init; }

    /// <summary>0 para libro de compras (siempre un solo libro); <c>Establecimiento.Numero</c> para ventas.</summary>
    public required int NumeroEstablecimiento { get; init; }

    /// <summary>Texto editable del TextBox; se valida y se convierte a <see cref="FolioInicial"/> al continuar.</summary>
    public string FolioTexto { get; set; } = "1";

    public int FolioInicial { get; set; } = 1;
}
