using System.Globalization;
using System.Text.Json;
using ContaSuite.Core.Utilidades;
using ContaSuite.Data.Entidades;
using Microsoft.EntityFrameworkCore;

namespace ContaSuite.Data.Servicios;

/// <summary>
/// Ver <see cref="IImportacionDatosService"/>. Toda fila cuyo nombre contenga
/// "EJEMPLO - BORRAR" o cuyo NIT (normalizado) sea "EJEMPLO-000000" se
/// ignora al importar — es la fila de muestra de la plantilla, no un dato
/// real. Las filas completamente en blanco también se ignoran en silencio.
/// </summary>
public sealed class ImportacionDatosService(IDbContextFactory<ContaSuiteDbContext> fabricaDb) : IImportacionDatosService
{
    private const string MarcaNombreEjemplo = "EJEMPLO - BORRAR";
    private static readonly string NitEjemploNormalizado = NitUtil.Normalizar("EJEMPLO-000000");

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public DatosContaSuiteJson ParsearJson(string contenidoJson)
    {
        DatosContaSuiteJson? datos;
        try
        {
            datos = JsonSerializer.Deserialize<DatosContaSuiteJson>(contenidoJson, OpcionesJson);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"El archivo no es un .json válido: {ex.Message}", ex);
        }

        if (datos is null) throw new FormatException("El archivo está vacío o no tiene el formato esperado.");
        if (datos.Version != 1)
        {
            throw new FormatException(
                $"Versión de archivo no soportada ({datos.Version}). Esta versión de ContaSuite solo admite archivos de versión 1.");
        }

        return datos;
    }

    public Task<ResumenImportacion> PrevisualizarAsync(DatosContaSuiteJson datos) => ProcesarAsync(datos, aplicarCambios: false);

    public Task<ResumenImportacion> ImportarAsync(DatosContaSuiteJson datos) => ProcesarAsync(datos, aplicarCambios: true);

    public string GenerarPlantillaVacia()
    {
        // No lee la base de datos en ningún momento: la plantilla es siempre
        // esta estructura fija con una fila de ejemplo, nunca datos reales.
        var plantilla = new DatosContaSuiteJson
        {
            Version = 1,
            Clientes =
            [
                new DatosClienteJson
                {
                    Nit = "EJEMPLO-000000",
                    Nombre = "EJEMPLO - BORRAR ESTA FILA",
                    Establecimientos = [new DatosEstablecimientoJson { Numero = 1, Nombre = "EJEMPLO - BORRAR ESTA FILA", Tipo = "Comercial", Exporta = false }],
                },
            ],
            Proveedores = [new DatosProveedorJson { Nit = "EJEMPLO-000000", Nombre = "EJEMPLO - BORRAR ESTA FILA", Tipo = "Compra", Categoria = "Normal" }],
            ProveedoresRevisar = [new DatosProveedorRevisarJson { Nit = "EJEMPLO-000000", Nombre = "EJEMPLO - BORRAR ESTA FILA", Accion = "Revisar" }],
            TiposCambio = [new DatosTipoCambioJson { Fecha = "2026-01-01", Valor = 0.0m }],
            Selecciones = [new DatosSeleccionJson { NitCliente = "", Anio = DateTime.Today.Year, Mes = DateTime.Today.Month, TipoLibro = "Compras", IdentificadorFactura = "", Incluida = true }],
        };

        return JsonSerializer.Serialize(plantilla, OpcionesJson);
    }

    /// <summary>
    /// Único método que toca la base de datos: cuenta lo que se insertaría/
    /// actualizaría y, si <paramref name="aplicarCambios"/> es true, además
    /// lo guarda. Previsualizar e Importar comparten esta misma lógica para
    /// no arriesgarse a que el resumen mostrado no coincida con lo aplicado.
    /// </summary>
    private async Task<ResumenImportacion> ProcesarAsync(DatosContaSuiteJson datos, bool aplicarCambios)
    {
        var errores = new List<string>();
        var filasIgnoradas = 0;
        int clientesNuevos = 0, clientesActualizados = 0;
        int establecimientosNuevos = 0, establecimientosActualizados = 0;
        int proveedoresNuevos = 0, proveedoresActualizados = 0;
        int revisarNuevos = 0, revisarActualizados = 0;
        int tiposCambioNuevos = 0, tiposCambioActualizados = 0;
        int seleccionesNuevas = 0, seleccionesActualizadas = 0;

        await using var db = await fabricaDb.CreateDbContextAsync();

        // --- Clientes (+ sus establecimientos anidados, por Numero único dentro del cliente) ---
        var clientesExistentes = await db.Clientes.Include(c => c.Establecimientos).ToListAsync();
        var indice = 0;
        foreach (var fila in datos.Clientes)
        {
            indice++;
            if (EsFilaEnBlancoOEjemplo(fila.Nit, fila.Nombre)) { filasIgnoradas++; continue; }
            if (string.IsNullOrWhiteSpace(fila.Nit)) { errores.Add($"Cliente #{indice}: falta el NIT."); continue; }
            if (string.IsNullOrWhiteSpace(fila.Nombre)) { errores.Add($"Cliente #{indice} (NIT \"{fila.Nit}\"): falta el nombre."); continue; }

            var nitNormalizado = NitUtil.Normalizar(fila.Nit);
            var existente = clientesExistentes.FirstOrDefault(c => NitUtil.Normalizar(c.Nit) == nitNormalizado);

            if (existente is null && fila.Establecimientos.Count == 0)
            {
                errores.Add($"Cliente \"{fila.Nombre}\": debe tener al menos un establecimiento.");
                continue;
            }

            Cliente cliente;
            if (existente is not null)
            {
                clientesActualizados++;
                cliente = existente;
                if (aplicarCambios) existente.Nombre = fila.Nombre;
            }
            else
            {
                clientesNuevos++;
                cliente = new Cliente { Nit = fila.Nit, Nombre = fila.Nombre, Activo = true };
                if (aplicarCambios) db.Clientes.Add(cliente);
            }

            var indiceEst = 0;
            foreach (var filaEst in fila.Establecimientos)
            {
                indiceEst++;
                if (filaEst.Numero <= 0)
                {
                    errores.Add($"Cliente \"{fila.Nombre}\", establecimiento #{indiceEst}: el número debe ser mayor que 0.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(filaEst.Nombre))
                {
                    errores.Add($"Cliente \"{fila.Nombre}\", establecimiento {filaEst.Numero}: falta el nombre.");
                    continue;
                }
                if (!Enum.TryParse<TipoCliente>(filaEst.Tipo, ignoreCase: true, out var tipoEstablecimiento))
                {
                    errores.Add($"Cliente \"{fila.Nombre}\", establecimiento {filaEst.Numero}: tipo \"{filaEst.Tipo}\" no válido (use Profesional, Comercial u Hotel).");
                    continue;
                }

                var establecimientoExistente = cliente.Establecimientos.FirstOrDefault(e => e.Numero == filaEst.Numero);
                if (establecimientoExistente is not null)
                {
                    establecimientosActualizados++;
                    if (aplicarCambios)
                    {
                        establecimientoExistente.Nombre = filaEst.Nombre;
                        establecimientoExistente.Tipo = tipoEstablecimiento;
                        establecimientoExistente.Exporta = filaEst.Exporta;
                    }
                }
                else
                {
                    establecimientosNuevos++;
                    if (aplicarCambios)
                    {
                        cliente.Establecimientos.Add(new Establecimiento
                        {
                            Numero = filaEst.Numero,
                            Nombre = filaEst.Nombre,
                            Tipo = tipoEstablecimiento,
                            Exporta = filaEst.Exporta,
                        });
                    }
                }
            }
        }

        // --- Proveedores ---
        var proveedoresExistentes = await db.Proveedores.ToListAsync();
        indice = 0;
        foreach (var fila in datos.Proveedores)
        {
            indice++;
            if (EsFilaEnBlancoOEjemplo(fila.Nit, fila.Nombre)) { filasIgnoradas++; continue; }
            if (string.IsNullOrWhiteSpace(fila.Nit)) { errores.Add($"Proveedor #{indice}: falta el NIT."); continue; }
            if (string.IsNullOrWhiteSpace(fila.Nombre)) { errores.Add($"Proveedor #{indice} (NIT \"{fila.Nit}\"): falta el nombre."); continue; }
            if (!Enum.TryParse<TipoProveedor>(fila.Tipo, ignoreCase: true, out var tipo))
            {
                errores.Add($"Proveedor \"{fila.Nombre}\": tipo \"{fila.Tipo}\" no válido (use Compra o Servicio).");
                continue;
            }
            if (!Enum.TryParse<CategoriaProveedor>(fila.Categoria, ignoreCase: true, out var categoria))
            {
                errores.Add($"Proveedor \"{fila.Nombre}\": categoría \"{fila.Categoria}\" no válida (use Normal, Gasolinera o EmpresaElectrica).");
                continue;
            }

            var nitNormalizado = NitUtil.Normalizar(fila.Nit);
            var existente = proveedoresExistentes.FirstOrDefault(p => NitUtil.Normalizar(p.Nit) == nitNormalizado);
            if (existente is not null)
            {
                proveedoresActualizados++;
                if (aplicarCambios)
                {
                    existente.Nombre = fila.Nombre;
                    existente.Tipo = tipo;
                    existente.Categoria = categoria;
                }
            }
            else
            {
                proveedoresNuevos++;
                if (aplicarCambios) db.Proveedores.Add(new Proveedor { Nit = nitNormalizado, Nombre = fila.Nombre, Tipo = tipo, Categoria = categoria });
            }
        }

        // --- Proveedores a revisar ---
        var revisarExistentes = await db.ProveedoresRevisar.ToListAsync();
        indice = 0;
        foreach (var fila in datos.ProveedoresRevisar)
        {
            indice++;
            if (EsFilaEnBlancoOEjemplo(fila.Nit, fila.Nombre)) { filasIgnoradas++; continue; }
            if (string.IsNullOrWhiteSpace(fila.Nit)) { errores.Add($"Proveedor a revisar #{indice}: falta el NIT."); continue; }
            if (string.IsNullOrWhiteSpace(fila.Nombre)) { errores.Add($"Proveedor a revisar #{indice} (NIT \"{fila.Nit}\"): falta el nombre."); continue; }
            if (!Enum.TryParse<AccionRevisar>(fila.Accion, ignoreCase: true, out var accion))
            {
                errores.Add($"Proveedor a revisar \"{fila.Nombre}\": acción \"{fila.Accion}\" no válida (use Revisar o ExcluirSiempre).");
                continue;
            }

            var nitNormalizado = NitUtil.Normalizar(fila.Nit);
            var existente = revisarExistentes.FirstOrDefault(p => NitUtil.Normalizar(p.Nit) == nitNormalizado);
            if (existente is not null)
            {
                revisarActualizados++;
                if (aplicarCambios)
                {
                    existente.Nombre = fila.Nombre;
                    existente.Accion = accion;
                }
            }
            else
            {
                revisarNuevos++;
                if (aplicarCambios) db.ProveedoresRevisar.Add(new ProveedorRevisar { Nit = nitNormalizado, Nombre = fila.Nombre, Accion = accion });
            }
        }

        // --- Tipos de cambio ---
        var tiposCambioExistentes = await db.TiposCambio.ToListAsync();
        indice = 0;
        foreach (var fila in datos.TiposCambio)
        {
            indice++;
            if (string.IsNullOrWhiteSpace(fila.Fecha) && fila.Valor == 0m) { filasIgnoradas++; continue; }
            if (!DateTime.TryParseExact(fila.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                errores.Add($"Tipo de cambio #{indice}: fecha \"{fila.Fecha}\" no válida (use el formato aaaa-mm-dd).");
                continue;
            }
            if (fila.Valor <= 0m)
            {
                errores.Add($"Tipo de cambio del {fecha:dd/MM/yyyy}: el valor debe ser mayor que 0.");
                continue;
            }

            var existente = tiposCambioExistentes.FirstOrDefault(t => t.Fecha.Date == fecha.Date);
            if (existente is not null)
            {
                tiposCambioActualizados++;
                if (aplicarCambios) existente.Valor = fila.Valor;
            }
            else
            {
                tiposCambioNuevos++;
                if (aplicarCambios) db.TiposCambio.Add(new TipoCambio { Fecha = fecha.Date, Valor = fila.Valor });
            }
        }

        // --- Selecciones (memoria de inclusión/exclusión, solo hashes) ---
        var seleccionesExistentes = await db.SeleccionesFactura.ToListAsync();
        indice = 0;
        foreach (var fila in datos.Selecciones)
        {
            indice++;
            if (EsFilaEnBlancoOEjemplo(fila.NitCliente, null) || string.IsNullOrWhiteSpace(fila.IdentificadorFactura))
            {
                filasIgnoradas++;
                continue;
            }
            if (!Enum.TryParse<TipoLibro>(fila.TipoLibro, ignoreCase: true, out var tipoLibro))
            {
                errores.Add($"Selección #{indice} (NIT {fila.NitCliente}): libro \"{fila.TipoLibro}\" no válido (use Compras o Ventas).");
                continue;
            }
            if (fila.Anio < 2000 || fila.Mes is < 1 or > 12)
            {
                errores.Add($"Selección #{indice} (NIT {fila.NitCliente}): año/mes inválido ({fila.Anio}/{fila.Mes}).");
                continue;
            }

            var nitNormalizado = NitUtil.Normalizar(fila.NitCliente);
            var existente = seleccionesExistentes.FirstOrDefault(s =>
                s.NitCliente == nitNormalizado && s.Anio == fila.Anio && s.Mes == fila.Mes &&
                s.TipoLibro == tipoLibro && s.IdentificadorFactura == fila.IdentificadorFactura);

            if (existente is not null)
            {
                seleccionesActualizadas++;
                if (aplicarCambios) existente.Incluida = fila.Incluida;
            }
            else
            {
                seleccionesNuevas++;
                if (aplicarCambios)
                {
                    db.SeleccionesFactura.Add(new SeleccionFactura
                    {
                        NitCliente = nitNormalizado,
                        Anio = fila.Anio,
                        Mes = fila.Mes,
                        TipoLibro = tipoLibro,
                        IdentificadorFactura = fila.IdentificadorFactura,
                        Incluida = fila.Incluida,
                    });
                }
            }
        }

        if (aplicarCambios) await db.SaveChangesAsync();

        return new ResumenImportacion(
            clientesNuevos, clientesActualizados,
            establecimientosNuevos, establecimientosActualizados,
            proveedoresNuevos, proveedoresActualizados,
            revisarNuevos, revisarActualizados,
            tiposCambioNuevos, tiposCambioActualizados,
            seleccionesNuevas, seleccionesActualizadas,
            filasIgnoradas,
            errores);
    }

    private static bool EsFilaEnBlancoOEjemplo(string? nit, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nit) && string.IsNullOrWhiteSpace(nombre)) return true;
        if (!string.IsNullOrWhiteSpace(nombre) && nombre.Contains(MarcaNombreEjemplo, StringComparison.OrdinalIgnoreCase)) return true;
        if (!string.IsNullOrWhiteSpace(nit) && NitUtil.Normalizar(nit) == NitEjemploNormalizado) return true;
        return false;
    }
}
