using Dominio;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Presentación.UserControls.Assign_Inventory;

namespace Presentación
{
    public partial class Employee_Viewer : UserControl
    {
        // ── Capa de negocio ───────────────────────────────────────────────
        private readonly CN_Colaboradores _cnColaboradores = new CN_Colaboradores();
        private readonly ColaboradorDominio _colaboradorDominio = new ColaboradorDominio();

        // ── Tabla completa desde BD ───────────────────────────────────────
        private DataTable _tablaCompleta;

        // ── Registros filtrados y paginación ─────────────────────────────
        private List<DataRow> _registrosFiltrados = new List<DataRow>();
        private int _paginaActual = 1;
        private const int _registrosPorPagina = 12;
        private int _totalPaginas = 1;

        // ── Filtros activos ───────────────────────────────────────────────
        private string _filtroRol = "";
        private string _filtroBusqueda = "";

        // ── Estado CRUD ───────────────────────────────────────────────────
        private bool _modoEdicion = false;
        private string _cedulaEnEdicion = "";   // clave para UPDATE
        private byte[] _fotoSeleccionada = null; // bytes de la foto elegida
        private bool _fotoFueModificada = false;
        private bool _isDarkMode = true;

        // ── Modo tema ─────────────────────────────────────────────────────
        private bool _esModoClaro = false;

        // ═════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═════════════════════════════════════════════════════════════════
        public Employee_Viewer()
        {
            InitializeComponent();
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═════════════════════════════════════════════════════════════════
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarCombosFormulario();
            CargarDatos();
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA DE COMBOS (Departamentos, Ubicaciones, Perfiles)
        // ═════════════════════════════════════════════════════════════════
        private void CargarCombosFormulario()
        {
            try
            {
                var dtDeptos = _colaboradorDominio.ListarDepartamentos();
                CbFDepartamento.Items.Clear();
                foreach (DataRow row in dtDeptos.Rows)
                    CbFDepartamento.Items.Add(new ComboItem
                    {
                        Display = row["Nombre"]?.ToString() ?? "",
                        Value = Convert.ToInt32(row["DepartamentoID"])
                    });
                CbFDepartamento.DisplayMemberPath = "Display";
                CbFDepartamento.SelectedValuePath = "Value";

                var dtUbics = _colaboradorDominio.ListarUbicaciones();
                CbFUbicacion.Items.Clear();
                foreach (DataRow row in dtUbics.Rows)
                    CbFUbicacion.Items.Add(new ComboItem
                    {
                        Display = row["NombreNomenclatura"]?.ToString() ?? "",
                        Value = Convert.ToInt32(row["UbicacionID"])
                    });
                CbFUbicacion.DisplayMemberPath = "Display";
                CbFUbicacion.SelectedValuePath = "Value";

                var dtPerfiles = _colaboradorDominio.ListarPerfiles();
                CbFPerfil.Items.Clear();
                foreach (DataRow row in dtPerfiles.Rows)
                    CbFPerfil.Items.Add(new ComboItem
                    {
                        Display = row["NombrePerfil"]?.ToString() ?? "",
                        Value = Convert.ToInt32(row["PerfilID"])
                    });
                CbFPerfil.DisplayMemberPath = "Display";
                CbFPerfil.SelectedValuePath = "Value";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar catálogos del formulario:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA Y FILTRADO DE DATOS
        // ═════════════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            try
            {
                object resultado = _cnColaboradores.MostrarColaboradores(_filtroBusqueda);

                if (resultado is DataTable dt)
                    _tablaCompleta = dt;
                else if (resultado is DataSet ds && ds.Tables.Count > 0)
                    _tablaCompleta = ds.Tables[0];
                else
                    _tablaCompleta = new DataTable();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar colaboradores:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltros()
        {
            if (_tablaCompleta == null) return;

            IEnumerable<DataRow> filas = _tablaCompleta.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_filtroBusqueda))
            {
                string criterio = _filtroBusqueda.ToLower();
                filas = filas.Where(r =>
                    (r["DocumentoIdentidad"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["NombreCompleto"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["Cargo"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["CorreoCorporativo"]?.ToString() ?? "").ToLower().Contains(criterio));
            }

            if (!string.IsNullOrEmpty(_filtroRol))
                filas = filas.Where(r =>
                    (r["NombrePerfil"]?.ToString() ?? "")
                        .Equals(_filtroRol, StringComparison.OrdinalIgnoreCase));

            _registrosFiltrados = filas.ToList();
            ActualizarBadgesTabs();
            _paginaActual = 1;
            RenderizarPagina();
        }

        private void ActualizarBadgesTabs()
        {
            if (_tablaCompleta == null) return;

            var todas = _tablaCompleta.AsEnumerable();
            BtnTabTodos.Tag = todas.Count().ToString();
            BtnTabAdministradores.Tag = todas.Count(r => (r["NombrePerfil"]?.ToString() ?? "") == "Administrador").ToString();
            BtnTabOperadores.Tag = todas.Count(r => (r["NombrePerfil"]?.ToString() ?? "") == "Operador").ToString();
            BtnTabEmpleados.Tag = todas.Count(r => (r["NombrePerfil"]?.ToString() ?? "") == "Empleado").ToString();
        }

        // ═════════════════════════════════════════════════════════════════
        // PAGINACIÓN
        // ═════════════════════════════════════════════════════════════════
        private void RenderizarPagina()
        {
            int total = _registrosFiltrados.Count;
            _totalPaginas = (int)Math.Ceiling(total / (double)_registrosPorPagina);
            if (_totalPaginas < 1) _totalPaginas = 1;
            if (_paginaActual > _totalPaginas) _paginaActual = _totalPaginas;

            var pagina = _registrosFiltrados
                .Skip((_paginaActual - 1) * _registrosPorPagina)
                .Take(_registrosPorPagina)
                .ToList();

            DgColaboradores.ItemsSource = pagina.Select(r => new
            {
                DocumentoIdentidad = r["DocumentoIdentidad"]?.ToString() ?? "",
                NombreCompleto = r["NombreCompleto"]?.ToString() ?? "",
                Cargo = r["Cargo"]?.ToString() ?? "",
                NombreDepartamento = r["NombreDepartamento"]?.ToString() ?? "",
                NombrePerfil = r["NombrePerfil"]?.ToString() ?? "",
                CorreoCorporativo = r["CorreoCorporativo"]?.ToString() ?? "",
                EstadoTexto = ConvertirEstado(r["Estado"]),
                FechaIngreso = r["FechaIngreso"] != DBNull.Value
                                        ? Convert.ToDateTime(r["FechaIngreso"])
                                        : (DateTime?)null,

                // Datos extra para panel lateral y formulario
                _Nombres = r["Nombres"]?.ToString() ?? "",
                _Apellidos = r["Apellidos"]?.ToString() ?? "",
                _FotoBytes = r["Foto"] != DBNull.Value ? (byte[])r["Foto"] : null,
                _DepartamentoID = r["DepartamentoID"] != DBNull.Value ? Convert.ToInt32(r["DepartamentoID"]) : 0,
                _UbicacionID = r["UbicacionID"] != DBNull.Value ? Convert.ToInt32(r["UbicacionID"]) : 0,
                _PerfilID = r["PerfilID"] != DBNull.Value ? Convert.ToInt32(r["PerfilID"]) : 0,
                _FechaIngresoCruda = r["FechaIngreso"] != DBNull.Value
                                        ? (DateTime?)Convert.ToDateTime(r["FechaIngreso"]) : null,
                _EstadoRaw = r["Estado"],
                _UsuarioApp = r["UsuarioApp"]?.ToString() ?? ""
            }).ToList<dynamic>();

            TxtInfoPagina.Text = $"Página {_paginaActual} de {_totalPaginas}";
            TxtContadorRegistros.Text = $"Mostrando {pagina.Count} de {total} registros";

            BtnPaginaAnterior.IsEnabled = _paginaActual > 1;
            BtnPaginaSiguiente.IsEnabled = _paginaActual < _totalPaginas;

            GenerarBotonesPagina();
            LimpiarPanelLateral();
        }

        private void GenerarBotonesPagina()
        {
            PnlNumerosPagina.Children.Clear();

            int inicio = Math.Max(1, _paginaActual - 2);
            int fin = Math.Min(_totalPaginas, _paginaActual + 2);

            for (int i = inicio; i <= fin; i++)
            {
                int numPagina = i;
                var btn = new Button
                {
                    Content = i.ToString(),
                    Style = (Style)FindResource("PagerButtonStyle"),
                    IsEnabled = i != _paginaActual,
                    Background = i == _paginaActual
                                    ? new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x45))
                                    : Brushes.Transparent,
                    Foreground = i == _paginaActual
                                    ? Brushes.White
                                    : new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8))
                };
                btn.Click += (s, e) => { _paginaActual = numPagina; RenderizarPagina(); };
                PnlNumerosPagina.Children.Add(btn);
            }
        }

        private void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1) { _paginaActual--; RenderizarPagina(); }
        }

        private void BtnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _totalPaginas) { _paginaActual++; RenderizarPagina(); }
        }

        // ═════════════════════════════════════════════════════════════════
        // SELECCIÓN EN DATAGRID → Panel lateral detalle
        // ═════════════════════════════════════════════════════════════════
        private void DgColaboradores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgColaboradores.SelectedItem == null)
            {
                LimpiarPanelLateral();
                return;
            }

            dynamic fila = DgColaboradores.SelectedItem;
            try
            {
                TxtPanelNombre.Text = fila.NombreCompleto ?? "—";
                TxtPanelCargo.Text = fila.Cargo ?? "—";
                TxtPanelCedula.Text = fila.DocumentoIdentidad ?? "—";
                TxtPanelCorreo.Text = fila.CorreoCorporativo ?? "—";
                TxtPanelDepartamento.Text = fila.NombreDepartamento ?? "—";
                TxtPanelFechaIngreso.Text = fila._FechaIngresoCruda != null
                    ? ((DateTime)fila._FechaIngresoCruda).ToString("dd/MM/yyyy") : "—";

                string perfil = fila.NombrePerfil ?? "—";
                TxtPanelPerfil.Text = perfil;
                TxtPanelPerfil.Foreground = perfil switch
                {
                    "Administrador" => new SolidColorBrush(Color.FromRgb(0xE8, 0x9A, 0x24)),
                    "Operador" => new SolidColorBrush(Color.FromRgb(0x34, 0xAA, 0xDC)),
                    "Empleado" => new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64)),
                    _ => new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8))
                };

                string estado = fila.EstadoTexto ?? "—";
                TxtPanelEstado.Text = estado;
                TxtPanelEstado.Foreground = estado == "Activo"
                    ? new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64))
                    : new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));

                // Avatar / foto
                string iniciales = ObtenerIniciales(fila.NombreCompleto ?? "?");
                TxtAvatarInicial.Text = iniciales;

                byte[] fotoBytes = fila._FotoBytes as byte[];
                if (fotoBytes != null && fotoBytes.Length > 0)
                {
                    ImgFotoColaborador.Source = BytesToImagen(fotoBytes);
                    ImgFotoColaborador.Visibility = Visibility.Visible;
                    TxtAvatarInicial.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ImgFotoColaborador.Visibility = Visibility.Collapsed;
                    TxtAvatarInicial.Visibility = Visibility.Visible;
                }

                // Habilitar botones acción
                BtnEditar.IsEnabled = true;
                BtnEliminar.IsEnabled = true;

                // Aseguramos que se vea el panel detalle (no el formulario)
                MostrarDetalle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el panel lateral:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // FILTROS POR TAB
        // ═════════════════════════════════════════════════════════════════
        private void FilterTab_Click(object sender, RoutedEventArgs e)
        {
            BtnTabTodos.IsEnabled = true;
            BtnTabAdministradores.IsEnabled = true;
            BtnTabOperadores.IsEnabled = true;
            BtnTabEmpleados.IsEnabled = true;

            var btn = sender as Button;
            if (btn == null) return;
            btn.IsEnabled = false;

            _filtroRol = btn.Name switch
            {
                "BtnTabAdministradores" => "Administrador",
                "BtnTabOperadores" => "Operador",
                "BtnTabEmpleados" => "Empleado",
                _ => ""
            };

            AplicarFiltros();
        }

        // ═════════════════════════════════════════════════════════════════
        // BÚSQUEDA EN VIVO
        // ═════════════════════════════════════════════════════════════════
        private void TxtBuscarColaborador_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filtroBusqueda = TxtBuscarColaborador.Text.Trim();
            AplicarFiltros();
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — NUEVO COLABORADOR
        // ═════════════════════════════════════════════════════════════════
        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            _modoEdicion = false;
            _cedulaEnEdicion = "";
            _fotoSeleccionada = null;
            _fotoFueModificada = false;

            TxtFormTitulo.Text = "Nuevo Colaborador";
            BtnGuardar.Content = "Guardar Colaborador";
            LblPassword.Text = "Contraseña *";
            TxtPasswordHint.Visibility = Visibility.Collapsed;
            TxtFCedula.IsReadOnly = false;
            TxtFCedula.Opacity = 1.0;

            LimpiarFormulario();
            MostrarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — EDITAR COLABORADOR
        // ═════════════════════════════════════════════════════════════════
        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (DgColaboradores.SelectedItem == null) return;

            dynamic fila = DgColaboradores.SelectedItem;

            _modoEdicion = true;
            _cedulaEnEdicion = fila.DocumentoIdentidad ?? "";
            _fotoSeleccionada = fila._FotoBytes as byte[];
            _fotoFueModificada = false;

            TxtFormTitulo.Text = "Editar Colaborador";
            BtnGuardar.Content = "Actualizar Colaborador";
            LblPassword.Text = "Contraseña";
            TxtPasswordHint.Visibility = Visibility.Visible;

            // Cédula no editable en modo edición (es la clave primaria)
            TxtFCedula.IsReadOnly = true;
            TxtFCedula.Opacity = 0.6;

            // Pre-poblar campos
            TxtFCedula.Text = fila.DocumentoIdentidad ?? "";
            TxtFNombres.Text = fila._Nombres ?? "";
            TxtFApellidos.Text = fila._Apellidos ?? "";
            TxtFCargo.Text = fila.Cargo ?? "";
            TxtFCorreo.Text = fila.CorreoCorporativo ?? "";
            TxtFUsuario.Text = fila._UsuarioApp ?? "";
            PbFPassword.Clear();

            // Fecha de ingreso
            DpFIngreso.SelectedDate = fila._FechaIngresoCruda as DateTime?;

            // Estado
            string estadoRaw = ConvertirEstado(fila._EstadoRaw);
            CbFEstado.SelectedIndex = estadoRaw == "Activo" ? 0 : 1;

            // Seleccionar combos por ID
            SeleccionarComboItem(CbFDepartamento, "DepartamentoID", fila._DepartamentoID);
            SeleccionarComboItem(CbFUbicacion, "UbicacionID", fila._UbicacionID);
            SeleccionarComboItem(CbFPerfil, "PerfilID", fila._PerfilID);

            // Foto preview
            CargarFotoEnPreview(_fotoSeleccionada);

            MostrarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — SELECCIONAR FOTO (explorador de archivos)
        // ═════════════════════════════════════════════════════════════════
        private void BtnSeleccionarFoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar foto del colaborador",
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp|Todos los archivos|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _fotoSeleccionada = File.ReadAllBytes(dialog.FileName);
                    _fotoFueModificada = true;
                    CargarFotoEnPreview(_fotoSeleccionada);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo cargar la imagen:\n{ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void CargarFotoEnPreview(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                ImgFotoPreview.Source = BytesToImagen(bytes);
                ImgFotoPreview.Visibility = Visibility.Visible;
                TxtFotoPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImgFotoPreview.Visibility = Visibility.Collapsed;
                TxtFotoPlaceholder.Visibility = Visibility.Visible;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — GUARDAR (crear o actualizar)
        // ═════════════════════════════════════════════════════════════════
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ── Recolección de campos ─────────────────────────────────
                string cedula = TxtFCedula.Text.Trim();
                string nombres = TxtFNombres.Text.Trim();
                string apellidos = TxtFApellidos.Text.Trim();
                string cargo = TxtFCargo.Text.Trim();
                string correo = TxtFCorreo.Text.Trim();
                string usuario = TxtFUsuario.Text.Trim();
                string password = PbFPassword.Password;
                string estado = (CbFEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Activo";

                DateTime? fechaIngreso = DpFIngreso.SelectedDate;

                int departamentoId = ObtenerIdCombo(CbFDepartamento, "DepartamentoID");
                int ubicacionId = ObtenerIdCombo(CbFUbicacion, "UbicacionID");
                int perfilId = ObtenerIdCombo(CbFPerfil, "PerfilID");

                // ── Validaciones básicas ──────────────────────────────────
                if (string.IsNullOrWhiteSpace(cedula))
                {
                    Alerta("La cédula es obligatoria."); TxtFCedula.Focus(); return;
                }
                if (string.IsNullOrWhiteSpace(nombres))
                {
                    Alerta("Los nombres son obligatorios."); TxtFNombres.Focus(); return;
                }
                if (string.IsNullOrWhiteSpace(apellidos))
                {
                    Alerta("Los apellidos son obligatorios."); TxtFApellidos.Focus(); return;
                }
                if (string.IsNullOrWhiteSpace(cargo))
                {
                    Alerta("El cargo es obligatorio."); TxtFCargo.Focus(); return;
                }
                if (departamentoId <= 0)
                {
                    Alerta("Selecciona un departamento."); CbFDepartamento.Focus(); return;
                }
                if (ubicacionId <= 0)
                {
                    Alerta("Selecciona una ubicación."); CbFUbicacion.Focus(); return;
                }
                if (perfilId <= 0)
                {
                    Alerta("Selecciona un perfil."); CbFPerfil.Focus(); return;
                }
                if (fechaIngreso == null)
                {
                    Alerta("Selecciona la fecha de ingreso."); DpFIngreso.Focus(); return;
                }
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    Alerta("El usuario de la app es obligatorio."); TxtFUsuario.Focus(); return;
                }
                if (!_modoEdicion && string.IsNullOrWhiteSpace(password))
                {
                    Alerta("La contraseña es obligatoria para un nuevo colaborador."); PbFPassword.Focus(); return;
                }

                // ── Foto: usar la seleccionada o null ─────────────────────
                byte[] fotoFinal = _fotoSeleccionada;

                bool ok;

                if (_modoEdicion)
                {
                    // En edición: si no se escribió nueva contraseña, pasamos la existente vacía
                    // El método ModificarColaborador espera el texto plano (será hasheado de nuevo)
                    // Si password está vacío, pasamos un placeholder para que no cambie
                    // NOTA: si tu BD requiere siempre actualizar el hash, se recomienda una lógica
                    //       adicional en AccesoDatos; aquí enviamos lo que el usuario escribió.
                    string passParaActualizar = string.IsNullOrWhiteSpace(password)
                        ? "##SIN_CAMBIO##"   // Valor especial — manejar en AccesoDatos si se desea
                        : password;

                    ok = _cnColaboradores.EditarColaborador(
                        cedula, nombres, apellidos, correo,
                        departamentoId, ubicacionId,
                        fechaIngreso.Value, estado, perfilId,
                        usuario, passParaActualizar,
                        _fotoFueModificada ? fotoFinal : null, // null = no cambiar foto
                        cargo);

                    MostrarResultado(ok,
                        "Colaborador actualizado correctamente.",
                        "No se pudo actualizar el colaborador.");
                }
                else
                {
                    var resultado = _colaboradorDominio.RegistrarColaborador(
                        cedula, nombres, apellidos, correo,
                        departamentoId, ubicacionId,
                        fechaIngreso.Value, estado, perfilId,
                        usuario, password, fotoFinal, cargo);

                    ok = resultado.Exitoso;
                    MostrarResultado(ok, resultado.Mensaje, resultado.Mensaje);
                }

                if (ok)
                {
                    OcultarFormulario();
                    CargarDatos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al guardar:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                                "Error del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — ELIMINAR COLABORADOR
        // ═════════════════════════════════════════════════════════════════
        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (DgColaboradores.SelectedItem == null) return;

            dynamic fila = DgColaboradores.SelectedItem;
            string cedula = fila.DocumentoIdentidad ?? "";
            string nombre = fila.NombreCompleto ?? cedula;

            var confirmacion = MessageBox.Show(
                $"¿Eliminar permanentemente al colaborador:\n\n«{nombre}» (ID: {cedula})?\n\nEsta acción es irreversible.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmacion != MessageBoxResult.Yes) return;

            try
            {
                bool ok = _cnColaboradores.EliminarColaborador(cedula);
                MostrarResultado(ok,
                    "Colaborador eliminado correctamente.",
                    "No se pudo completar la eliminación.");

                if (ok) CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar:\n{ex.Message}",
                                "Error de Restricción", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CANCELAR FORMULARIO
        // ═════════════════════════════════════════════════════════════════
        private void BtnCancelarForm_Click(object sender, RoutedEventArgs e)
            => OcultarFormulario();

        // ═════════════════════════════════════════════════════════════════
        // EXPORTAR EXCEL (stub)
        // ═════════════════════════════════════════════════════════════════
        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de exportación a Excel pendiente de implementar.",
                            "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ═════════════════════════════════════════════════════════════════
        // MODO CLARO / OSCURO
        // Llamar desde Dashboard.xaml.cs al cambiar el tema:
        //   miViewer.AplicarTema(esModoClaro: true/false);
        // ═════════════════════════════════════════════════════════════════
        public void AplicarTema(bool modoClaro)
        {
            _isDarkMode = !modoClaro;
            var bc = new BrushConverter();

            if (modoClaro)
            {
                // ── Fondo raíz del UserControl ────────────────────────────────
                RootGrid.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");

                // ── Panel lateral ─────────────────────────────────────────────
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");

                // ── Textos del panel lateral ──────────────────────────────────
                TxtPanelNombre.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelCargo.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtPanelCedula.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelCorreo.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtPanelDepartamento.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelFechaIngreso.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtAvatarInicial.Foreground = (SolidColorBrush)bc.ConvertFromString("#4B93FF");

                // ── Títulos y buscador ────────────────────────────────────────
                TxtTituloSeccion.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtBuscarColaborador.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");

                // ── Contenedor del DataGrid ───────────────────────────────────
                GridContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                GridContainerBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");

                // ── DataGrid fondo y filas (sobreescribir recursos locales) ───
                DgColaboradores.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                DgColaboradores.RowBackground = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                DgColaboradores.AlternatingRowBackground = (SolidColorBrush)bc.ConvertFromString("#E2EAFF");
                DgColaboradores.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                DgColaboradores.HorizontalGridLinesBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                DgColaboradores.VerticalGridLinesBrush = Brushes.Transparent;

                // Sobreescribir el estilo de filas en tiempo de ejecución
                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#EDF2FF")));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#1E3A5F")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderBrushProperty,
                    (SolidColorBrush)bc.ConvertFromString("#C3D3F0")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty,
                    new Thickness(0, 0, 0, 1)));

                // Trigger hover
                var triggerHover = new Trigger
                {
                    Property = DataGridRow.IsMouseOverProperty,
                    Value = true
                };
                triggerHover.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#D6E4FF")));
                rowStyle.Triggers.Add(triggerHover);

                // Trigger seleccionado
                var triggerSelected = new Trigger
                {
                    Property = DataGridRow.IsSelectedProperty,
                    Value = true
                };
                triggerSelected.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#BFCFE8")));
                triggerSelected.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#1E3A5F")));
                rowStyle.Triggers.Add(triggerSelected);

                DgColaboradores.RowStyle = rowStyle;

                // Sobreescribir estilo de encabezados de columna
                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#EDF2FF")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#4A6080")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty,
                    FontWeights.SemiBold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 12.0));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty,
                    new Thickness(12, 12, 12, 12)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty,
                    new Thickness(0, 0, 0, 1)));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty,
                    (SolidColorBrush)bc.ConvertFromString("#C3D3F0")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty,
                    HorizontalAlignment.Center));
                DgColaboradores.ColumnHeaderStyle = headerStyle;

                // ── Paginador ─────────────────────────────────────────────────
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
            }
            else
            {
                // ════════════ MODO OSCURO (restaurar) ════════════

                RootGrid.Background = Brushes.Transparent;

                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#090924");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#151538");

                TxtPanelNombre.Foreground = Brushes.White;
                TxtPanelCargo.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                TxtPanelCedula.Foreground = Brushes.White;
                TxtPanelCorreo.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                TxtPanelDepartamento.Foreground = Brushes.White;
                TxtPanelFechaIngreso.Foreground = Brushes.White;
                TxtAvatarInicial.Foreground = (SolidColorBrush)bc.ConvertFromString("#3F3F6B");

                TxtTituloSeccion.Foreground = Brushes.White;
                TxtBuscarColaborador.Foreground = Brushes.White;

                GridContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#090924");
                GridContainerBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#151538");

                // ── Restaurar DataGrid oscuro ─────────────────────────────────
                DgColaboradores.Background = Brushes.Transparent;
                DgColaboradores.RowBackground = Brushes.Transparent;
                DgColaboradores.AlternatingRowBackground = Brushes.Transparent;
                DgColaboradores.Foreground = Brushes.White;
                DgColaboradores.HorizontalGridLinesBrush = Brushes.Transparent;
                DgColaboradores.VerticalGridLinesBrush = Brushes.Transparent;

                // Restaurar estilo de filas original
                var rowStyleDark = new Style(typeof(DataGridRow));
                rowStyleDark.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.Transparent));
                rowStyleDark.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Brushes.White));
                rowStyleDark.Setters.Add(new Setter(DataGridRow.BorderBrushProperty,
                    (SolidColorBrush)bc.ConvertFromString("#151538")));
                rowStyleDark.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty,
                    new Thickness(0, 0, 0, 1)));

                var triggerHoverDark = new Trigger
                {
                    Property = DataGridRow.IsMouseOverProperty,
                    Value = true
                };
                triggerHoverDark.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#0D0D2D")));
                rowStyleDark.Triggers.Add(triggerHoverDark);

                var triggerSelectedDark = new Trigger
                {
                    Property = DataGridRow.IsSelectedProperty,
                    Value = true
                };
                triggerSelectedDark.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#1F1F45")));
                triggerSelectedDark.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#E89A24")));
                rowStyleDark.Triggers.Add(triggerSelectedDark);

                DgColaboradores.RowStyle = rowStyleDark;

                // Restaurar encabezados oscuros
                var headerStyleDark = new Style(typeof(DataGridColumnHeader));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.Transparent));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty,
                    (SolidColorBrush)bc.ConvertFromString("#A0A0B8")));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty,
                    FontWeights.SemiBold));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 12.0));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty,
                    new Thickness(12, 12, 12, 12)));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty,
                    new Thickness(0, 0, 0, 1)));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty,
                    (SolidColorBrush)bc.ConvertFromString("#1F1F45")));
                headerStyleDark.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty,
                    HorizontalAlignment.Center));
                DgColaboradores.ColumnHeaderStyle = headerStyleDark;

                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // HELPERS DE UI
        // ═════════════════════════════════════════════════════════════════
        private void MostrarFormulario()
        {
            PanelDetalle.Visibility = Visibility.Collapsed;
            PanelFormulario.Visibility = Visibility.Visible;
            BtnEditar.IsEnabled = false;
            BtnEliminar.IsEnabled = false;
        }

        private void OcultarFormulario()
        {
            PanelFormulario.Visibility = Visibility.Collapsed;
            PanelDetalle.Visibility = Visibility.Visible;
            LimpiarFormulario();
        }

        private void MostrarDetalle()
        {
            PanelFormulario.Visibility = Visibility.Collapsed;
            PanelDetalle.Visibility = Visibility.Visible;
        }

        private void LimpiarFormulario()
        {
            TxtFCedula.Clear();
            TxtFNombres.Clear();
            TxtFApellidos.Clear();
            TxtFCargo.Clear();
            TxtFCorreo.Clear();
            TxtFUsuario.Clear();
            PbFPassword.Clear();
            CbFDepartamento.SelectedIndex = -1;
            CbFUbicacion.SelectedIndex = -1;
            CbFPerfil.SelectedIndex = -1;
            CbFEstado.SelectedIndex = 0;
            DpFIngreso.SelectedDate = DateTime.Today;

            _fotoSeleccionada = null;
            _fotoFueModificada = false;
            ImgFotoPreview.Source = null;
            ImgFotoPreview.Visibility = Visibility.Collapsed;
            TxtFotoPlaceholder.Visibility = Visibility.Visible;
        }

        private void LimpiarPanelLateral()
        {
            TxtPanelNombre.Text = "Selecciona un colaborador";
            TxtPanelCargo.Text = "—";
            TxtPanelPerfil.Text = "—";
            TxtPanelCedula.Text = "—";
            TxtPanelCorreo.Text = "—";
            TxtPanelDepartamento.Text = "—";
            TxtPanelEstado.Text = "—";
            TxtPanelFechaIngreso.Text = "—";
            TxtAvatarInicial.Text = "?";
            TxtAvatarInicial.Visibility = Visibility.Visible;
            ImgFotoColaborador.Visibility = Visibility.Collapsed;
            ImgFotoColaborador.Source = null;
            BtnEditar.IsEnabled = false;
            BtnEliminar.IsEnabled = false;
        }

        // ── Seleccionar un item de ComboBox por valor de columna ─────────
        private void SeleccionarComboItem(ComboBox combo, string columna, int valor)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboItem item && Convert.ToInt32(item.Value) == valor)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = -1;
        }

        // ── Obtener el ID seleccionado de un ComboBox ────────────────────
        private int ObtenerIdCombo(ComboBox combo, string columna)
        {
            if (combo.SelectedItem is ComboItem item)
                return Convert.ToInt32(item.Value);
            return -1;
        }

        // ── Convertir el campo Estado (bit/bool/string) a texto ─────────
        private string ConvertirEstado(object valor)
        {
            if (valor == null || valor == DBNull.Value) return "—";
            if (valor is bool b) return b ? "Activo" : "Inactivo";
            string s = valor.ToString().ToLower();
            return (s == "true" || s == "1" || s == "activo") ? "Activo" : "Inactivo";
        }

        // ── Iniciales del nombre ─────────────────────────────────────────
        private string ObtenerIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return "?";
            var partes = nombreCompleto.Trim().Split(' ');
            return partes.Length >= 2
                ? $"{partes[0][0]}{partes[1][0]}".ToUpper()
                : partes[0][0].ToString().ToUpper();
        }

        // ── Convertir byte[] a BitmapImage ───────────────────────────────
        private BitmapImage BytesToImagen(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }

        // ── Mensajes rápidos ─────────────────────────────────────────────
        private void Alerta(string msg)
            => MessageBox.Show(msg, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);

        private void MostrarResultado(bool ok, string msgOk, string msgError)
            => MessageBox.Show(ok ? msgOk : msgError,
                               ok ? "Éxito" : "Error",
                               MessageBoxButton.OK,
                               ok ? MessageBoxImage.Information : MessageBoxImage.Error);

        public class ComboItem
        {
            public string Display { get; set; }
            public object Value { get; set; }
            public override string ToString() => Display;
        }


    }
}
