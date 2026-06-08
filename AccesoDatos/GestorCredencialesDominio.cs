using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AccesoDatos
{
    public class GestorCredencialesAccesoDatos : ConexionSql
    {
        // ─────────────────────────────────────────────────────────────
        // LISTAR credenciales del colaborador autenticado
        // ─────────────────────────────────────────────────────────────
        public DataTable ObtenerCredencialesDeColaborador(int colaboradorId)
        {
            const string sql = @"
                SELECT 
                    G.CredencialID,
                    G.NombreServicio,
                    G.URL_Acceso,
                    G.Usuario,
                    G.ContrasenaCifrada,
                    G.Categoria,
                    G.FechaCreacion,
                    G.UltimaActualizacion,
                    G.FechaVencimientoClave,
                    G.NotasSeguras,
                    G.PropietarioColaboradorID,
                    G.PropietarioDepartamentoID,
                    D.Nombre AS NombreDepartamento
                FROM Seguridad.GestorCredenciales G
                LEFT JOIN Core.Departamentos D 
                    ON G.PropietarioDepartamentoID = D.DepartamentoID
                WHERE G.PropietarioColaboradorID = @ColaboradorID
                ORDER BY G.UltimaActualizacion DESC";

            return Ejecutar(sql, cmd =>
                cmd.Parameters.AddWithValue("@ColaboradorID", colaboradorId));
        }

        // ─────────────────────────────────────────────────────────────
        // BUSCAR dentro de las credenciales del usuario
        // ─────────────────────────────────────────────────────────────
        public DataTable BuscarCredenciales(int colaboradorId, string termino)
        {
            const string sql = @"
                SELECT 
                    G.CredencialID,
                    G.NombreServicio,
                    G.URL_Acceso,
                    G.Usuario,
                    G.ContrasenaCifrada,
                    G.Categoria,
                    G.FechaCreacion,
                    G.UltimaActualizacion,
                    G.FechaVencimientoClave,
                    G.NotasSeguras,
                    G.PropietarioColaboradorID,
                    G.PropietarioDepartamentoID
                FROM Seguridad.GestorCredenciales G
                WHERE G.PropietarioColaboradorID = @ColaboradorID
                  AND (G.NombreServicio    LIKE @T
                    OR G.Usuario          LIKE @T
                    OR G.Categoria        LIKE @T
                    OR G.URL_Acceso       LIKE @T)
                ORDER BY G.UltimaActualizacion DESC";

            return Ejecutar(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@ColaboradorID", colaboradorId);
                cmd.Parameters.AddWithValue("@T", $"%{termino}%");
            });
        }

        // ─────────────────────────────────────────────────────────────
        // INSERTAR nueva credencial (CON MANEJO DE ERRORES)
        // ─────────────────────────────────────────────────────────────
        public bool InsertarCredencial(
    string nombreServicio, string urlAcceso, string usuario,
    string contrasenaCifrada, string categoria,
    int colaboradorId, int? departamentoId,
    DateTime? fechaVencimiento, string notasSeguras)
        {
            try
            {
                const string sql = @"
            INSERT INTO Seguridad.GestorCredenciales
                (NombreServicio, URL_Acceso, Usuario, ContrasenaCifrada,
                 Categoria, PropietarioColaboradorID, PropietarioDepartamentoID,
                 FechaCreacion, UltimaActualizacion, FechaVencimientoClave, NotasSeguras)
            VALUES
                (@NombreServicio, @URL, @Usuario, @Contrasena,
                 @Categoria, @ColaboradorID, @DepartamentoID,
                 GETDATE(), GETDATE(), @FechaVenc, @Notas)";

                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@NombreServicio", SqlDbType.VarChar).Value = (object)nombreServicio ?? DBNull.Value;
                cmd.Parameters.Add("@URL", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(urlAcceso) ? DBNull.Value : urlAcceso;
                cmd.Parameters.Add("@Usuario", SqlDbType.VarChar).Value = (object)usuario ?? DBNull.Value;

                // ─────────────────────────────────────────────────────────────────────────────
                // MODIFICACIÓN AQUÍ: Convertir el string de la contraseña a bytes para VARBINARY
                // ─────────────────────────────────────────────────────────────────────────────
                object valorContrasena = DBNull.Value;
                if (!string.IsNullOrEmpty(contrasenaCifrada))
                {
                    // Opción A: Si tu capa de dominio cifra la contraseña devolviendo texto normal o Hexadecimal:
                    valorContrasena = Encoding.UTF8.GetBytes(contrasenaCifrada);

                    // Opción B: Si tu capa de dominio cifra la contraseña y la devuelve en formato Base64 (Muy común):
                    // valorContrasena = Convert.FromBase64String(contrasenaCifrada);
                }
                cmd.Parameters.Add("@Contrasena", SqlDbType.VarBinary).Value = valorContrasena;
                // ─────────────────────────────────────────────────────────────────────────────

                cmd.Parameters.Add("@Categoria", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(categoria) ? DBNull.Value : categoria;
                cmd.Parameters.Add("@ColaboradorID", SqlDbType.Int).Value = colaboradorId;
                cmd.Parameters.Add("@DepartamentoID", SqlDbType.Int).Value = (object)departamentoId ?? DBNull.Value;
                cmd.Parameters.Add("@FechaVenc", SqlDbType.DateTime).Value = (object)fechaVencimiento ?? DBNull.Value;
                cmd.Parameters.Add("@Notas", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(notasSeguras) ? DBNull.Value : notasSeguras;

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de SQL [Código {ex.Number}]: {ex.Message}");
                throw new Exception("Error en la base de datos al intentar registrar la credencial.", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error general en Acceso a Datos: {ex.Message}");
                throw new Exception("Ocurrió un error inesperado al procesar la solicitud.", ex);
            }
        }
        // ─────────────────────────────────────────────────────────────
        // ACTUALIZAR credencial (SOLUCIÓN AL VARBINARY EN UPDATE)
        // ─────────────────────────────────────────────────────────────
        public bool ActualizarCredencial(
            int credencialId, string nombreServicio, string urlAcceso, string usuario,
            string contrasenaCifrada, string categoria, int? departamentoId,
            DateTime? fechaVencimiento, string notasSeguras)
        {
            try
            {
                const string sql = @"
            UPDATE Seguridad.GestorCredenciales
            SET NombreServicio = @NombreServicio,
                URL_Acceso = @URL,
                Usuario = @Usuario,
                ContrasenaCifrada = @Contrasena,
                Categoria = @Categoria,
                PropietarioDepartamentoID = @DepartamentoID,
                UltimaActualizacion = GETDATE(),
                FechaVencimientoClave = @FechaVenc,
                NotasSeguras = @Notas
            WHERE CredencialID = @CredencialID";

                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@CredencialID", SqlDbType.Int).Value = credencialId;
                cmd.Parameters.Add("@NombreServicio", SqlDbType.VarChar).Value = (object)nombreServicio ?? DBNull.Value;
                cmd.Parameters.Add("@URL", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(urlAcceso) ? DBNull.Value : urlAcceso;
                cmd.Parameters.Add("@Usuario", SqlDbType.VarChar).Value = (object)usuario ?? DBNull.Value;

                // ─────────────────────────────────────────────────────────────────────────────
                // CORRECCIÓN AQUÍ: Convertir el string de la contraseña a bytes para VARBINARY
                // ─────────────────────────────────────────────────────────────────────────────
                object valorContrasena = DBNull.Value;
                if (!string.IsNullOrEmpty(contrasenaCifrada))
                {
                    // Opción A: Si usaste Encoding.UTF8 en el método AplicarFiltros:
                    valorContrasena = Encoding.UTF8.GetBytes(contrasenaCifrada);

                    // Opción B: Si estás utilizando Base64:
                    // valorContrasena = Convert.FromBase64String(contrasenaCifrada);
                }
                cmd.Parameters.Add("@Contrasena", SqlDbType.VarBinary).Value = valorContrasena;
                // ─────────────────────────────────────────────────────────────────────────────

                cmd.Parameters.Add("@Categoria", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(categoria) ? DBNull.Value : categoria;
                cmd.Parameters.Add("@DepartamentoID", SqlDbType.Int).Value = (object)departamentoId ?? DBNull.Value;
                cmd.Parameters.Add("@FechaVenc", SqlDbType.DateTime).Value = (object)fechaVencimiento ?? DBNull.Value;
                cmd.Parameters.Add("@Notas", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(notasSeguras) ? DBNull.Value : notasSeguras;

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de SQL [Código {ex.Number}]: {ex.Message}");
                throw new Exception("Error en la base de datos al intentar actualizar la credencial.", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error general en Actualizar: {ex.Message}");
                throw new Exception("Ocurrió un error inesperado al actualizar la solicitud.", ex);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ELIMINAR credencial (física — el usuario la borra intencionalmente)
        // ─────────────────────────────────────────────────────────────
        public bool EliminarCredencial(int credencialId, int colaboradorId)
        {
            // La condición de colaboradorId evita borrar credenciales ajenas
            const string sql = @"
                DELETE FROM Seguridad.GestorCredenciales
                WHERE CredencialID = @CredencialID
                  AND PropietarioColaboradorID = @ColaboradorID";

            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CredencialID", credencialId);
            cmd.Parameters.AddWithValue("@ColaboradorID", colaboradorId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // ─────────────────────────────────────────────────────────────
        // Helper privado
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
