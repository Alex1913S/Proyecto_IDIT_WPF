using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class ActivosAccesoDatos : ConexionSql
    {
        private static object NullSiVacio(string valor) =>string.IsNullOrWhiteSpace(valor) ? DBNull.Value : (object)valor;
        public bool InsertarActivo(
            // ActivosBase
            int categoriaId, int ubicacionId, string etiquetaActivo,
            string marca, string modelo, string numeroSerie, int? proveedorId,
            DateTime? fechaAdquis, decimal? costo, string estadoOperativo,

            // EspecificacionesHardware
            string procesador, string memoriaRAM, string almac1, string almac2,
            string tarjetaGrafica, string sistemaOperativo, string mac, string ip,
            string resolucion,
            byte[] facturaCompra = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // GUID generado en C#
                        Guid activoId = Guid.NewGuid();

                        // INSERT ActivosBase (Se agrega EtiquetaActivo)
                        string sqlBase = @"
                            INSERT INTO ITAM.ActivosBase
                                (ActivoID, CategoriaID, UbicacionID,
                                 Marca, Modelo, NumeroSerie, ProveedorID,
                                 FechaAdquisicion, Costo, EstadoOperativo,
                                 FacturaCompra)
                            VALUES
                                (@ActivoID, @CategoriaID, @UbicacionID,
                                 @Marca, @Modelo, @NumeroSerie, @ProveedorID,
                                 @FechaAdquisicion, @Costo, @EstadoOperativo,
                                 @FacturaCompra)";

                        var cmdBase = new SqlCommand(sqlBase, conn, transaction);
                        cmdBase.Parameters.Add("@ActivoID", SqlDbType.UniqueIdentifier).Value = activoId;
                        cmdBase.Parameters.Add("@CategoriaID", SqlDbType.Int).Value = categoriaId;
                        cmdBase.Parameters.Add("@UbicacionID", SqlDbType.Int).Value = ubicacionId;
                        cmdBase.Parameters.Add("@Marca", SqlDbType.NVarChar).Value = marca ?? (object)DBNull.Value;
                        cmdBase.Parameters.Add("@Modelo", SqlDbType.NVarChar).Value = modelo ?? (object)DBNull.Value;
                        cmdBase.Parameters.Add("@NumeroSerie", SqlDbType.VarChar).Value = numeroSerie ?? (object)DBNull.Value;
                        cmdBase.Parameters.Add("@ProveedorID", SqlDbType.Int).Value = proveedorId.HasValue ? proveedorId.Value : (object)DBNull.Value;
                        cmdBase.Parameters.Add("@FechaAdquisicion", SqlDbType.Date).Value = fechaAdquis.HasValue ? fechaAdquis.Value : (object)DBNull.Value;
                        cmdBase.Parameters.Add("@Costo", SqlDbType.Decimal).Value = costo.HasValue ? costo.Value : (object)DBNull.Value;
                        cmdBase.Parameters.Add("@EstadoOperativo", SqlDbType.VarChar).Value = estadoOperativo ?? (object)DBNull.Value;
                        cmdBase.Parameters.Add("@FacturaCompra", SqlDbType.VarBinary).Value = (object)facturaCompra ?? DBNull.Value;


                        cmdBase.ExecuteNonQuery();

                        // INSERT EspecificacionesHardware con el mismo GUID
                        string sqlEspec = @"
                            INSERT INTO ITAM.EspecificacionesHardware
                                (ActivoID, Procesador, MemoriaRAM,
                                 Almacenamiento1, Almacenamiento2,
                                 TarjetaGrafica, SistemaOperativo,
                                 DireccionMAC, DireccionIP_Estatica,
                                 ResolucionPantalla)
                            VALUES
                                (@ActivoID, @Procesador, @MemoriaRAM,
                                 @Almacenamiento1, @Almacenamiento2,
                                 @TarjetaGrafica, @SistemaOperativo,
                                 @MAC, @IP, @Resolucion)";

                        var cmdEspec = new SqlCommand(sqlEspec, conn, transaction);
                        cmdEspec.Parameters.Add("@ActivoID", SqlDbType.UniqueIdentifier).Value = activoId;
                        cmdEspec.Parameters.Add("@Procesador", SqlDbType.NVarChar).Value = procesador ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@MemoriaRAM", SqlDbType.VarChar).Value = memoriaRAM ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@Almacenamiento1", SqlDbType.NVarChar).Value = almac1 ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@Almacenamiento2", SqlDbType.NVarChar).Value = almac2 ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@TarjetaGrafica", SqlDbType.NVarChar).Value = tarjetaGrafica ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@SistemaOperativo", SqlDbType.NVarChar).Value = sistemaOperativo ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@MAC", SqlDbType.VarChar).Value = mac ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@IP", SqlDbType.VarChar).Value = ip ?? (object)DBNull.Value;
                        cmdEspec.Parameters.Add("@Resolucion", SqlDbType.VarChar).Value = resolucion ?? (object)DBNull.Value;

                        cmdEspec.ExecuteNonQuery();

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool ActualizarActivo(
            Guid activoId, int categoriaId, int ubicacionId, string etiquetaActivo, string marca, string modelo,
            string numeroSerie, int? proveedorId, DateTime? fechaAdquis, decimal? costo, string estadoOperativo,
            string procesador, string memoriaRAM, string almac1, string almac2,
            string tarjetaGrafica, string sistemaOperativo, string mac, string ip, string resolucion, byte[] facturaCompra = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Actualizar tabla maestra: ITAM.ActivosBase (Se agrega EtiquetaActivo al SET)
                        string sqlBase = @"
                            UPDATE ITAM.ActivosBase
                            SET CategoriaID = @CategoriaID, UbicacionID = @UbicacionID, 
                                EtiquetaActivo = @EtiquetaActivo, Marca = @Marca, Modelo = @Modelo, 
                                NumeroSerie = @NumeroSerie, ProveedorID = @ProveedorID, 
                                FechaAdquisicion = @FechaAdquisicion, Costo = @Costo, EstadoOperativo = @EstadoOperativo,
                                FacturaCompra = COALESCE(@FacturaCompra, FacturaCompra)
                            WHERE ActivoID = @ActivoID";

                        using (var cmd = new SqlCommand(sqlBase, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ActivoID", activoId);
                            cmd.Parameters.AddWithValue("@CategoriaID", categoriaId);
                            cmd.Parameters.AddWithValue("@UbicacionID", ubicacionId);
                            cmd.Parameters.AddWithValue("@EtiquetaActivo", etiquetaActivo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Marca", marca ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Modelo", modelo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NumeroSerie", numeroSerie ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProveedorID", proveedorId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FechaAdquisicion", fechaAdquis ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Costo", costo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EstadoOperativo", estadoOperativo);
                            cmd.Parameters.AddWithValue("@FacturaCompra", facturaCompra != null ? (object)facturaCompra : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Actualizar tabla detalle: ITAM.EspecificacionesHardware
                        string sqlHardware = @"
                            UPDATE ITAM.EspecificacionesHardware
                            SET Procesador = @Procesador, MemoriaRAM = @MemoriaRAM, 
                                Almacenamiento1 = @Almacenamiento1, Almacenamiento2 = @Almacenamiento2,
                                TarjetaGrafica = @TarjetaGrafica, SistemaOperativo = @SistemaOperativo, 
                                DireccionMAC = @MAC, DireccionIP_Estatica = @IP, ResolucionPantalla = @Resolucion
                            WHERE ActivoID = @ActivoID";

                        using (var cmdEspec = new SqlCommand(sqlHardware, conn, transaction))
                        {
                            cmdEspec.Parameters.AddWithValue("@ActivoID", activoId);
                            cmdEspec.Parameters.AddWithValue("@Procesador", procesador ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@MemoriaRAM", memoriaRAM ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@Almacenamiento1", almac1 ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@Almacenamiento2", almac2 ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@TarjetaGrafica", tarjetaGrafica ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@SistemaOperativo", sistemaOperativo ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@MAC", mac ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@IP", ip ?? (object)DBNull.Value);
                            cmdEspec.Parameters.AddWithValue("@Resolucion", resolucion ?? (object)DBNull.Value);
                            cmdEspec.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ELIMINACIÓN LÓGICA
        public bool DarDeBajaActivo(Guid activoId)
        {
            const string sql = "UPDATE ITAM.ActivosBase SET EstadoOperativo = 'De Baja' WHERE ActivoID = @ActivoID";
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ActivoID", activoId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public DataTable ObtenerTodosLosActivos()
        {
            const string sql = @"
                SELECT 
                    AB.ActivoID, 
                    AB.EtiquetaActivo, 
                    AB.CategoriaID, 
                    CAT.Nombre AS Categoria,
                    AB.UbicacionID, 
                    UBI.NombreNomenclatura AS Sede, 
                    AB.Marca, 
                    AB.Modelo, 
                    AB.NumeroSerie, 
                    AB.FechaAdquisicion, 
                    AB.Costo, 
                    AB.EstadoOperativo,
                    EH.Procesador,
                    EH.MemoriaRAM,
                    EH.Almacenamiento1,
                    EH.Almacenamiento2,
                    EH.TarjetaGrafica,
                    EH.SistemaOperativo,
                    EH.DireccionMAC,
                    EH.DireccionIP_Estatica,
                    EH.ResolucionPantalla,
                    AB.FacturaCompra
                FROM ITAM.ActivosBase AB
                INNER JOIN ITAM.CategoriasActivo CAT ON AB.CategoriaID = CAT.CategoriaID
                LEFT JOIN Core.Ubicaciones UBI ON AB.UbicacionID = UBI.UbicacionID
                LEFT JOIN ITAM.EspecificacionesHardware EH ON AB.ActivoID = EH.ActivoID
                WHERE AB.EstadoOperativo <> 'De Baja'
                ORDER BY AB.FechaRegistro DESC";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    var dt = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    return dt;
                }
            }
        }

        public DataTable ObtenerCategorias()
        {
            const string sql = "SELECT CategoriaID, Nombre FROM ITAM.CategoriasActivo ORDER BY Nombre";
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    var dt = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }
                    return dt;
                }
            }
        }

        public DataTable ObtenerUbicaciones()
        {
            const string sql = "SELECT UbicacionID, NombreNomenclatura FROM Core.Ubicaciones ORDER BY NombreNomenclatura";
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    var dt = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd)) { adapter.Fill(dt); }
                    return dt;
                }
            }
        }

        public decimal ObtenerValorTotalInventario()
        {
            const string sql = @"
        SELECT ISNULL(SUM(Costo), 0)
        FROM ITAM.ActivosBase
        WHERE EstadoOperativo <> 'De Baja'";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    return (decimal)cmd.ExecuteScalar();
                }
            }
        }

        public int ObtenerTotalActivos()
        {
            const string sql = @"
        SELECT COUNT(*)
        FROM ITAM.ActivosBase
        WHERE EstadoOperativo <> 'De Baja'";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                    return (int)cmd.ExecuteScalar();
            }
        }

        public decimal ObtenerPorcentajeGarantiasVigentes()
        {
            const string sql = @"
        SELECT 
            COUNT(*) AS Total,
            SUM(CASE WHEN DATEADD(YEAR, 1, FechaAdquisicion) >= CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS Vigentes
        FROM ITAM.ActivosBase
        WHERE EstadoOperativo <> 'De Baja'
          AND FechaAdquisicion IS NOT NULL";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int total = reader.GetInt32(0);
                        int vigentes = reader.GetInt32(1);
                        return total == 0 ? 0 : Math.Round((decimal)vigentes / total * 100, 1);
                    }
                    return 0;
                }
            }
        }

        public DataTable ObtenerTop5CategoriasPorCantidad()
        {
            const string sql = @"
        SELECT TOP 6
            CAT.Nombre AS Categoria,
            COUNT(*) AS Cantidad
        FROM ITAM.ActivosBase AB
        INNER JOIN ITAM.CategoriasActivo CAT ON AB.CategoriaID = CAT.CategoriaID
        WHERE AB.EstadoOperativo <> 'De Baja'
        GROUP BY CAT.Nombre
        ORDER BY COUNT(*) DESC";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    var dt = new DataTable();
                    using (var adapter = new SqlDataAdapter(cmd))
                        adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public (int ColaboradorID, string Nombres, string Apellidos, string Departamento, string Rol, string Cargo, byte[] Foto) ObtenerDatosUsuario(string correo)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT c.ColaboradorID, c.Nombres, c.Apellidos, 
                                d.Nombre       AS Departamento,
                                p.NombrePerfil AS Rol,
                                c.Cargo,           
                                c.Foto                
                         FROM Core.Colaboradores c
                         INNER JOIN Core.Departamentos d ON c.DepartamentoID = d.DepartamentoID
                         INNER JOIN Seguridad.Perfiles p ON c.PerfilID = p.PerfilID
                         WHERE c.CorreoCorporativo = @correo";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add("@correo", SqlDbType.NVarChar).Value = correo;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        byte[] foto = reader["Foto"] == DBNull.Value ? null : (byte[])reader["Foto"];
                        return (
                            Convert.ToInt32(reader["ColaboradorID"]),
                            reader["Nombres"].ToString()!,
                            reader["Apellidos"].ToString()!,
                            reader["Departamento"].ToString()!,
                            reader["Rol"].ToString()!,
                            reader["Cargo"].ToString()!,
                            foto
                        );
                    }
                }
            }
            return (0, "", "", "", "", "", null);
        }
    }
}