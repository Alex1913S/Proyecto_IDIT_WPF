using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace AccesoDatos
{
    public class UsuarioAccesoDatos : ConexionSql
    {
        //public bool ValidarCredenciales(string correo, string passwordHash)
        //{
        //    using (var conn = GetConnection())
        //    {
        //        conn.Open();
        //        string query = @"SELECT COUNT(*) FROM Core.Colaboradores 
        //                     WHERE CorreoCorporativo = @correo 
        //                     AND PasswordHash = HASHBYTES('SHA2_256', @pass)";

        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        cmd.Parameters.Add("@correo", SqlDbType.VarChar).Value = correo;
        //        cmd.Parameters.Add("@pass", SqlDbType.VarChar).Value = passwordHash;

        //        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        //    }
        //}

        public int ValidarCredenciales(string correo, string passwordHash)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT Estado FROM Core.Colaboradores 
                         WHERE CorreoCorporativo = @correo 
                         AND PasswordHash = HASHBYTES('SHA2_256', @pass)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add("@correo", SqlDbType.VarChar).Value = correo;
                cmd.Parameters.Add("@pass", SqlDbType.VarChar).Value = passwordHash;

                object resultado = cmd.ExecuteScalar();

                if (resultado == null)
                {
                    return -1;
                }
                return Convert.ToBoolean(resultado) ? 1 : 0;
            }
        }

        public (string Nombres, string Apellidos, string Departamento, string Rol, string Cargo, byte[] Foto) ObtenerDatosUsuario(string correo)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"SELECT c.Nombres, c.Apellidos, 
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
                        byte[] foto = reader["Foto"] == DBNull.Value
                                      ? null
                                      : (byte[])reader["Foto"];

                        return
                            (
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
            return ("", "", "", "", "", null);
        }

    }
}
