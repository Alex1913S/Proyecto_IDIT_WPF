using System;
using System.Data;

namespace Dominio
{
    public class MantenimientoDominio
    {
        private readonly AccesoDatos.MantenimientoAccesoDatos _datos = new();

        public DataTable Listar(string filtroEstado = null, string busqueda = null)
            => _datos.ObtenerMantenimientos(filtroEstado, busqueda);

        public DataTable ObtenerKPIs() => _datos.ObtenerKPIs();

        public DataTable ObtenerActivosDisponibles() => _datos.ObtenerActivosParaMantenimiento();

        public ResultadoActivo Crear(Guid activoId, string tipo, string prioridad,
            string descripcion, int? responsableId, int? proveedorId, DateTime? fechaEstimada,
            int colaboradorAccionId)
        {
            var r = new ResultadoActivo();
            if (activoId == Guid.Empty) { r.Mensaje = "Selecciona un activo."; return r; }
            if (string.IsNullOrWhiteSpace(tipo)) { r.Mensaje = "El tipo de mantenimiento es obligatorio."; return r; }
            try
            {
                _datos.CrearMantenimiento(activoId, tipo, prioridad, descripcion,
                    responsableId, proveedorId, fechaEstimada, colaboradorAccionId);
                r.Exitoso = true;
                r.Mensaje = "Ingreso a mantenimiento registrado. El activo cambió a estado 'En Mantenimiento'.";
            }
            catch (Exception ex) { r.Mensaje = $"Error: {ex.Message}"; }
            return r;
        }

        public (ResultadoActivo resultado, int historialId) CambiarEstado(
            int id, Guid activoId, string estadoActual, string estadoNuevo, string comentario,
            decimal? costo, bool garantiaAplicada, string diagnostico, int colaboradorAccionId)
        {
            var r = new ResultadoActivo();
            int historialId = 0;
            try
            {
                historialId = _datos.CambiarEstado(id, activoId, estadoActual, estadoNuevo,
                    comentario, costo, garantiaAplicada, diagnostico, colaboradorAccionId);
                r.Exitoso = true;
                r.Mensaje = "Estado del mantenimiento actualizado correctamente.";
            }
            catch (Exception ex) { r.Mensaje = $"Error: {ex.Message}"; }
            return (r, historialId);
        }

        public DataTable ObtenerHistorial(int id) => _datos.ObtenerHistorial(id);

        public void AgregarFoto(int id, int? historialId, byte[] img, string desc)
            => _datos.AgregarFoto(id, historialId, img, desc);

        public DataTable ObtenerFotos(int id) => _datos.ObtenerFotos(id);
    }
}