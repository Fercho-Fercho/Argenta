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

## Licencia por computadora autorizada

Argenta solo funciona en computadoras que el desarrollador autorizó
explícitamente. La validación corre en el arranque de la app (nunca en el
instalador, que es fácil de burlar copiando archivos) y se implementa en
`src/Argenta.Wpf/Servicios/Licencia/`:

- **`FingerprintService`**: genera el código único de la computadora
  combinando el `MachineGuid` de Windows (registro
  `HKLM\SOFTWARE\Microsoft\Cryptography`) con el número de serie de la placa
  base (WMI, opcional/mejor esfuerzo), y aplicando SHA-256. Es estable entre
  reinicios y no reversible.
- **`AutorizacionService`**: descarga `autorizadas.json` desde el repositorio
  público **[Fercho-Fercho/argenta-licencias](https://github.com/Fercho-Fercho/argenta-licencias)**
  (separado de este código fuente a propósito) usando la URL *raw* de GitHub,
  sin token porque el repo es público. La URL vive en
  `Licencia:UrlListaAutorizadas` de `appsettings.json`.
- **`CacheLicenciaService`**: guarda la fecha de la última validación exitosa,
  cifrada con DPAPI (`ProtectedData`, ligada al usuario de Windows de esta
  máquina) en `%LocalAppData%\Argenta\Data\licencia.cache`. No es editable a
  mano ni portable a otra computadora/usuario — es solo la caché que sostiene
  el período de gracia offline, nunca la fuente de verdad.
- **`ValidadorLicenciaService`**: combina todo lo anterior en un
  `EstadoLicencia` (`Autorizada` / `EnGracia` / `Bloqueada`).

### Cómo autorizar una computadora nueva

1. El cliente abre Argenta; si su computadora no está autorizada, ve la
   pantalla de bloqueo con su código y un botón para copiarlo.
2. El cliente envía ese código al desarrollador (por el medio que sea).
3. El desarrollador edita `autorizadas.json` en el repo
   [`argenta-licencias`](https://github.com/Fercho-Fercho/argenta-licencias)
   y agrega una entrada:

   ```json
   { "codigo": "<el código de 64 caracteres>", "cliente": "Nombre del contador", "activa": true }
   ```

4. El cliente presiona **"Reintentar validación"** (pantalla de bloqueo) o
   **Ayuda → Licencia / Acerca de → "Validar ahora"** (si ya estaba dentro).
   La app se desbloquea sola, sin reiniciar. No hace falta recompilar ni
   publicar una versión nueva de Argenta.

### Cómo revocar

Cambie `"activa": true` a `"activa": false` (o quite la entrada por completo)
y suba el cambio. En cuanto la app de ese cliente vuelva a validar (arranque
o "Validar ahora"), se bloquea — la revocación explícita del servidor siempre
gana, aunque hubiera período de gracia vigente.

> **Nota:** `raw.githubusercontent.com` cachea el contenido unos minutos en su
> CDN, así que un cambio recién subido puede tardar un rato en verse reflejado
> la primera vez que se consulta.

### Período de gracia offline

Si la app no logra contactar `autorizadas.json` (sin internet, timeout, DNS,
etc.), no bloquea de inmediato: revisa la fecha de la última validación
exitosa guardada localmente y, si fue hace menos de `Licencia:DiasGracia` días
(7 por defecto, configurable en `appsettings.json`), deja usar la app
normalmente. Pasado ese plazo sin poder validar, bloquea hasta que haya
conexión. Esto es distinto de una revocación explícita (`activa: false`), que
siempre bloquea de inmediato en cuanto hay internet.

### Nota de seguridad

Este método alcanza para el caso de uso (evitar copiar la app entre usuarios
y controlar altas/bajas de clientes de forma simple, sin recompilar). No es
infalible ante un atacante experto — toda la lógica de validación viaja
dentro de la app instalada, así que alguien con suficiente conocimiento
técnico podría parchear el binario para saltársela. Para un producto en fase
inicial vendido a contadores, es el mejor equilibrio entre seguridad real y
facilidad de mantenimiento (agregar/quitar un cliente es editar una línea de
JSON, no publicar una versión nueva).

## Datos semilla

`Argenta.Data/Semilla/ProveedoresSemilla.cs` precarga 66 proveedores con su
clasificación (Compra/Servicio) y la marca de gasolinera (solo UNO GUATEMALA,
NIT 321052). `SembradorDatos.cs` además crea el cliente de prueba. La siembra
es idempotente: solo inserta si el catálogo respectivo está vacío.
