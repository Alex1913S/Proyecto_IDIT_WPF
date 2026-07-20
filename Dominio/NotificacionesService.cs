// Dominio/NotificacionService.cs
using AccesoDatos;

namespace Dominio
{
    public static class NotificacionesService
    {
        private static readonly NotificacionAccesoDatos _datos = new();

        // Se configuran una vez al hacer login (ver Dashboard_Loaded)
        public static int ColaboradorActualId { get; set; }
        public static bool EsAdministrador { get; set; }

        public static class Modulos
        {
            public const string Inventario = "Inventario";
            public const string Colaboradores = "Colaboradores";
            public const string Contrasenas = "Contraseñas";
            public const string Asignaciones = "Asignaciones";
            public const string Mantenimiento = "Mantenimiento";
        }

        public static class Acciones
        {
            public const string Creacion = "Creación";
            public const string Edicion = "Edición";
            public const string Eliminacion = "Eliminación";
        }

        public static void Notificar(string modulo, string accion, string descripcion, string entidadId = null)
        {
            try
            {
                _datos.Insertar(modulo, accion, descripcion, entidadId, ColaboradorActualId);
            }
            catch { /* nunca debe tumbar el flujo principal por un fallo de notificación */ }
        }

        public static System.Data.DataTable Obtener(int top = 60)
            => _datos.Obtener(ColaboradorActualId, EsAdministrador, top);

        public static int ContarNoLeidas()
            => _datos.ContarNoLeidas(ColaboradorActualId, EsAdministrador);

        public static void MarcarTodasLeidas()
            => _datos.MarcarTodasLeidas(ColaboradorActualId, EsAdministrador);
    }
}