using ContaSuite.Data.Entidades;

namespace ContaSuite.Wpf.Modelos;

/// <summary>Opción legible del ComboBox de Acción (Revisar / Excluir siempre).</summary>
public sealed record OpcionAccionRevisar(AccionRevisar Valor, string Texto)
{
    public static readonly OpcionAccionRevisar[] Todas =
    [
        new(AccionRevisar.Revisar, "Revisar"),
        new(AccionRevisar.ExcluirSiempre, "Excluir siempre"),
    ];

    public static string ATexto(AccionRevisar accion) =>
        Todas.First(o => o.Valor == accion).Texto;
}
