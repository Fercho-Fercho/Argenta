namespace Argenta.Data.Entidades;

/// <summary>Qué hacer con las facturas de un proveedor marcado en el catálogo "Proveedores a revisar".</summary>
public enum AccionRevisar
{
    /// <summary>La factura queda incluida, pero resaltada en naranja para revisión manual.</summary>
    Revisar = 0,

    /// <summary>La factura arranca excluida (checkbox "Incluir" desmarcado) y resaltada en rojo suave; el usuario puede reactivarla.</summary>
    ExcluirSiempre = 1,
}
