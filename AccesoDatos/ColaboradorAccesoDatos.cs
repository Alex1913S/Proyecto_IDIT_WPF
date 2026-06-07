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

        //public DataTable ListarColaboradores(string busqueda)
        //{
        //    DataTable tabla = new DataTable();
        //    string query = "SELECT DocumentoIdentidad, (Nombres + ' ' + Apellidos) AS Nombre, Cargo, Estado FROM Core.Colaboradores";

        //    if (!string.IsNullOrEmpty(busqueda))
        //    {
        //        query += " WHERE Nombres LIKE @busqueda OR Apellidos LIKE @busqueda OR DocumentoIdentidad LIKE @busqueda";
        //    }

        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            if (!string.IsNullOrEmpty(busqueda))
        //            {
        //                cmd.Parameters.AddWithValue("@busqueda", "%" + busqueda + "%");
        //            }

        //            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
        //            {
        //                adapter.Fill(tabla);
        //            }
        //        }
        //    }
        //    return tabla;
        //}

        public DataTable ListarColaboradores(string busqueda)
        {
            DataTable tabla = new DataTable();
            string query = @"SELECT DocumentoIdentidad, 
                            Nombres, 
                            Apellidos, 
                            (Nombres + ' ' + Apellidos) AS NombreCompleto, 
                            CorreoCorporativo, 
                            DepartamentoID, 
                            UbicacionID, 
                            FechaIngreso, 
                            Estado, 
                            PerfilID, 
                            UsuarioApp, 
                            Foto, 
                            Cargo 
                     FROM Core.Colaboradores";

            if (!string.IsNullOrEmpty(busqueda))
            {
                query += " WHERE Nombres LIKE @busqueda OR Apellidos LIKE @busqueda OR DocumentoIdentidad LIKE @busqueda";
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
    }
}
