using AccesoDatos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Dominio
{
    public class ResultadoLogin
    {
        public bool Exitoso { get; set; }
        public bool Bloqueado { get; set; }
        public int Intentos { get; set; }
        public string Mensaje { get; set; } = "";

        // Datos del usuario
        public string Nombres { get; set; } = "";
        public string Apellidos { get; set; } = "";
        public string Departamento { get; set; } = "";
        public string Rol { get; set; } = string.Empty;
        public string Cargo { get; set; } = "";
        public byte[] Foto { get; set; }
    }

    public class ResultadoActivo
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class ResultadoColaborador
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class UsuarioDominio
    {
        private readonly UsuarioAccesoDatos _accesoDatos = new UsuarioAccesoDatos();
        private int _intentosFallidos = 0;
        private const int MaxIntentos = 3;

        public ResultadoLogin Login(string correo, string passwordHash)
        {
            ResultadoLogin resultado = new ResultadoLogin();

            // 1. Validar credenciales y capturar el estado de integridad
            int codigoControl = _accesoDatos.ValidarCredenciales(correo, passwordHash);

            if (codigoControl == -1)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Usuario o contraseña incorrectos.";
                return resultado;
            }
            else if (codigoControl == 0)
            {
                // CONTROL DE ACCESO CRÍTICO: Detiene la autenticación de raíz si está Inactivo
                resultado.Exitoso = false;
                resultado.Mensaje = "Acceso denegado: Esta cuenta se encuentra INACTIVA. Comuníquese con el Administrador de Seguridad de la Información.";
                return resultado;
            }

            // 2. Si el código es 1 (Activo), procedemos a extraer el perfil completo para el Dashboard
            var datos = _accesoDatos.ObtenerDatosUsuario(correo);

            resultado.Exitoso = true;
            resultado.Nombres = datos.Nombres;
            resultado.Apellidos = datos.Apellidos;
            resultado.Departamento = datos.Departamento;
            resultado.Rol = datos.Rol;
            resultado.Cargo = datos.Cargo;
            resultado.Foto = datos.Foto;
            resultado.Mensaje = "Bienvenido al sistema.";

            return resultado;
        }

        public class ActivosDominio
        {
            private readonly ActivosAccesoDatos _datos = new ActivosAccesoDatos();
            public decimal ObtenerValorTotalInventario() => _datos.ObtenerValorTotalInventario();
            public int ObtenerTotalActivos() => _datos.ObtenerTotalActivos();
            public decimal ObtenerPorcentajeGarantiasVigentes() => _datos.ObtenerPorcentajeGarantiasVigentes();


            // Métodos nuevos para soportar el DataGrid y los filtros
            public DataTable ListarActivos() => _datos.ObtenerTodosLosActivos();
            public DataTable ObtenerCategorias() => _datos.ObtenerCategorias();
            public DataTable ObtenerUbicaciones() => _datos.ObtenerUbicaciones();

            public DataTable ObtenerTop5CategoriasPorCantidad() => _datos.ObtenerTop5CategoriasPorCantidad();


            /// <summary>
            /// CORREGIDO: Ahora recibe e inserta el parámetro 'etiqueta' requerido por la capa de datos.
            /// </summary>
            public ResultadoActivo CrearActivo(
                int categoriaId, int ubicacionId, string etiqueta, // <-- Agregado parámetro etiqueta
                string marca, string modelo, string numeroSerie, int? proveedorId,
                DateTime? fechaAdquis, decimal? costo, string estadoOperativo,
                string procesador, string memoriaRAM, string almac1, string almac2,
                string tarjetaGrafica, string sistemaOperativo, string mac, string ip,
                string resolucion)
            {
                var resultado = new ResultadoActivo();

                try
                {
                    if (categoriaId <= 0)
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = "Debe seleccionar una categoría.";
                        return resultado;
                    }

                    if (ubicacionId <= 0)
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = "Debe seleccionar una ubicación.";
                        return resultado;
                    }

                    if (string.IsNullOrWhiteSpace(etiqueta))
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = "La etiqueta o placa de inventario es obligatoria.";
                        return resultado;
                    }

                    bool ok = _datos.InsertarActivo(
                        categoriaId, ubicacionId, etiqueta, // <-- Enviado correctamente a Acceso a Datos
                        marca, modelo, numeroSerie, proveedorId,
                        fechaAdquis, costo, estadoOperativo,
                        procesador, memoriaRAM, almac1, almac2,
                        tarjetaGrafica, sistemaOperativo, mac, ip,
                        resolucion
                    );

                    resultado.Exitoso = ok;
                    resultado.Mensaje = ok
                        ? "Activo registrado correctamente."
                        : "No se pudo registrar el activo.";
                }
                catch (Exception ex)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"ERROR: {ex.Message}";
                }

                return resultado;
            }

            /// <summary>
            /// CORREGIDO: Se incluyó 'string etiqueta' en la firma para que coincida con los 20 parámetros esperados.
            /// </summary>
            public ResultadoActivo ModificarActivo(
                Guid activoId, int categoriaId, int ubicacionId, string etiqueta, // <-- Insertado en la 4ta posición reglamentaria
                string marca, string modelo, string numeroSerie, int? proveedorId,
                DateTime? fechaAdquis, decimal? costo, string estadoOperativo,
                string procesador, string memoriaRAM, string almac1, string almac2,
                string tarjetaGrafica, string sistemaOperativo, string mac, string ip, string resolucion)
            {
                var resultado = new ResultadoActivo();
                try
                {
                    if (activoId == Guid.Empty)
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = "Identificador de activo inválido o vacío.";
                        return resultado;
                    }

                    if (string.IsNullOrWhiteSpace(etiqueta))
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = "La etiqueta no puede estar vacía durante una actualización.";
                        return resultado;
                    }

                    // Se envía de forma ordenada mapeando la correspondencia exacta de tipos de la BD
                    bool operacionExitosa = _datos.ActualizarActivo(
                        activoId, categoriaId, ubicacionId, etiqueta, marca, modelo,
                        numeroSerie, proveedorId, fechaAdquis, costo, estadoOperativo,
                        procesador, memoriaRAM, almac1, almac2, tarjetaGrafica, sistemaOperativo,
                        mac, ip, resolucion
                    );

                    resultado.Exitoso = operacionExitosa;
                    resultado.Mensaje = operacionExitosa
                        ? "El activo y su ficha técnica se modificaron con éxito."
                        : "No se pudo actualizar en la base de datos.";
                }
                catch (Exception ex)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"Error en capa de dominio: {ex.Message}";
                }
                return resultado;
            }

            public ResultadoActivo EliminarActivoLogico(Guid activoId, string estadoActual)
            {
                var resultado = new ResultadoActivo();

                // REGLA CRÍTICA SGSI: Un activo asignado a un empleado no puede ser borrado sin una devolución formal
                if (estadoActual.Equals("Asignado", StringComparison.OrdinalIgnoreCase))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "RESTRICCIÓN: No es posible dar de baja un activo que está actualmente con el estado 'Asignado'. Debe registrar la devolución en el módulo de Asignaciones antes de sacarlo de circulación.";
                    return resultado;
                }

                bool operacionExitosa = _datos.DarDeBajaActivo(activoId);
                resultado.Exitoso = operacionExitosa;
                resultado.Mensaje = operacionExitosa ? "El activo ha sido retirado y marcado 'De Baja' correctamente." : "Error al procesar la baja.";
                return resultado;
            }
        }
    }

    public class AsignarActivoDominio
    {
        private readonly AsignarActivoAccesoDatos _acceso = new();

        public DataTable ObtenerActivosDisponibles() => _acceso.ObtenerActivosDisponibles();

        public DataTable BuscarActivos(string termino)
            => string.IsNullOrWhiteSpace(termino)
                ? _acceso.ObtenerActivosDisponibles()
                : _acceso.BuscarActivosDisponibles(termino.Trim());

        public DataTable ObtenerColaboradores() => _acceso.ObtenerColaboradores();

        public DataTable BuscarColaboradores(string termino)
            => string.IsNullOrWhiteSpace(termino)
                ? _acceso.ObtenerColaboradores()
                : _acceso.BuscarColaboradores(termino.Trim());

        public ResultadoAsignacion Registrar(
            Guid? activoId, int? colaboradorId, DateTime fechaAsignacion, string observaciones)
        {
            if (activoId == null || activoId == Guid.Empty)
                return Error("Debe seleccionar un activo de la lista.");

            if (colaboradorId == null || colaboradorId <= 0)
                return Error("Debe seleccionar un colaborador de la lista.");

            if (fechaAsignacion > DateTime.Today)
                return Error("La fecha de asignación no puede ser futura.");

            int id = _acceso.RegistrarAsignacion(
                activoId.Value, colaboradorId.Value, fechaAsignacion, observaciones);

            return id > 0
                ? new ResultadoAsignacion { Exitoso = true, AsignacionID = id }
                : Error("Ocurrió un error al guardar. Intente nuevamente.");
        }

        private static ResultadoAsignacion Error(string msg) => new() { Exitoso = false, Mensaje = msg };
    }

    public class ResultadoAsignacion
    {
        public bool Exitoso { get; set; }
        public int AsignacionID { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class AuditoriaDominio
    {
        private readonly AuditoriaAccesoDatos _datos = new AuditoriaAccesoDatos();

        public DataTable ListarLogsAuditoria(DateTime desde, DateTime hasta)
        {
            if (desde > hasta)
            {
                throw new ArgumentException("La fecha de inicio ('Desde') no puede ser posterior a la fecha de fin ('Hasta').");
            }
            return _datos.ObtenerLogs(desde, hasta);
        }
    }

    public class ColaboradorDominio
    {
        private readonly ColaboradorAccesoDatos _accesoDatos = new ColaboradorAccesoDatos();
        public int ObtenerTotalColaboradores() => _accesoDatos.ObtenerTotalColaboradores();

        public ResultadoColaborador RegistrarColaborador(
            string documentoIdentidad, string nombres, string apellidos,
            string correoCorporativo, int departamentoId, int ubicacionId,
            DateTime fechaIngreso, string estado, int perfilId,
            string usuarioApp, string password, byte[] foto, string cargo)
        {
            var resultado = new ResultadoColaborador();

            try
            {
                if (string.IsNullOrWhiteSpace(documentoIdentidad) || string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Cédula, Nombres y Apellidos son campos estrictamente obligatorios.";
                    return resultado;
                }

                if (string.IsNullOrWhiteSpace(usuarioApp) || string.IsNullOrWhiteSpace(password))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Las credenciales de acceso de la aplicación (Usuario y Contraseña) son obligatorias.";
                    return resultado;
                }

                if (departamentoId <= 0 || ubicacionId <= 0 || perfilId <= 0)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Debe seleccionar un Departamento, Ubicación y Perfil válidos.";
                    return resultado;
                }

                string passwordHash = EncriptarTextoSHA256(password);

                bool ok = _accesoDatos.InsertarColaborador(
                    documentoIdentidad, nombres, apellidos, correoCorporativo,
                    departamentoId, ubicacionId, fechaIngreso, estado, perfilId,
                    usuarioApp, passwordHash, foto, cargo
                );

                resultado.Exitoso = ok;
                resultado.Mensaje = ok ? "Colaborador registrado exitosamente en el sistema." : "No se pudo completar el registro del colaborador.";
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"ERROR en Capa de Dominio: {ex.Message}";
            }

            return resultado;
        }

        private string EncriptarTextoSHA256(string textoPlano)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public DataTable ListarDepartamentos() => _accesoDatos.ObtenerDepartamentos();
        public DataTable ListarUbicaciones() => _accesoDatos.ObtenerUbicaciones();
        public DataTable ListarPerfiles() => _accesoDatos.ObtenerPerfiles();
    }

    public class CN_Colaboradores
    {
        private ColaboradorAccesoDatos objetoCD = new ColaboradorAccesoDatos();

        public bool EditarColaborador(
            string documentoIdentidad, string nombres, string apellidos,
            string correoCorporativo, int departamentoId, int ubicacionId,
            DateTime fechaIngreso, string estado, int perfilId,
            string usuarioApp, string passwordPlano, byte[] foto, string cargo)
        {
            return objetoCD.ModificarColaborador(documentoIdentidad, nombres, apellidos, correoCorporativo,
                                                 departamentoId, ubicacionId, fechaIngreso, estado,
                                                 perfilId, usuarioApp, passwordPlano, foto, cargo);
        }

        public bool EliminarColaborador(string documentoIdentidad) => objetoCD.EliminarColaborador(documentoIdentidad);

        public DataTable MostrarColaboradores(string busqueda = "") => objetoCD.ListarColaboradores(busqueda);
    }


}