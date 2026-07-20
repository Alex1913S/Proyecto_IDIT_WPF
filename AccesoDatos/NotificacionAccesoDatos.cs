// AccesoDatos/NotificacionAccesoDatos.cs
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class NotificacionAccesoDatos : ConexionSql
    {
        public void Insertar(string modulo, string accion, string descripcion,
                              string entidadId, int colaboradorAccionId)
        {
            const string sql = @"
                INSERT INTO Seguridad.Notificaciones
                    (Modulo, Accion, Descripcion, EntidadID, ColaboradorAccionID)
                VALUES (@Modulo, @Accion, @Desc, @EntidadID, @ColabID)";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Modulo", modulo);
            cmd.Parameters.AddWithValue("@Accion", accion);
            cmd.Parameters.AddWithValue("@Desc", descripcion);
            cmd.Parameters.AddWithValue("@EntidadID", (object)entidadId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ColabID", colaboradorAccionId);
            cmd.ExecuteNonQuery();
        }

        // esAdmin = ve todas; si no, solo las suyas
        public DataTable Obtener(int colaboradorId, bool esAdmin, int top = 60)
        {
            string sql = $@"
                SELECT TOP {top}
                    N.NotificacionID, N.Modulo, N.Accion, N.Descripcion,
                    N.FechaCreacion, N.Leida,
                    C.Nombres + ' ' + C.Apellidos AS Autor
                FROM Seguridad.Notificaciones N
                LEFT JOIN Core.Colaboradores C ON N.ColaboradorAccionID = C.ColaboradorID
                WHERE (@EsAdmin = 1 OR N.ColaboradorAccionID = @ColabID)
                ORDER BY N.FechaCreacion DESC";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EsAdmin", esAdmin);
            cmd.Parameters.AddWithValue("@ColabID", colaboradorId);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public int ContarNoLeidas(int colaboradorId, bool esAdmin)
        {
            const string sql = @"
                SELECT COUNT(*) FROM Seguridad.Notificaciones
                WHERE Leida = 0 AND (@EsAdmin = 1 OR ColaboradorAccionID = @ColabID)";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EsAdmin", esAdmin);
            cmd.Parameters.AddWithValue("@ColabID", colaboradorId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void MarcarTodasLeidas(int colaboradorId, bool esAdmin)
        {
            const string sql = @"
                UPDATE Seguridad.Notificaciones SET Leida = 1
                WHERE Leida = 0 AND (@EsAdmin = 1 OR ColaboradorAccionID = @ColabID)";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EsAdmin", esAdmin);
            cmd.Parameters.AddWithValue("@ColabID", colaboradorId);
            cmd.ExecuteNonQuery();
        }
    }
}