using Argenta.Core.Utilidades;
using Argenta.Data.Entidades;

namespace Argenta.Data.Semilla;

/// <summary>
/// Catálogo inicial de 66 proveedores conocidos, extraído del libro de compras
/// modelo. Se siembra una sola vez, en la primera ejecución de la aplicación.
/// </summary>
public static class ProveedoresSemilla
{
    private static readonly (string Nit, string Nombre, TipoProveedor Tipo, CategoriaProveedor Categoria)[] Datos =
    [
        ("74973657", "TENDENCIAS COMERCIALES", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("100738311", "ARGANZUELA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("1367714", "ASEGURADORA FIDELIS, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("29358647", "ASOCIACION DE VECINOS DE FINCA SAN RAFAEL Y ANEXOS", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("4323742", "ASOCIACION ESPAÑOLA DE BENEFICENCIA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("7080506", "ASOCIACION GUATEMALTECA DE DESARROLLO FAMILIAR ONG DE CARIDAD Y SERVICIOS -AGUDEF-ONG-", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("5559731", "BELLUNO SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("74485067", "BUNNA GROUP, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("119016591", "CENTRAL DEPOT, SOCIEDAD ANÓNIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("5498104", "COMUNICACIONES CELULARES, S.A.", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("54885051", "CONSTRUCTORES Y CONSULTORES VIALES, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("6521150", "CORPORACION MERINO, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("93648324", "DEL GRIEGO, SOCIEDAD ANÓNIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("1077236", "DESARROLLOS INMOBILIARIOS CONCEPCION, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("326267", "DISTRIBUIDORA DE MATERIALES LA PINTURA, LIMITADA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("73999504", "DOLLARCITY GUATEMALA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("110367782", "EL ESPARTANO, SOCIEDAD ANÓNIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("8034281", "EL INJERTO, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("326445", "EMPRESA ELECTRICA DE GUATEMALA SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.EmpresaElectrica),
        ("337854", "EMPRESA HOSPITALARIA CEMESA SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("88263533", "GREEN TEA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("103319700", "GRUPO DE TIENDAS ASOCIADAS, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("109765796", "GRUPO GECO SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("84518227", "GRUPO TOBRA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("87514419", "GUATE INMUEBLES Y SERVICIOS HOSPITALARIOS SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("69723125", "IMAGINOVA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("24275328", "IMPORTADORA Y EXPORTADORA MARKELY, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("4521587", "INDUSTRIA DE HAMBURGUESAS SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("12778419", "INMOBILIARIA CIMIENTOS, S.A.", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("47704926", "INMOBILIARIA FONTABELLA, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("8275637", "INVERMEDICS, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("6604455", "INVERSIONES MOKA , SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("12736384", "IRENE BEATRIZ , HERNÁNDEZ CASTILLO", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("2595206", "JUAN CARLOS , CASTRO QUIÑONEZ", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("95043101", "LA GALLETERÍA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("28155106", "LA PANERIA SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("76141748", "LA PANOTECA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("8466114", "MAPFRE | SEGUROS GUATEMALA, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("120046970", "NEW RETAIL S.A", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("90666240", "NOCURE, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("32375913", "NUEVOS ALMACENES, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("7378106", "OPERADORA DE TIENDAS, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("4761065", "PALACE SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("81281188", "PARQUEO MAJADAS SOCIEDAD ANÓNIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("56079427", "PARQUEO SIXTINO, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("5686865", "POLLO BRUJO DE CENTROAMERICA, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("904945", "POLLO CAMPERO SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("14940450", "PRICESMART (GUATEMALA) SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("1698826", "PRODUCTOS SUPERB ESPECIAS, S.A.", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("321737", "SEGUROS G&T SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("96173076", "SERVICIOS DE ALTURA, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("74859005", "SERVICIOS INNOVADORES DE COMUNICACIÓN Y ENTRETENIMIENTO, S.A", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("104242663", "SOLUCIONES DE COLOCACION Y TRANSPORTE, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("14945908", "SOPHOS", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("7993765", "SUBWAY, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("11941502", "TAHOE SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("76412865", "TAIM SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("9929290", "TELECOMUNICACIONES DE GUATEMALA, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("90072251", "TRES ELEFANTES, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("26532476", "UNISUPER, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("33480788", "VALORES HOTELEROS, SOCIEDAD ANONIMA", TipoProveedor.Servicio, CategoriaProveedor.Normal),
        ("29685508", "VINOTECA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("65558251", "XOCOLI SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("321052", "UNO GUATEMALA, SOCIEDAD ANONIMA", TipoProveedor.Compra, CategoriaProveedor.Gasolinera),
        ("120685280", "ASOCIACIÓN ACCIONADA EDIFICIO LAS MERCEDES", TipoProveedor.Compra, CategoriaProveedor.Normal),
        ("116055049", "ASOCIACIÓN DE PROPIETARIOS DEL CONDOMINIO HACIENDA DEL COMENDADOR", TipoProveedor.Compra, CategoriaProveedor.Normal),
    ];

    public static IReadOnlyList<Proveedor> ObtenerProveedores() =>
        Datos.Select(d => new Proveedor
        {
            Nit = NitUtil.Normalizar(d.Nit),
            Nombre = d.Nombre,
            Tipo = d.Tipo,
            Categoria = d.Categoria,
        }).ToList();
}
