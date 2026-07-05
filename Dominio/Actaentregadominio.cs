using System;
using System.Collections.Generic;
using System.Data;
using AccesoDatos;

namespace Dominio
{
    // ═══════════════════════════════════════════════════════════════════
    // MODELOS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Un equipo/activo dentro del acta. "Incluido" controla si entra o no
    /// en el PDF final (por si el colaborador no recibe TODO lo asignado
    /// en un mismo acto, por ejemplo dispositivos en préstamo aparte).
    /// "Estado" es la calificación 1-10 declarada en el formato original.
    /// </summary>
    public class ActivoActaItem
    {
        public Guid ActivoID { get; set; }
        public string Categoria { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public string NumeroSerie { get; set; } = "";

        public bool Incluido { get; set; } = true;
        public int Estado { get; set; } = 10; // 1 (malo) - 10 (perfecto), por defecto "perfecto estado"

        public string MarcaModelo => string.IsNullOrWhiteSpace(Modelo) ? Marca : $"{Marca} {Modelo}".Trim();
    }

    /// <summary>
    /// Toda la información necesaria para renderizar el Acta de Entrega,
    /// fiel a la plantilla física: datos del colaborador, lugar/fecha,
    /// listado de equipos y las tres firmas (Colaborador, Coordinador, Líder TI).
    /// </summary>
    public class ActaEntregaModel
    {
        // Encabezado
        public string Lugar { get; set; } = "";
        public DateTime Fecha { get; set; } = DateTime.Today;

        // Colaborador (se precarga automáticamente desde Core.Colaboradores)
        public int ColaboradorID { get; set; }
        public string NombreCompleto { get; set; } = "";
        public string DocumentoIdentidad { get; set; } = "";
        public string Cargo { get; set; } = "";

        // Equipos a entregar
        public List<ActivoActaItem> Activos { get; set; } = new();

        // Campos libres de cierre del acta
        public string Observaciones { get; set; } = "";
        public string NombreCoordinador { get; set; } = "";
        public string NombreLiderTI { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════════════════════
    // DOMINIO
    // ═══════════════════════════════════════════════════════════════════

    public class ActaEntregaDominio
    {
        private readonly ActaEntregaAccesoDatos _datos = new();

        /// <summary>Para poblar el ComboBox de selección de colaborador.</summary>
        public DataTable ListarColaboradores() => _datos.ObtenerColaboradoresActivos();

        /// <summary>
        /// Construye el modelo completo del acta (datos del colaborador + sus
        /// activos actualmente asignados) listo para mostrar en pantalla y,
        /// luego, para exportar a PDF.
        /// </summary>
        public ActaEntregaModel PrepararActa(int colaboradorId)
        {
            var fila = _datos.ObtenerColaborador(colaboradorId)
                ?? throw new InvalidOperationException("No se encontró el colaborador seleccionado.");

            var modelo = new ActaEntregaModel
            {
                ColaboradorID = colaboradorId,
                NombreCompleto = $"{fila["Nombres"]} {fila["Apellidos"]}".Trim(),
                DocumentoIdentidad = fila["DocumentoIdentidad"]?.ToString() ?? "",
                Cargo = fila["Cargo"]?.ToString() ?? "",
            };

            var dtActivos = _datos.ObtenerActivosAsignados(colaboradorId);
            foreach (DataRow r in dtActivos.Rows)
            {
                modelo.Activos.Add(new ActivoActaItem
                {
                    ActivoID = r["ActivoID"] is Guid g ? g : Guid.Parse(r["ActivoID"].ToString()!),
                    Categoria = r["Categoria"]?.ToString() ?? "",
                    Marca = r["Marca"]?.ToString() ?? "",
                    Modelo = r["Modelo"]?.ToString() ?? "",
                    NumeroSerie = r["NumeroSerie"]?.ToString() ?? "",
                });
            }

            return modelo;
        }

        /// <summary>Genera el PDF del acta en la ruta indicada.</summary>
        public void GenerarPdf(ActaEntregaModel modelo, string rutaDestino)
            => ActaEntregaPdfService.Generar(modelo, rutaDestino);
    }
}