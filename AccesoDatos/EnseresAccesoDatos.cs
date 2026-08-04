using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class EnseresAccesoDatos : ConexionSql
    {
        public DataTable ObtenerEnseres(string busqueda = null)
        {
            const string sql = @"
                SELECT 
                    E.EnserID, E.Nombre, E.CategoriaEnser, E.UbicacionID,
                    ISNULL(U.NombreNomenclatura, '—') AS Ubicacion,
                    E.Cantidad, E.EstadoFisico, E.NumeroInventario,
                    E.FechaAdquisicion, E.Costo, E.Observaciones, E.FechaRegistro
                FROM ITAM.Enseres E
                LEFT JOIN Core.Ubicaciones U ON E.UbicacionID = U.UbicacionID
                WHERE E.Activo = 1
                  AND (@Busq IS NULL 
                       OR E.Nombre LIKE @Busq 
                       OR E.CategoriaEnser LIKE @Busq 
                       OR E.NumeroInventario LIKE @Busq)
                ORDER BY E.FechaRegistro DESC";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Busq", string.IsNullOrWhiteSpace(busqueda) ? (object)DBNull.Value : $"%{busqueda}%");
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public int InsertarEnser(
            string nombre, string categoria, int? ubicacionId, int cantidad,
            string estadoFisico, string numeroInventario, DateTime? fechaAdquisicion,
            decimal? costo, string observaciones)
        {
            const string sql = @"
                INSERT INTO ITAM.Enseres
                    (Nombre, CategoriaEnser, UbicacionID, Cantidad, EstadoFisico,
                     NumeroInventario, FechaAdquisicion, Costo, Observaciones)
                OUTPUT INSERTED.EnserID
                VALUES
                    (@Nombre, @Categoria, @UbicacionID, @Cantidad, @Estado,
                     @NumInv, @FechaAdq, @Costo, @Obs)";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@Categoria", (object)categoria ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UbicacionID", (object)ubicacionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Estado", estadoFisico);
            cmd.Parameters.AddWithValue("@NumInv", string.IsNullOrWhiteSpace(numeroInventario) ? (object)DBNull.Value : numeroInventario);
            cmd.Parameters.AddWithValue("@FechaAdq", (object)fechaAdquisicion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Costo", (object)costo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Obs", string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones);

            var result = cmd.ExecuteScalar();
            return result != null ? 1 : 0;
        }

        public bool ActualizarEnser(
            Guid enserId, string nombre, string categoria, int? ubicacionId, int cantidad,
            string estadoFisico, string numeroInventario, DateTime? fechaAdquisicion,
            decimal? costo, string observaciones)
        {
            const string sql = @"
                UPDATE ITAM.Enseres
                SET Nombre = @Nombre, CategoriaEnser = @Categoria, UbicacionID = @UbicacionID,
                    Cantidad = @Cantidad, EstadoFisico = @Estado, NumeroInventario = @NumInv,
                    FechaAdquisicion = @FechaAdq, Costo = @Costo, Observaciones = @Obs
                WHERE EnserID = @EnserID";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EnserID", enserId);
            cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@Categoria", (object)categoria ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UbicacionID", (object)ubicacionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Estado", estadoFisico);
            cmd.Parameters.AddWithValue("@NumInv", string.IsNullOrWhiteSpace(numeroInventario) ? (object)DBNull.Value : numeroInventario);
            cmd.Parameters.AddWithValue("@FechaAdq", (object)fechaAdquisicion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Costo", (object)costo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Obs", string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool EliminarEnser(Guid enserId)
        {
            // Baja lógica, igual que ActivosBase
            const string sql = "UPDATE ITAM.Enseres SET Activo = 0 WHERE EnserID = @EnserID";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EnserID", enserId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public DataTable ObtenerUbicaciones()
        {
            const string sql = "SELECT UbicacionID, NombreNomenclatura FROM Core.Ubicaciones ORDER BY NombreNomenclatura";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public int ObtenerTotalEnseres()
        {
            const string sql = "SELECT ISNULL(SUM(Cantidad),0) FROM ITAM.Enseres WHERE Activo = 1";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            return (int)cmd.ExecuteScalar();
        }
    }
}