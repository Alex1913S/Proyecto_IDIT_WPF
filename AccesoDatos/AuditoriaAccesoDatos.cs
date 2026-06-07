using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace AccesoDatos
{
    public class AuditoriaAccesoDatos : ConexionSql
    {
        // 🔍 Obtener los logs de activos exclusivamente por rango de fecha
        public DataTable ObtenerLogs(DateTime desde, DateTime hasta)
        {
            var dt = new DataTable();

            // Forzamos el rango para incluir el día completo (00:00:00 a 23:59:59)
            DateTime fechaInicio = desde.Date;
            DateTime fechaFin = hasta.Date.AddDays(1).AddTicks(-1);

            const string sql = @"
                SELECT LogID, TablaAfectada, RegistroID, Accion, UsuarioBD, FechaAccion, DetalleAnterior, DetalleNuevo 
                FROM SGSI.LogAuditoria
                WHERE FechaAccion BETWEEN @Desde AND @Hasta
                ORDER BY FechaAccion DESC";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Desde", fechaInicio);
                    cmd.Parameters.AddWithValue("@Hasta", fechaFin);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }
    }
}
