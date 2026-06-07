using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class ConexionSql
    {
        protected readonly string _connectionString;

        public DataSet Ds = new DataSet();
        public DataSet DsDM = new DataSet();
        private SqlDataAdapter Da;
        private SqlDataAdapter DaDM;
        private SqlCommandBuilder Cmb;
        private SqlCommandBuilder CmbDM;

        public ConexionSql()
        {
            // Tu cadena de conexión hacia GSSGSI1
            _connectionString = @"Data Source=.;Initial Catalog=GSSGSI1;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
        }

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public static class VariablesGlobales
        {
            public static int xEstIni = 0;
            public static string xNomU = "";
            public static string xTipoU = "";
            public static byte[] xFoto;
        }

        public void ConsultaDatos(string sql, string Tabla)
        {
            Ds.Tables.Clear();
            try
            {
                Da = new SqlDataAdapter(sql, _connectionString);
                Cmb = new SqlCommandBuilder(Da); // Ahora compilará perfectamente
                Da.Fill(Ds, Tabla);
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al ejecutar SQL: {sql}. Mensaje: {ex.Message}", ex);
            }
        }

        public void ConsultaDatosDM(string sql, string Tabla)
        {
            DsDM.Tables.Clear();
            DaDM = new SqlDataAdapter(sql, _connectionString);
            CmbDM = new SqlCommandBuilder(DaDM);
            DaDM.Fill(DsDM, Tabla);
        }

        // =================================================================
        // MÉTODO NUEVO RECOMENDADO: INSERCIÓN SEGURA PARAMETRIZADA (SGSI)
        // =================================================================
        public bool InsertarParametrizado(string query, SqlParameter[] parametros)
        {
            using (var Conn = GetConnection())
            {
                try
                {
                    Conn.Open();
                    using (SqlCommand Comando = new SqlCommand(query, Conn))
                    {
                        if (parametros != null)
                        {
                            Comando.Parameters.AddRange(parametros);
                        }
                        int filasAfectadas = Comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception($"Error en inserción parametrizada: {ex.Message}", ex);
                }
            }
        }

        public bool Insertar(string sql)
        {
            using (var Conn = GetConnection())
            {
                Conn.Open();
                SqlCommand Comando = new SqlCommand(sql, Conn);
                int i = Comando.ExecuteNonQuery();
                return i > 0;
            }
        }

        public bool ConsultaItem(string tabla, string condicion)
        {
            using (var Conn = GetConnection())
            {
                Conn.Open();
                string query = $"Select Count(*) From {tabla} Where {condicion}";
                SqlCommand Comando = new SqlCommand(query, Conn);
                int i = Convert.ToInt32(Comando.ExecuteScalar());
                return i > 0;
            }
        }

        public bool ConsultaLike(string tabla, string condicion)
        {
            using (var Conn = GetConnection())
            {
                Conn.Open();
                string query = $"Select Count(*) From {tabla} Where {condicion}";
                SqlCommand Comando = new SqlCommand(query, Conn);
                int i = Convert.ToInt32(Comando.ExecuteScalar());
                return i > 0;
            }
        }

        public bool Eliminar(string tabla, string condicion)
        {
            using (var Conn = GetConnection())
            {
                Conn.Open();
                string query = $"Delete From {tabla} Where {condicion}";
                SqlCommand Comando = new SqlCommand(query, Conn);
                int i = Comando.ExecuteNonQuery();
                return i > 0;
            }
        }

        public bool Actualizar(string tabla, string campos, string condicion)
        {
            using (var Conn = GetConnection())
            {
                Conn.Open();
                string query = $"Update {tabla} set {campos} Where {condicion}";
                SqlCommand Comando = new SqlCommand(query, Conn);
                int i = Comando.ExecuteNonQuery();
                return i > 0;
            }
        }

        public bool Buscar(string tabla, string condicion)
        {
            using (var Conn = GetConnection())
            {
                Conn.Open();
                string query = $"Select Count(*) From {tabla} Where {condicion}";
                SqlCommand Comando = new SqlCommand(query, Conn);
                int i = Convert.ToInt32(Comando.ExecuteScalar());
                return i > 0;
            }
        }
    }
}