using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    public class ConexionSql
    {
        // 1. La cadena de conexión ahora es ESTÁTICA para que se comparta entre todas las instancias.
        protected static readonly string _connectionString;

        // Variables de instancia que se mantienen igual
        public DataSet Ds = new DataSet();
        public DataSet DsDM = new DataSet();
        private SqlDataAdapter Da;
        private SqlDataAdapter DaDM;
        private SqlCommandBuilder Cmb;
        private SqlCommandBuilder CmbDM;

        // 2. El constructor estático se ejecuta UNA SOLA VEZ en todo el ciclo de vida de la aplicación.
        static ConexionSql()
        {
            string[] instanciasComunes = {
                @".\SQLEXPRESS",
                @".",
                @"(localdb)\MSSQLLocalDB",
                @"localhost"
            };

            bool conexionExitosa = false;

            foreach (string instancia in instanciasComunes)
            {
                string cadenaTentativa = $"Data Source={instancia};Initial Catalog=GSSGSI1;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connection Timeout=2;";

                if (ProbarConexionEstatica(cadenaTentativa))
                {
                    _connectionString = cadenaTentativa;
                    conexionExitosa = true;
                    break;
                }
            }

            if (!conexionExitosa)
            {
                _connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=GSSGSI1;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
            }
        }

        // 3. El constructor de instancia ahora está vacío (o se puede omitir si no hace nada más).
        // La creación de objetos de esta clase ahora será instantánea.
        public ConexionSql()
        {
        }

        // Este método también debe ser estático para ser llamado desde el constructor estático
        private static bool ProbarConexionEstatica(string cadena)
        {
            try
            {
                using (var conexion = new SqlConnection(cadena))
                {
                    conexion.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
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

        // ... [El resto de tus métodos: ConsultaDatos, InsertarParametrizado, etc., se mantienen exactamente igual] ...

        public void ConsultaDatos(string sql, string Tabla)
        {
            Ds.Tables.Clear();
            try
            {
                Da = new SqlDataAdapter(sql, _connectionString);
                Cmb = new SqlCommandBuilder(Da);
                Da.Fill(Ds, Tabla);
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error al ejecutar SQL: {sql}. Mensaje: {ex.Message}", ex);
            }
        }

        // ... (resto de métodos omitidos por brevedad, no cambian) ...
    }
}