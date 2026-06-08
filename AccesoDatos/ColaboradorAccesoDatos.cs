using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class ColaboradorAccesoDatos : ConexionSql
    {
        public bool InsertarColaborador(
            string documentoIdentidad, string nombres, string apellidos,
            string correoCorporativo, int departamentoId, int ubicacionId,
            DateTime fechaIngreso, string estado, int perfilId,
            string usuarioApp, string passwordPlano, byte[] foto, string cargo)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO Core.Colaboradores 
                (DocumentoIdentidad, Nombres, Apellidos, CorreoCorporativo, DepartamentoID, UbicacionID, FechaIngreso, Estado, PerfilID, UsuarioApp, PasswordHash, Foto, Cargo)
                VALUES 
                (@cedula, @nombre, @apellidos, @correo, @deptoId, @ubiId, @fecha, @estado, @perfilId, @usuario, HASHBYTES('SHA2_256', @pass), @foto, @cargo)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@cedula", SqlDbType.VarChar).Value = documentoIdentidad;
                    cmd.Parameters.Add("@nombre", SqlDbType.VarChar).Value = nombres;
                    cmd.Parameters.Add("@apellidos", SqlDbType.VarChar).Value = apellidos;
                    cmd.Parameters.Add("@correo", SqlDbType.VarChar).Value = correoCorporativo;
                    cmd.Parameters.Add("@deptoId", SqlDbType.Int).Value = departamentoId;
                    cmd.Parameters.Add("@ubiId", SqlDbType.Int).Value = ubicacionId;
                    cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = fechaIngreso;
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = (estado == "Activo");
                    cmd.Parameters.Add("@perfilId", SqlDbType.Int).Value = perfilId;
                    cmd.Parameters.Add("@usuario", SqlDbType.VarChar).Value = usuarioApp;
                    cmd.Parameters.Add("@pass", SqlDbType.VarChar).Value = passwordPlano;
                    cmd.Parameters.Add("@cargo", SqlDbType.VarChar).Value = cargo;

                    if (foto != null)
                        cmd.Parameters.Add("@foto", SqlDbType.VarBinary).Value = foto;
                    else
                        cmd.Parameters.Add("@foto", SqlDbType.VarBinary).Value = DBNull.Value;

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public DataTable ObtenerDepartamentos()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = "SELECT DepartamentoID, Nombre FROM Core.Departamentos ORDER BY Nombre ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public DataTable ObtenerUbicaciones()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = "SELECT UbicacionID, NombreNomenclatura FROM Core.Ubicaciones ORDER BY NombreNomenclatura ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public DataTable ObtenerPerfiles()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = "SELECT PerfilID, NombrePerfil FROM Seguridad.Perfiles ORDER BY NombrePerfil ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public DataTable ListarColaboradores(string busqueda)
        {
            DataTable tabla = new DataTable();

            // Se agregaron LEFT JOINs para traer los nombres reales de Perfiles, Departamentos y Ubicaciones
            string query = @"SELECT C.DocumentoIdentidad, 
                                    C.Nombres, 
                                    C.Apellidos, 
                                    (C.Nombres + ' ' + C.Apellidos) AS NombreCompleto, 
                                    C.CorreoCorporativo, 
                                    C.DepartamentoID, 
                                    D.Nombre AS NombreDepartamento,
                                    C.UbicacionID, 
                                    U.NombreNomenclatura AS NombreUbicacion,
                                    C.FechaIngreso, 
                                    C.Estado, 
                                    C.PerfilID, 
                                    P.NombrePerfil, 
                                    C.UsuarioApp, 
                                    C.Foto, 
                                    C.Cargo 
                             FROM Core.Colaboradores C
                             LEFT JOIN Core.Departamentos D ON C.DepartamentoID = D.DepartamentoID
                             LEFT JOIN Core.Ubicaciones U ON C.UbicacionID = U.UbicacionID
                             LEFT JOIN Seguridad.Perfiles P ON C.PerfilID = P.PerfilID";

            if (!string.IsNullOrEmpty(busqueda))
            {
                query += " WHERE C.Nombres LIKE @busqueda OR C.Apellidos LIKE @busqueda OR C.DocumentoIdentidad LIKE @busqueda";
            }

            using (var conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(busqueda))
                    {
                        cmd.Parameters.AddWithValue("@busqueda", "%" + busqueda + "%");
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(tabla);
                    }
                }
            }
            return tabla;
        }

        public bool ModificarColaborador(
            string documentoIdentidad, string nombres, string apellidos,
            string correoCorporativo, int departamentoId, int ubicacionId,
            DateTime fechaIngreso, string estado, int perfilId,
            string usuarioApp, string passwordPlano, byte[] foto, string cargo)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"UPDATE Core.Colaboradores 
                        SET Nombres = @nombre, 
                            Apellidos = @apellidos, 
                            CorreoCorporativo = @correo, 
                            DepartamentoID = @deptoId, 
                            UbicacionID = @ubiId, 
                            FechaIngreso = @fecha, 
                            Estado = @estado, 
                            PerfilID = @perfilId, 
                            UsuarioApp = @usuario, 
                            PasswordHash = HASHBYTES('SHA2_256', @pass), 
                            Foto = @foto, 
                            Cargo = @cargo
                        WHERE DocumentoIdentidad = @cedula";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@cedula", SqlDbType.VarChar).Value = documentoIdentidad;
                    cmd.Parameters.Add("@nombre", SqlDbType.VarChar).Value = nombres;
                    cmd.Parameters.Add("@apellidos", SqlDbType.VarChar).Value = apellidos;
                    cmd.Parameters.Add("@correo", SqlDbType.VarChar).Value = correoCorporativo;
                    cmd.Parameters.Add("@deptoId", SqlDbType.Int).Value = departamentoId;
                    cmd.Parameters.Add("@ubiId", SqlDbType.Int).Value = ubicacionId;
                    cmd.Parameters.Add("@fecha", SqlDbType.DateTime).Value = fechaIngreso;
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = (estado == "Activo");
                    cmd.Parameters.Add("@perfilId", SqlDbType.Int).Value = perfilId;
                    cmd.Parameters.Add("@usuario", SqlDbType.VarChar).Value = usuarioApp;
                    cmd.Parameters.Add("@pass", SqlDbType.VarChar).Value = passwordPlano;
                    cmd.Parameters.Add("@cargo", SqlDbType.VarChar).Value = cargo;

                    if (foto != null)
                        cmd.Parameters.Add("@foto", SqlDbType.VarBinary).Value = foto;
                    else
                        cmd.Parameters.Add("@foto", SqlDbType.VarBinary).Value = DBNull.Value;

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool EliminarColaborador(string documentoIdentidad)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM Core.Colaboradores WHERE DocumentoIdentidad = @cedula";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@cedula", SqlDbType.VarChar).Value = documentoIdentidad;
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public int ObtenerTotalColaboradores()
        {
            const string sql = "SELECT COUNT(*) FROM Core.Colaboradores";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                    return (int)cmd.ExecuteScalar();
            }
        }
    }
}