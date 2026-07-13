using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class MantenimientoAccesoDatos : ConexionSql
    {
        public DataTable ObtenerMantenimientos(string filtroEstado = null, string busqueda = null)
        {
            string sql = @"
                SELECT M.MantenimientoID, M.ActivoID, AB.EtiquetaActivo,
                       ISNULL(AB.Marca,'—') + ' ' + ISNULL(AB.Modelo,'') AS EquipoNombre,
                       AB.NumeroSerie,
                       M.TipoMantenimiento, M.Prioridad, M.Estado, M.Descripcion, M.DiagnosticoTecnico,
                       ISNULL(C.Nombres + ' ' + C.Apellidos, 'Sin asignar') AS Responsable,
                       ISNULL(P.RazonSocial, '—') AS ProveedorExterno,
                       M.FechaIngreso, M.FechaEstimadaEntrega, M.FechaCierre, M.CostoReparacion,
                       M.GarantiaAplicada,
                       CASE WHEN M.Estado NOT IN ('Cerrado','Cancelado') AND M.FechaEstimadaEntrega < GETDATE()
                            THEN 1 ELSE 0 END AS Vencido
                FROM ITAM.Mantenimientos M
                INNER JOIN ITAM.ActivosBase AB ON M.ActivoID = AB.ActivoID
                LEFT JOIN Core.Colaboradores C ON M.ResponsableColaboradorID = C.ColaboradorID
                LEFT JOIN Core.Proveedores P ON M.ProveedorExternoID = P.ProveedorID
                WHERE (@Estado IS NULL OR M.Estado = @Estado)
                  AND (@Busq IS NULL OR AB.EtiquetaActivo LIKE @Busq OR AB.NumeroSerie LIKE @Busq
                       OR AB.Marca LIKE @Busq OR AB.Modelo LIKE @Busq OR M.Descripcion LIKE @Busq)
                ORDER BY M.FechaIngreso DESC";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Estado", (object)filtroEstado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Busq", string.IsNullOrWhiteSpace(busqueda) ? (object)DBNull.Value : $"%{busqueda}%");
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public DataTable ObtenerKPIs()
        {
            const string sql = "SELECT * FROM ITAM.vw_MantenimientoKPIs";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public int CrearMantenimiento(Guid activoId, string tipo, string prioridad,
            string descripcion, int? responsableId, int? proveedorId, DateTime? fechaEstimada,
            int colaboradorAccionId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                const string sqlIns = @"
                    INSERT INTO ITAM.Mantenimientos
                        (ActivoID, TipoMantenimiento, Prioridad, Descripcion, ResponsableColaboradorID,
                         ProveedorExternoID, FechaEstimadaEntrega)
                    VALUES (@ActivoID, @Tipo, @Prioridad, @Desc, @Resp, @Prov, @FechaEst);
                    SELECT SCOPE_IDENTITY();";
                using var cmd = new SqlCommand(sqlIns, conn, tx);
                cmd.Parameters.AddWithValue("@ActivoID", activoId);
                cmd.Parameters.AddWithValue("@Tipo", tipo);
                cmd.Parameters.AddWithValue("@Prioridad", prioridad);
                cmd.Parameters.AddWithValue("@Desc", (object)descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Resp", (object)responsableId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Prov", (object)proveedorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaEst", (object)fechaEstimada ?? DBNull.Value);
                int nuevoId = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmdEstado = new SqlCommand(
                    "UPDATE ITAM.ActivosBase SET EstadoOperativo='En Mantenimiento' WHERE ActivoID=@A", conn, tx))
                {
                    cmdEstado.Parameters.AddWithValue("@A", activoId);
                    cmdEstado.ExecuteNonQuery();
                }

                using (var cmdHist = new SqlCommand(@"
                    INSERT INTO ITAM.MantenimientoHistorial (MantenimientoID, EstadoAnterior, EstadoNuevo, Comentario, ColaboradorID)
                    VALUES (@Id, NULL, 'Abierto', 'Ingreso a mantenimiento', @Col)", conn, tx))
                {
                    cmdHist.Parameters.AddWithValue("@Id", nuevoId);
                    cmdHist.Parameters.AddWithValue("@Col", colaboradorAccionId <= 0 ? (object)DBNull.Value : colaboradorAccionId);
                    cmdHist.ExecuteNonQuery();
                }

                tx.Commit();
                return nuevoId;
            }
            catch { tx.Rollback(); throw; }
        }

        public int CambiarEstado(int mantenimientoId, Guid activoId, string estadoActual,
            string estadoNuevo, string comentario, decimal? costo, bool garantiaAplicada,
            string diagnostico, int colaboradorAccionId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand(@"
                    UPDATE ITAM.Mantenimientos
                    SET Estado=@Nuevo,
                        CostoReparacion = COALESCE(@Costo, CostoReparacion),
                        GarantiaAplicada = @Garantia,
                        DiagnosticoTecnico = COALESCE(@Diag, DiagnosticoTecnico),
                        FechaCierre = CASE WHEN @Nuevo='Cerrado' THEN GETDATE() ELSE FechaCierre END
                    WHERE MantenimientoID=@Id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Nuevo", estadoNuevo);
                    cmd.Parameters.AddWithValue("@Costo", (object)costo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Garantia", garantiaAplicada);
                    cmd.Parameters.AddWithValue("@Diag", string.IsNullOrWhiteSpace(diagnostico) ? (object)DBNull.Value : diagnostico);
                    cmd.Parameters.AddWithValue("@Id", mantenimientoId);
                    cmd.ExecuteNonQuery();
                }

                int historialId;
                using (var cmdH = new SqlCommand(@"
                    INSERT INTO ITAM.MantenimientoHistorial (MantenimientoID, EstadoAnterior, EstadoNuevo, Comentario, ColaboradorID)
                    VALUES (@Id, @Ant, @Nuevo, @Com, @Col);
                    SELECT SCOPE_IDENTITY();", conn, tx))
                {
                    cmdH.Parameters.AddWithValue("@Id", mantenimientoId);
                    cmdH.Parameters.AddWithValue("@Ant", (object)estadoActual ?? DBNull.Value);
                    cmdH.Parameters.AddWithValue("@Nuevo", estadoNuevo);
                    cmdH.Parameters.AddWithValue("@Com", (object)comentario ?? DBNull.Value);
                    cmdH.Parameters.AddWithValue("@Col", colaboradorAccionId <= 0 ? (object)DBNull.Value : colaboradorAccionId);
                    historialId = Convert.ToInt32(cmdH.ExecuteScalar());
                }

                if (estadoNuevo == "Cerrado" || estadoNuevo == "Cancelado")
                {
                    using var cmdActivo = new SqlCommand(
                        "UPDATE ITAM.ActivosBase SET EstadoOperativo='En Bodega' WHERE ActivoID=@A", conn, tx);
                    cmdActivo.Parameters.AddWithValue("@A", activoId);
                    cmdActivo.ExecuteNonQuery();
                }

                tx.Commit();
                return historialId;
            }
            catch { tx.Rollback(); throw; }
        }

        public DataTable ObtenerHistorial(int mantenimientoId)
        {
            const string sql = @"
                SELECT H.HistorialID, H.EstadoAnterior, H.EstadoNuevo, H.Comentario,
                       H.UsuarioBD, H.FechaCambio,
                       ISNULL(C.Nombres + ' ' + C.Apellidos, H.UsuarioBD) AS AutorCambio
                FROM ITAM.MantenimientoHistorial H
                LEFT JOIN Core.Colaboradores C ON H.ColaboradorID = C.ColaboradorID
                WHERE H.MantenimientoID = @Id
                ORDER BY H.FechaCambio ASC";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", mantenimientoId);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public void AgregarFoto(int mantenimientoId, int? historialId, byte[] imagen, string descripcion)
        {
            const string sql = @"
                INSERT INTO ITAM.MantenimientoFotos (MantenimientoID, HistorialID, Imagen, Descripcion)
                VALUES (@Id, @Hist, @Img, @Desc)";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", mantenimientoId);
            cmd.Parameters.AddWithValue("@Hist", (object)historialId ?? DBNull.Value);
            cmd.Parameters.Add("@Img", SqlDbType.VarBinary, -1).Value = imagen;
            cmd.Parameters.AddWithValue("@Desc", (object)descripcion ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public DataTable ObtenerFotos(int mantenimientoId)
        {
            const string sql = @"
                SELECT FotoID, HistorialID, Imagen, Descripcion, FechaCarga
                FROM ITAM.MantenimientoFotos WHERE MantenimientoID=@Id ORDER BY FechaCarga DESC";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", mantenimientoId);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public DataTable ObtenerActivosParaMantenimiento()
        {
            const string sql = @"
                SELECT ActivoID, EtiquetaActivo, Marca, Modelo, NumeroSerie
                FROM ITAM.ActivosBase
                WHERE EstadoOperativo IN ('En Bodega','Asignado')
                ORDER BY Marca, Modelo";
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }
    }
}