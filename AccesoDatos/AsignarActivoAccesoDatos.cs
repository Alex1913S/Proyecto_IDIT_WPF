using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace AccesoDatos
{
    public class AsignarActivoAccesoDatos : ConexionSql
    {

        public DataTable ObtenerActivosDisponibles()
        {
            const string sql = @"
                SELECT
                    AB.ActivoID,
                    AB.EtiquetaActivo,
                    CAT.Nombre          AS Categoria,
                    ISNULL(AB.Marca,  '—') AS Marca,
                    ISNULL(AB.Modelo, '—') AS Modelo,
                    ISNULL(AB.NumeroSerie, 'S/N') AS NumeroSerie,
                    ISNULL(UBI.NombreNomenclatura, '—') AS Sede
                FROM ITAM.ActivosBase      AB
                INNER JOIN ITAM.CategoriasActivo CAT ON AB.CategoriaID = CAT.CategoriaID
                LEFT  JOIN Core.Ubicaciones      UBI ON AB.UbicacionID = UBI.UbicacionID
                WHERE AB.EstadoOperativo = 'En Bodega'
                ORDER BY CAT.Nombre, AB.EtiquetaActivo";

            return Ejecutar(sql);
        }

        public DataTable BuscarActivosDisponibles(string termino)
        {
            const string sql = @"
                SELECT
                    AB.ActivoID,
                    AB.EtiquetaActivo,
                    CAT.Nombre          AS Categoria,
                    ISNULL(AB.Marca,  '—') AS Marca,
                    ISNULL(AB.Modelo, '—') AS Modelo,
                    ISNULL(AB.NumeroSerie, 'S/N') AS NumeroSerie,
                    ISNULL(UBI.NombreNomenclatura, '—') AS Sede
                FROM ITAM.ActivosBase      AB
                INNER JOIN ITAM.CategoriasActivo CAT ON AB.CategoriaID = CAT.CategoriaID
                LEFT  JOIN Core.Ubicaciones      UBI ON AB.UbicacionID = UBI.UbicacionID
                WHERE AB.EstadoOperativo = 'En Bodega'
                  AND (AB.EtiquetaActivo LIKE @t
                    OR CAT.Nombre        LIKE @t
                    OR AB.Marca          LIKE @t
                    OR AB.Modelo         LIKE @t
                    OR AB.NumeroSerie    LIKE @t)
                ORDER BY CAT.Nombre, AB.EtiquetaActivo";

            return Ejecutar(sql, cmd => cmd.Parameters.AddWithValue("@t", $"%{termino}%"));
        }


        public DataTable ObtenerColaboradores()
        {
            const string sql = @"
                SELECT
                    C.ColaboradorID,
                    C.Nombres + ' ' + C.Apellidos   AS NombreCompleto,
                    C.DocumentoIdentidad,
                    C.CorreoCorporativo,
                    D.Nombre                         AS Departamento,
                    U.NombreNomenclatura             AS Sede
                FROM Core.Colaboradores  C
                INNER JOIN Core.Departamentos D ON C.DepartamentoID = D.DepartamentoID
                LEFT  JOIN Core.Ubicaciones   U ON C.UbicacionID    = U.UbicacionID
                WHERE C.Estado = 1
                ORDER BY C.Apellidos, C.Nombres";

            return Ejecutar(sql);
        }

        public DataTable BuscarColaboradores(string termino)
        {
            const string sql = @"
                SELECT
                    C.ColaboradorID,
                    C.Nombres + ' ' + C.Apellidos   AS NombreCompleto,
                    C.DocumentoIdentidad,
                    C.CorreoCorporativo,
                    D.Nombre                         AS Departamento,
                    U.NombreNomenclatura             AS Sede
                FROM Core.Colaboradores  C
                INNER JOIN Core.Departamentos D ON C.DepartamentoID = D.DepartamentoID
                LEFT  JOIN Core.Ubicaciones   U ON C.UbicacionID    = U.UbicacionID
                WHERE C.Estado = 1
                  AND (C.Nombres            LIKE @t
                    OR C.Apellidos          LIKE @t
                    OR C.DocumentoIdentidad LIKE @t
                    OR C.CorreoCorporativo  LIKE @t
                    OR D.Nombre             LIKE @t)
                ORDER BY C.Apellidos, C.Nombres";

            return Ejecutar(sql, cmd => cmd.Parameters.AddWithValue("@t", $"%{termino}%"));
        }


        // REGISTRAR ASIGNACIÓN

        public int RegistrarAsignacion(
            Guid activoId,
            int colaboradorId,
            DateTime fechaAsignacion,
            string observaciones)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Insertar en ITAM.Asignaciones
                const string sqlInsert = @"
                    INSERT INTO ITAM.Asignaciones
                           (ActivoID, ColaboradorID, FechaAsignacion, Observaciones)
                    VALUES (@activoId, @colaboradorId, @fecha, @obs);
                    SELECT SCOPE_IDENTITY();";

                int nuevoId;
                using (var cmd = new SqlCommand(sqlInsert, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@activoId", activoId);
                    cmd.Parameters.AddWithValue("@colaboradorId", colaboradorId);
                    cmd.Parameters.AddWithValue("@fecha", fechaAsignacion);
                    cmd.Parameters.AddWithValue("@obs",
                        string.IsNullOrWhiteSpace(observaciones) ? DBNull.Value : observaciones);

                    nuevoId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Actualizar estado del activo
                const string sqlUpdate = @"
                    UPDATE ITAM.ActivosBase
                       SET EstadoOperativo = 'Asignado'
                     WHERE ActivoID = @activoId";

                using (var cmd = new SqlCommand(sqlUpdate, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@activoId", activoId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return nuevoId;
            }
            catch
            {
                tx.Rollback();
                return -1;
            }
        }


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

        public DataTable ObtenerAsignacionesActivas()
        {
            const string sql = @"
        SELECT 
            A.AsignacionID,
            C.Nombres + ' ' + C.Apellidos AS NombreColaborador,
            ISNULL(AB.EtiquetaActivo, AB.Marca + ' ' + AB.Modelo) AS NombreActivo,
            A.FechaAsignacion,
            AB.EstadoOperativo AS Estado,
            ISNULL(A.Observaciones, '—') AS Observaciones,
            A.ActivoID,
            A.ColaboradorID
        FROM ITAM.Asignaciones A
        INNER JOIN Core.Colaboradores C ON A.ColaboradorID = C.ColaboradorID
        INNER JOIN ITAM.ActivosBase AB ON A.ActivoID = AB.ActivoID
        ORDER BY A.FechaAsignacion DESC";

            return Ejecutar(sql);
        }
    }
}
