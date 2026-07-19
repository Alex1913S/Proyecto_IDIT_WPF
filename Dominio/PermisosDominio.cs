using System.Collections.Generic;
using AccesoDatos;

namespace Dominio
{
    public class PermisosDominio
    {
        private readonly PermisosAccesoDatos _datos = new();

        public bool ExistenPermisos(string rol) => _datos.ExistenPermisosParaRol(rol);

        public Dictionary<string, bool> Obtener(string rol) => _datos.ObtenerPermisosPorRol(rol);

        public bool Guardar(string rol, Dictionary<string, bool> permisos)
            => _datos.GuardarPermisos(rol, permisos);
    }
}