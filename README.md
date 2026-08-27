# Argenta

Suite de escritorio para Windows que apoya a contadores en Guatemala con tareas
contables recurrentes. Crece por módulos: el primero es **Libro de Compras**
(genera el "Libro de Compras de Bienes y Servicios Adquiridos" a partir del
Excel de facturas recibidas que se descarga del portal de la SAT). Próximos
módulos (Libro de Ventas, retenciones, conciliaciones, etc.) se agregan sin
tocar el shell ni los módulos existentes — ver [Cómo agregar un módulo
nuevo](#cómo-agregar-un-módulo-nuevo).

## Regla de privacidad

La base de datos local (SQLite) **solo** guarda catálogos: Clientes,
Proveedores y Tipos de Cambio. **Nunca** se guarda información de facturas.
Los documentos se leen del Excel/CSV, se procesan en memoria y se descartan
al terminar de generar cada reporte.

## Stack

- .NET 8 (LTS), C#
- WPF (MVVM) con [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- EF Core 8 + SQLite (catálogos)
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) (escribir `.xlsx`)
- [NPOI](https://github.com/nissl-lab/npoi) (leer `.xls` binario de la SAT)
- [CsvHelper](https://joshclose.github.io/CsvHelper/) (CSV de tipo de cambio del Banguat)
- [Velopack](https://velopack.io/) (auto-actualización)
- Microsoft.Extensions.DependencyInjection (contenedor de DI)

## Estructura de la solución

```
Argenta.slnx
src/
  Argenta.Core/                  Contratos y utilidades compartidas (sin WPF ni EF)
  Argenta.Data/                  EF Core + SQLite: entidades de catálogos, migraciones, semilla
  Argenta.Modules.LibroCompras/  Lógica del módulo: parseo, clasificación, validaciones, generación .xlsx
  Argenta.Wpf/                   Shell de navegación, vistas y ViewModels
Docs/Ejemplos/                      Archivos de ejemplo reales (Excel SAT, CSV Banguat, libro modelo)
```

- **Argenta.Core**: interfaces base (`IModuloContable`), motor de
  validaciones genérico, conversión de moneda, redondeo, normalización de NIT.
  No depende de WPF ni de EF Core.
- **Argenta.Data**: `ArgentaDbContext`, entidades `Cliente`,
  `Proveedor`, `TipoCambio`, repositorios, migraciones y semilla de datos
  (66 proveedores + un cliente de prueba).
- **Argenta.Modules.LibroCompras**: lector del `.xls` de la SAT (NPOI),
  lector del CSV del Banguat (CsvHelper), motor de clasificación (orden de
  precedencia del libro de compras), reglas de validación del módulo y
  generador del `.xlsx` final (ClosedXML).
- **Argenta.Wpf**: `MainWindow` (shell), `ShellViewModel` (arma el menú a
  partir de los módulos registrados por DI), vistas/ViewModels de Catálogos
  (Clientes, Proveedores, Tipo de Cambio) y de la operación "Generar libro de
  compras".

## Compilar

Requiere el SDK de .NET 8 (o superior, siempre que incluya el *runtime pack*
de `net8.0-windows`).

```powershell
dotnet build Argenta.slnx
```

## Ejecutar

```powershell
dotnet run --project src/Argenta.Wpf/Argenta.Wpf.csproj
```

En el primer arranque:

1. Se crea la base de datos SQLite en `%LOCALAPPDATA%\Argenta\argenta.db`.
2. Se aplican las migraciones de EF Core (con respaldo automático del archivo
   `.db` antes de migrar; si la migración falla, se restaura el respaldo — ver
   `Argenta.Data/Servicios/RespaldoBaseDatosService.cs`).
3. Se siembran los 66 proveedores conocidos y el cliente de prueba (Randall
   Manuel Lou Meda, NIT 468783-3).

### Probar con los archivos de ejemplo

En `Docs/Ejemplos/` hay un Excel real de facturas de la SAT, un CSV real del
Banguat y el libro de compras modelo (formato de referencia). Desde
**Tipo de Cambio → Importar archivo...** cargue el CSV, y desde
**Operaciones → Generar libro de compras** seleccione el cliente de prueba y
el `.xls` de ejemplo para generar el libro y comparar los formatos.

> Nota: el `.xls` de ejemplo y el libro modelo corresponden a periodos
> distintos (el modelo es de referencia de formato), así que sus sumas no
> van a coincidir entre sí. La prueba de aceptación real (sección más abajo)
> se hace comparando el libro generado contra un libro modelo **del mismo
> periodo**.

## Prueba de aceptación

Genere el libro con un Excel de facturas real y compare las **sumas de las
columnas H (Compras), I (Servicios), J (Exento), K (Iva) y L (Total)** contra
el libro de compras modelo del mismo periodo. Si los totales cuadran (con el
redondeo ".5 hacia arriba"), la lógica de clasificación es correcta.

## Publicar una versión con Velopack

1. Publique la app como *framework-dependent* o *self-contained*:

   ```powershell
   dotnet publish src/Argenta.Wpf/Argenta.Wpf.csproj -c Release -r win-x64 --self-contained -o publish
   ```

2. Empaquete el instalador con la herramienta `vpk` (`dotnet tool install -g vpk`):

   ```powershell
   vpk pack --packId Argenta --packVersion 1.0.0 --packDir publish --mainExe Argenta.Wpf.exe
   ```

3. Suba la carpeta de salida (`Releases/`) al servidor/feed configurado en
   `src/Argenta.Wpf/appsettings.json` (`Actualizaciones:UrlFeed`).

4. Desde **Ayuda → Buscar actualizaciones**, la app detecta, descarga y aplica
   la nueva versión (`Velopack.UpdateManager`).

## Cómo agregar un módulo nuevo

El shell nunca conoce los módulos de antemano: los descubre por inyección de
dependencias a través de `IEnumerable<IModuloContable>`
(`Argenta.Core.Modulos.IModuloContable`). Para agregar, por ejemplo, el
futuro **Libro de Ventas**, sin tocar el módulo de Compras ni el resto del
shell:

1. Cree el proyecto `Argenta.Modules.LibroVentas` (referencia a `Core` y
   `Data`) con su propia lógica de negocio (parseo, clasificación,
   validaciones, generación del reporte), siguiendo el mismo patrón que
   `Argenta.Modules.LibroCompras`.
2. En `Argenta.Wpf`, agregue el ViewModel y la vista (XAML) de su(s)
   operación(es), y una `DataTemplate` para esa vista en
   `Views/PlantillasVistas.xaml`.
3. Implemente `IModuloContable` (por ejemplo `Modulos/LibroVentasModulo.cs`)
   describiendo su `Nombre`, `Icono`, `Orden` y los `ElementoMenuModulo` que
   debe mostrar en "Operaciones".
4. Registre en `App.xaml.cs`:
   - los servicios del nuevo módulo (`servicios.AddModuloLibroVentas()`, con
     su propia extensión de `IServiceCollection`),
   - `servicios.AddSingleton<IModuloContable, LibroVentasModulo>();`,
   - los ViewModels nuevos como `Transient`.

El `ShellViewModel` arma el menú "Operaciones" recorriendo todos los
`IModuloContable` registrados, así que el módulo nuevo aparece automáticamente
sin más cambios. Los catálogos de Clientes y Tipo de Cambio ya son
compartidos y reutilizables por el módulo nuevo a través de
`IClienteRepositorio` / `ITipoCambioRepositorio` / `IProveedorTipoCambio`.

## Datos semilla

`Argenta.Data/Semilla/ProveedoresSemilla.cs` precarga 66 proveedores con su
clasificación (Compra/Servicio) y la marca de gasolinera (solo UNO GUATEMALA,
NIT 321052). `SembradorDatos.cs` además crea el cliente de prueba. La siembra
es idempotente: solo inserta si el catálogo respectivo está vacío.
