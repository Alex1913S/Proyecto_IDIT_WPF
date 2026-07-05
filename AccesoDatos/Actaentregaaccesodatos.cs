using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    /// <summary>
    /// Acceso a datos exclusivo para la generación de Actas de Entrega de Equipos.
    /// Reutiliza ConexionSql igual que el resto del módulo SGSI.
    /// </summary>
    public class ActaEntregaAccesoDatos : ConexionSql
    {
        // ─────────────────────────────────────────────────────────────
        // Colaboradores activos, para el selector del UserControl
        // ─────────────────────────────────────────────────────────────
        public DataTable ObtenerColaboradoresActivos()
        {
            const string sql = @"
                SELECT ColaboradorID,
                       DocumentoIdentidad,
                       (Nombres + ' ' + Apellidos) AS NombreCompleto,
                       Cargo
                FROM Core.Colaboradores
                WHERE Estado = 1
                ORDER BY Apellidos, Nombres";

            return Ejecutar(sql);
        }

        // ─────────────────────────────────────────────────────────────
        // Datos puntuales del colaborador (nombre completo, cédula, cargo)
        // ─────────────────────────────────────────────────────────────
        public DataRow? ObtenerColaborador(int colaboradorId)
        {
            const string sql = @"
                SELECT ColaboradorID, DocumentoIdentidad, Nombres, Apellidos, Cargo
                FROM Core.Colaboradores
                WHERE ColaboradorID = @ColaboradorID";

            var dt = Ejecutar(sql, cmd => cmd.Parameters.AddWithValue("@ColaboradorID", colaboradorId));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // ─────────────────────────────────────────────────────────────
        // Activos actualmente ASIGNADOS a ese colaborador (los que van en el acta)
        // ─────────────────────────────────────────────────────────────
        public DataTable ObtenerActivosAsignados(int colaboradorId)
        {
            const string sql = @"
                SELECT
                    AB.ActivoID,
                    CAT.Nombre                       AS Categoria,
                    ISNULL(AB.Marca,  '—')           AS Marca,
                    ISNULL(AB.Modelo, '—')            AS Modelo,
                    ISNULL(AB.NumeroSerie, 'S/N')      AS NumeroSerie,
                    A.FechaAsignacion
                FROM ITAM.Asignaciones A
                INNER JOIN ITAM.ActivosBase      AB  ON A.ActivoID = AB.ActivoID
                INNER JOIN ITAM.CategoriasActivo CAT ON AB.CategoriaID = CAT.CategoriaID
                WHERE A.ColaboradorID = @ColaboradorID
                  AND AB.EstadoOperativo = 'Asignado'
                ORDER BY A.FechaAsignacion DESC";

            return Ejecutar(sql, cmd => cmd.Parameters.AddWithValue("@ColaboradorID", colaboradorId));
        }

        // ─────────────────────────────────────────────────────────────
        // Helper privado (mismo patrón usado en el resto del proyecto)
        // ─────────────────────────────────────────────────────────────
        private DataTable Ejecutar(string sql, Action<SqlCommand>? parametros = null)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            parametros?.Invoke(cmd);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }
    }
}