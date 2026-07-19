using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class PermisosAccesoDatos : ConexionSql
    {
        public bool ExistenPermisosParaRol(string rol)
        {
            const string sql = "SELECT COUNT(*) FROM Seguridad.PermisosRol WHERE RolNombre = @Rol";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Rol", rol);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public Dictionary<string, bool> ObtenerPermisosPorRol(string rol)
        {
            var resultado = new Dictionary<string, bool>();
            const string sql = "SELECT PermisoID, Activo FROM Seguridad.PermisosRol WHERE RolNombre = @Rol";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Rol", rol);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                resultado[reader.GetString(0)] = reader.GetBoolean(1);

            return resultado;
        }

        public bool GuardarPermisos(string rol, Dictionary<string, bool> permisos)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using (var cmdDel = new SqlCommand(
                    "DELETE FROM Seguridad.PermisosRol WHERE RolNombre = @Rol", conn, tx))
                {
                    cmdDel.Parameters.AddWithValue("@Rol", rol);
                    cmdDel.ExecuteNonQuery();
                }

                const string sqlIns = @"INSERT INTO Seguridad.PermisosRol (RolNombre, PermisoID, Activo)
                                         VALUES (@Rol, @PermisoID, @Activo)";

                foreach (var kv in permisos)
                {
                    using var cmdIns = new SqlCommand(sqlIns, conn, tx);
                    cmdIns.Parameters.AddWithValue("@Rol", rol);
                    cmdIns.Parameters.AddWithValue("@PermisoID", kv.Key);
                    cmdIns.Parameters.AddWithValue("@Activo", kv.Value);
                    cmdIns.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}