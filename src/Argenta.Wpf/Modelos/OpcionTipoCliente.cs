using Argenta.Data.Entidades;

namespace Argenta.Wpf.Modelos;

/// <summary>Opción legible del ComboBox de Tipo de cliente (Profesional / Comercial / Hotel).</summary>
public sealed record OpcionTipoCliente(TipoCliente Valor, string Texto)
{
    public static readonly OpcionTipoCliente[] Todas =
    [
        new(TipoCliente.Profesional, "Profesional"),
        new(TipoCliente.Comercial, "Comercial"),
        new(TipoCliente.Hotel, "Hotel"),
    ];

    public static string ATexto(TipoCliente tipo) =>
        Todas.First(o => o.Valor == tipo).Texto;
}
