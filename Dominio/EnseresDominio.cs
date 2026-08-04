using System;
using System.Data;
using AccesoDatos;

namespace Dominio
{
    public class ResultadoEnser
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class EnseresDominio
    {
        private readonly EnseresAccesoDatos _datos = new();

        public DataTable Listar(string busqueda = null) => _datos.ObtenerEnseres(busqueda);
        public DataTable ListarUbicaciones() => _datos.ObtenerUbicaciones();
        public int ObtenerTotal() => _datos.ObtenerTotalEnseres();

        public ResultadoEnser Crear(
            string nombre, string categoria, int? ubicacionId, int cantidad,
            string estadoFisico, string numeroInventario, DateTime? fechaAdquisicion,
            decimal? costo, string observaciones)
        {
            var r = new ResultadoEnser();

            if (string.IsNullOrWhiteSpace(nombre))
            { r.Mensaje = "El nombre del enser es obligatorio."; return r; }

            if (cantidad <= 0)
            { r.Mensaje = "La cantidad debe ser mayor a cero."; return r; }

            try
            {
                bool ok = _datos.InsertarEnser(nombre.Trim(), categoria, ubicacionId, cantidad,
                    estadoFisico, numeroInventario, fechaAdquisicion, costo, observaciones) > 0;

                r.Exitoso = ok;
                r.Mensaje = ok ? "Enser registrado correctamente." : "No se pudo registrar el enser.";

                if (ok)
                    NotificacionesService.Notificar("Enseres", NotificacionesService.Acciones.Creacion,
                        $"Se registró el enser '{nombre}'.", null);
            }
            catch (Exception ex)
            {
                r.Mensaje = $"Error: {ex.Message}";
            }
            return r;
        }

        public ResultadoEnser Editar(
            Guid enserId, string nombre, string categoria, int? ubicacionId, int cantidad,
            string estadoFisico, string numeroInventario, DateTime? fechaAdquisicion,
            decimal? costo, string observaciones)
        {
            var r = new ResultadoEnser();

            if (enserId == Guid.Empty)
            { r.Mensaje = "Enser no válido."; return r; }

            if (string.IsNullOrWhiteSpace(nombre))
            { r.Mensaje = "El nombre del enser es obligatorio."; return r; }

            try
            {
                bool ok = _datos.ActualizarEnser(enserId, nombre.Trim(), categoria, ubicacionId, cantidad,
                    estadoFisico, numeroInventario, fechaAdquisicion, costo, observaciones);

                r.Exitoso = ok;
                r.Mensaje = ok ? "Enser actualizado correctamente." : "No se pudo actualizar el enser.";

                if (ok)
                    NotificacionesService.Notificar("Enseres", NotificacionesService.Acciones.Edicion,
                        $"Se editó el enser '{nombre}'.", enserId.ToString());
            }
            catch (Exception ex)
            {
                r.Mensaje = $"Error: {ex.Message}";
            }
            return r;
        }

        public ResultadoEnser Eliminar(Guid enserId, string nombre)
        {
            var r = new ResultadoEnser();
            try
            {
                bool ok = _datos.EliminarEnser(enserId);
                r.Exitoso = ok;
                r.Mensaje = ok ? "Enser dado de baja correctamente." : "No se pudo eliminar el enser.";

                if (ok)
                    NotificacionesService.Notificar("Enseres", NotificacionesService.Acciones.Eliminacion,
                        $"Se dio de baja el enser '{nombre}'.", enserId.ToString());
            }
            catch (Exception ex)
            {
                r.Mensaje = $"Error: {ex.Message}";
            }
            return r;
        }
    }
}