using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using static AccesoDatos.ConexionSql;

namespace Presentación
{
    public partial class GestorContrasenas : UserControl
    {
        // ── Capa de negocio ───────────────────────────────────────────────
        private readonly GestorCredencialesDominio _dominio = new();

        // ── Estado general ────────────────────────────────────────────────
        private int _colaboradorId;          // Se inyecta desde el Dashboard
        private DataTable _tablaTodos;       // Tabla completa traída de BD

        // ── Paginación ────────────────────────────────────────────────────
        private List<CredencialVM> _registrosFiltrados = new();
        private int _paginaActual = 1;
        private const int _porPagina = 10;
        private int _totalPaginas = 1;

        // ── Filtros activos ───────────────────────────────────────────────
        private string _filtroBusqueda = "";
        private string _filtroCategoria = "";   // "" = Todas

        // ── Estado del panel lateral ──────────────────────────────────────
        private CredencialVM? _seleccionada;
        private bool _modoEdicion = false;
        private bool _passwordVisible = false;

        // ── Constantes de tabs ────────────────────────────────────────────
        private const string TAB_CORREO = "Correo Electrónico";
        private const string TAB_SISTEMA = "Sistema Interno";
        private const string TAB_VENCIDAS = "PROXIMAS";
        private bool _isDarkMode = true;
        private bool _esModoClaro = false;

        // ═════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═════════════════════════════════════════════════════════════════

        public GestorContrasenas()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor con ColaboradorID inyectado desde el Dashboard.
        /// Usar este en producción: NavegaA(new GestorContrasenas(colaboradorId))
        /// </summary>
        public GestorContrasenas(int colaboradorId) : this()
        {
            _colaboradorId = colaboradorId;
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═════════════════════════════════════════════════════════════════

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Si _colaboradorId no fue inyectado (diseñador / demo), usar variable global
            if (_colaboradorId <= 0)
                _colaboradorId = ObtenerColaboradorIdDeSesion();

            CargarDatos();
        }

        /// <summary>
        /// Obtiene el ColaboradorID de la sesión activa.
        /// Ajustar según cómo manejes la sesión en tu proyecto.
        /// </summary>
        private int ObtenerColaboradorIdDeSesion()
        {
            // Ejemplo: si guardas el ID en VariablesGlobales, usar ese valor.
            // Por ahora retorna 0 para que el diseñador no explote.
            return 0;
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA Y TRANSFORMACIÓN DE DATOS
        // ═════════════════════════════════════════════════════════════════

        private void CargarDatos()
        {
            try
            {
                _tablaTodos = _dominio.ListarCredenciales(_colaboradorId);
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar credenciales: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltros()
        {
            if (_tablaTodos == null) return;

            IEnumerable<DataRow> filas = _tablaTodos.AsEnumerable();

            // Filtro de búsqueda
            if (!string.IsNullOrWhiteSpace(_filtroBusqueda))
            {
                string t = _filtroBusqueda.ToLower();
                filas = filas.Where(r =>
                    (r["NombreServicio"]?.ToString() ?? "").ToLower().Contains(t) ||
                    (r["Usuario"]?.ToString() ?? "").ToLower().Contains(t) ||
                    (r["Categoria"]?.ToString() ?? "").ToLower().Contains(t) ||
                    (r["URL_Acceso"]?.ToString() ?? "").ToLower().Contains(t));
            }

            // Filtro de tab de categoría
            if (_filtroCategoria == TAB_VENCIDAS)
            {
                // Próximas a vencer: fecha ≤ 30 días desde hoy o ya vencidas
                DateTime limite = DateTime.Today.AddDays(30);
                filas = filas.Where(r =>
                {
                    if (r["FechaVencimientoClave"] == DBNull.Value) return false;
                    DateTime venc = Convert.ToDateTime(r["FechaVencimientoClave"]);
                    return venc <= limite;
                });
            }
            else if (!string.IsNullOrEmpty(_filtroCategoria))
            {
                filas = filas.Where(r =>
                    (r["Categoria"]?.ToString() ?? "")
                        .Equals(_filtroCategoria, StringComparison.OrdinalIgnoreCase));
            }

            // Proyectar a ViewModel
            _registrosFiltrados = filas.Select(r => new CredencialVM
            {
                CredencialID = Convert.ToInt32(r["CredencialID"]),
                NombreServicio = r["NombreServicio"]?.ToString() ?? "",
                Usuario = r["Usuario"]?.ToString() ?? "",

                // CORRECCIÓN: Conversión segura de VARBINARY (byte[]) a string legible
                ContrasenaCifrada = r["ContrasenaCifrada"] != DBNull.Value
                    ? System.Text.Encoding.UTF8.GetString((byte[])r["ContrasenaCifrada"])
                    : "",

                Categoria = r["Categoria"]?.ToString() ?? "—",
                UrlAcceso = r["URL_Acceso"]?.ToString() ?? "",
                NotasSeguras = r["NotasSeguras"]?.ToString() ?? "",
                FechaVencimiento = r["FechaVencimientoClave"] != DBNull.Value
                                        ? (DateTime?)Convert.ToDateTime(r["FechaVencimientoClave"])
                                        : null,
                UltimaActualizacion = r["UltimaActualizacion"] != DBNull.Value
                                        ? Convert.ToDateTime(r["UltimaActualizacion"])
                                        : DateTime.MinValue,
            }).ToList();

            // Actualizar badges de los tabs
            ActualizarBadgesTabs();

            _paginaActual = 1;
            RenderizarPagina();
        }

        private void ActualizarBadgesTabs()
        {
            if (_tablaTodos == null) return;

            var todas = _tablaTodos.AsEnumerable();
            BtnTabTodas.Tag = todas.Count().ToString();
            BtnTabCorreo.Tag = todas.Count(r => (r["Categoria"]?.ToString() ?? "") == TAB_CORREO).ToString();
            BtnTabSistema.Tag = todas.Count(r => (r["Categoria"]?.ToString() ?? "") == TAB_SISTEMA).ToString();

            DateTime limite = DateTime.Today.AddDays(30);
            BtnTabVencidas.Tag = todas.Count(r =>
            {
                if (r["FechaVencimientoClave"] == DBNull.Value) return false;
                return Convert.ToDateTime(r["FechaVencimientoClave"]) <= limite;
            }).ToString();
        }

        // ═════════════════════════════════════════════════════════════════
        // PAGINACIÓN
        // ═════════════════════════════════════════════════════════════════

        private void RenderizarPagina()
        {
            int total = _registrosFiltrados.Count;
            _totalPaginas = (int)Math.Ceiling(total / (double)_porPagina);
            if (_totalPaginas < 1) _totalPaginas = 1;
            if (_paginaActual > _totalPaginas) _paginaActual = _totalPaginas;

            var pagina = _registrosFiltrados
                .Skip((_paginaActual - 1) * _porPagina)
                .Take(_porPagina)
                .ToList();

            DgCredenciales.ItemsSource = pagina;

            TxtInfoPagina.Text = $"Página {_paginaActual} de {_totalPaginas}";
            TxtContadorRegistros.Text = $"Mostrando {pagina.Count} de {total} credenciales";

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
                int numPag = i;
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
                btn.Click += (s, e) => { _paginaActual = numPag; RenderizarPagina(); };
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
        // SELECCIÓN EN EL DATAGRID
        // ═════════════════════════════════════════════════════════════════

        private void DgCredenciales_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgCredenciales.SelectedItem is CredencialVM vm)
            {
                _seleccionada = vm;
                _passwordVisible = false;
                MostrarDetalle(vm);
            }
            else
            {
                LimpiarPanelLateral();
            }
        }

        private void MostrarDetalle(CredencialVM vm)
        {
            // Asegurar que mostramos el panel detalle, no el formulario
            PanelDetalle.Visibility = Visibility.Visible;
            PanelFormulario.Visibility = Visibility.Collapsed;

            TxtIconoServicio.Text = IconoPorCategoria(vm.Categoria);
            TxtPanelServicio.Text = vm.NombreServicio;
            TxtPanelCategoria.Text = vm.Categoria;
            TxtPanelUrl.Text = string.IsNullOrWhiteSpace(vm.UrlAcceso) ? "—" : vm.UrlAcceso;
            TxtPanelUsuario.Text = vm.Usuario;
            TxtPanelNotas.Text = string.IsNullOrWhiteSpace(vm.NotasSeguras) ? "—" : vm.NotasSeguras;
            TxtPanelPassword.Text = "••••••••";
            TxtPanelActualizacion.Text = vm.UltimaActualizacion != DateTime.MinValue
                ? vm.UltimaActualizacion.ToString("dd/MM/yyyy HH:mm")
                : "—";

            // Badge de vencimiento
            if (vm.FechaVencimiento.HasValue)
            {
                int diasRestantes = (vm.FechaVencimiento.Value - DateTime.Today).Days;
                if (diasRestantes < 0)
                    TxtPanelVencimiento.Text = $"Vencida hace {Math.Abs(diasRestantes)} días";
                else if (diasRestantes == 0)
                    TxtPanelVencimiento.Text = "Vence hoy";
                else if (diasRestantes <= 30)
                    TxtPanelVencimiento.Text = $"Vence en {diasRestantes} días";
                else
                    TxtPanelVencimiento.Text = $"Vence: {vm.FechaVencimiento.Value:dd/MM/yyyy}";
            }
            else
            {
                TxtPanelVencimiento.Text = "Sin vencimiento";
            }

            BtnEditar.IsEnabled = true;
            BtnEliminar.IsEnabled = true;
        }

        // ═════════════════════════════════════════════════════════════════
        // MOSTRAR / OCULTAR CONTRASEÑA EN EL PANEL LATERAL
        // ═════════════════════════════════════════════════════════════════

        private void BtnMostrarPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                TxtPanelPassword.Text = _dominio.Descifrar(_seleccionada.ContrasenaCifrada);
                EyeIconPanel.Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x9A, 0x24));
            }
            else
            {
                TxtPanelPassword.Text = "••••••••";
                EyeIconPanel.Fill = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8));
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // FILTROS POR TAB
        // ═════════════════════════════════════════════════════════════════

        private void FilterTab_Click(object sender, RoutedEventArgs e)
        {
            BtnTabTodas.IsEnabled = true;
            BtnTabCorreo.IsEnabled = true;
            BtnTabSistema.IsEnabled = true;
            BtnTabVencidas.IsEnabled = true;

            if (sender is not Button btn) return;
            btn.IsEnabled = false;

            _filtroCategoria = btn.Name switch
            {
                "BtnTabCorreo" => TAB_CORREO,
                "BtnTabSistema" => TAB_SISTEMA,
                "BtnTabVencidas" => TAB_VENCIDAS,
                _ => ""
            };

            _paginaActual = 1;
            AplicarFiltros();
        }

        // ═════════════════════════════════════════════════════════════════
        // BÚSQUEDA
        // ═════════════════════════════════════════════════════════════════

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filtroBusqueda = TxtBuscar.Text.Trim();
            _paginaActual = 1;
            AplicarFiltros();
        }

        // ═════════════════════════════════════════════════════════════════
        // NUEVA CREDENCIAL
        // ═════════════════════════════════════════════════════════════════

        private void BtnNueva_Click(object sender, RoutedEventArgs e)
        {
            _modoEdicion = false;
            _seleccionada = null;
            TxtFormTitulo.Text = "Nueva Credencial";
            BtnGuardar.Content = "Guardar Credencial";
            LimpiarFormulario();
            MostrarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // EDITAR
        // ═════════════════════════════════════════════════════════════════

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            _modoEdicion = true;
            TxtFormTitulo.Text = "Editar Credencial";
            BtnGuardar.Content = "Actualizar Credencial";

            // Pre-poblar campos del formulario
            TxtFServicio.Text = _seleccionada.NombreServicio;
            TxtFUrl.Text = _seleccionada.UrlAcceso;
            TxtFUsuario.Text = _seleccionada.Usuario;
            PbFPassword.Password = "";   // No revelamos la contraseña actual al editar
            TxtFNotas.Text = _seleccionada.NotasSeguras;
            DpFVencimiento.SelectedDate = _seleccionada.FechaVencimiento;

            // Seleccionar categoría en el combo
            foreach (ComboBoxItem item in CbFCategoria.Items)
            {
                if (item.Content.ToString() == _seleccionada.Categoria)
                {
                    CbFCategoria.SelectedItem = item;
                    break;
                }
            }

            MostrarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // GUARDAR (crear o actualizar)
        // ═════════════════════════════════════════════════════════════════

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string servicio = TxtFServicio.Text.Trim();
                string url = TxtFUrl.Text.Trim();
                string usuario = TxtFUsuario.Text.Trim();
                string password = PbFPassword.Password;
                string categoria = (CbFCategoria.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                string notas = TxtFNotas.Text.Trim();
                DateTime? venc = DpFVencimiento.SelectedDate;

                ResultadoCredencial resultado;

                if (_modoEdicion && _seleccionada != null)
                {
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        resultado = ActualizarSinCambiarPassword(_seleccionada.CredencialID,
                            servicio, url, usuario, categoria, venc, notas);
                    }
                    else
                    {
                        resultado = _dominio.Editar(
                            _seleccionada.CredencialID, servicio, url, usuario,
                            password, categoria, null, venc, notas);
                    }
                }
                else
                {
                    // OJO AQUÍ: Asegúrate de que _colaboradorId tenga un valor válido
                    resultado = _dominio.Crear(
                        servicio, url, usuario, password, categoria,
                        _colaboradorId, null, venc, notas);
                }

                MessageBox.Show(resultado.Mensaje,
                    resultado.Exitoso ? "Éxito" : "Error de Validación",
                    MessageBoxButton.OK,
                    resultado.Exitoso ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (resultado.Exitoso)
                {
                    OcultarFormulario();
                    CargarDatos();
                }
            }
            catch (Exception ex)
            {
                // Esto te mostrará el error real en una ventana emergente para saber qué falla
                MessageBox.Show($"Ocurrió un error técnico:\n\n{ex.Message}\n\nDetalles:\n{ex.InnerException?.Message}",
                    "Error del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Actualiza todos los campos excepto la contraseña (la conserva cifrada).
        /// </summary>
        private ResultadoCredencial ActualizarSinCambiarPassword(
            int credencialId, string servicio, string url, string usuario,
            string categoria, DateTime? venc, string notas)
        {
            // Llamamos al AccesoDatos directamente para pasar el valor cifrado ya existente
            var ad = new AccesoDatos.GestorCredencialesAccesoDatos();
            bool ok = ad.ActualizarCredencial(
                credencialId, servicio, url, usuario,
                _seleccionada!.ContrasenaCifrada,   // conservar cifrada original
                categoria, null, venc, notas);

            return new ResultadoCredencial
            {
                Exitoso = ok,
                Mensaje = ok ? "Credencial actualizada correctamente." : "No se pudo actualizar."
            };
        }

        // ═════════════════════════════════════════════════════════════════
        // ELIMINAR
        // ═════════════════════════════════════════════════════════════════

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            var confirmar = MessageBox.Show(
                $"¿Eliminar permanentemente la credencial de «{_seleccionada.NombreServicio}»?\n\nEsta acción es irreversible.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar != MessageBoxResult.Yes) return;

            var resultado = _dominio.Eliminar(_seleccionada.CredencialID, _colaboradorId);

            MessageBox.Show(resultado.Mensaje,
                resultado.Exitoso ? "Éxito" : "Error",
                MessageBoxButton.OK,
                resultado.Exitoso ? MessageBoxImage.Information : MessageBoxImage.Error);

            if (resultado.Exitoso)
                CargarDatos();
        }

        // ═════════════════════════════════════════════════════════════════
        // CANCELAR FORMULARIO
        // ═════════════════════════════════════════════════════════════════

        private void BtnCancelarForm_Click(object sender, RoutedEventArgs e)
        {
            OcultarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // GENERAR CONTRASEÑA SEGURA
        // ═════════════════════════════════════════════════════════════════

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%&*?";
            var rng = new Random(Guid.NewGuid().GetHashCode());
            string nuevaPass = new string(Enumerable.Repeat(chars, 16)
                                    .Select(s => s[rng.Next(s.Length)]).ToArray());

            PbFPassword.Password = nuevaPass;

            MessageBox.Show($"Contraseña generada:\n\n{nuevaPass}\n\nCópiala antes de guardar.",
                "Contraseña Segura", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void LimpiarFormulario()
        {
            TxtFServicio.Clear();
            TxtFUrl.Clear();
            TxtFUsuario.Clear();
            PbFPassword.Clear();
            TxtFNotas.Clear();
            CbFCategoria.SelectedIndex = -1;
            DpFVencimiento.SelectedDate = null;
        }

        private void LimpiarPanelLateral()
        {
            _seleccionada = null;
            _passwordVisible = false;
            TxtIconoServicio.Text = "🔑";
            TxtPanelServicio.Text = "Selecciona una credencial";
            TxtPanelCategoria.Text = "—";
            TxtPanelUrl.Text = "—";
            TxtPanelUsuario.Text = "—";
            TxtPanelPassword.Text = "••••••••";
            TxtPanelNotas.Text = "—";
            TxtPanelVencimiento.Text = "—";
            TxtPanelActualizacion.Text = "—";
            BtnEditar.IsEnabled = false;
            BtnEliminar.IsEnabled = false;

            PanelDetalle.Visibility = Visibility.Visible;
            PanelFormulario.Visibility = Visibility.Collapsed;
        }

        private static string IconoPorCategoria(string categoria) => categoria switch
        {
            "Correo Electrónico" => "✉",
            "Redes Sociales" => "🌐",
            "Sistema Interno" => "🖥",
            "Banco / Finanzas" => "🏦",
            "Desarrollo / DevOps" => "⚙",
            "VPN / Acceso Remoto" => "🔒",
            _ => "🔑"
        };

        public void AplicarTema(bool modoClaro)
        {
            _isDarkMode = !modoClaro;
            var bc = new BrushConverter();

            if (modoClaro)
            {
                RootGrid.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                TxtPanelServicio.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelCategoria.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtPanelUsuario.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelUrl.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtPanelNotas.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtPanelActualizacion.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelPassword.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                BadgeVencimiento.Background = (SolidColorBrush)bc.ConvertFromString("#D6E4FF");
                BusquedaBorder.Background = (SolidColorBrush)bc.ConvertFromString("#E8F0FF");
                BusquedaBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#BFCFE8");
                TxtBuscar.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtTitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                DgCredenciales.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                DgCredenciales.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");

                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#EDF2FF")));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#1E3A5F")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#C3D3F0")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
                var th = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
                th.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#D6E4FF")));
                rowStyle.Triggers.Add(th);
                var ts = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
                ts.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#BFCFE8")));
                ts.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#1E3A5F")));
                rowStyle.Triggers.Add(ts);
                DgCredenciales.RowStyle = rowStyle;

                var hdr = new Style(typeof(DataGridColumnHeader));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#EDF2FF")));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#4A6080")));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#C3D3F0")));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
                DgCredenciales.ColumnHeaderStyle = hdr;
            }
            else
            {
                RootGrid.Background = Brushes.Transparent;
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                TxtPanelServicio.Foreground = Brushes.White;
                TxtPanelCategoria.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtPanelUsuario.Foreground = Brushes.White;
                TxtPanelUrl.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtPanelNotas.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtPanelActualizacion.Foreground = Brushes.White;
                TxtPanelPassword.Foreground = Brushes.White;
                BadgeVencimiento.Background = (SolidColorBrush)bc.ConvertFromString("#0e3560");
                BusquedaBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                BusquedaBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#1a4a7a");
                TxtBuscar.Foreground = Brushes.White;
                TxtTitulo.Foreground = Brushes.White;
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                DgCredenciales.Background = Brushes.Transparent;
                DgCredenciales.Foreground = Brushes.White;

                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.Transparent));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Brushes.White));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#0d3a5c")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
                var tho = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
                tho.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#0e3560")));
                rowStyle.Triggers.Add(tho);
                var tso = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
                tso.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#1a4a7a")));
                tso.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#E89A24")));
                rowStyle.Triggers.Add(tso);
                DgCredenciales.RowStyle = rowStyle;

                var hdr = new Style(typeof(DataGridColumnHeader));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.Transparent));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#A0C4E0")));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#0e3560")));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
                hdr.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
                DgCredenciales.ColumnHeaderStyle = hdr;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // VIEW MODEL — representa una fila en el DataGrid
    // ═════════════════════════════════════════════════════════════════════
    public class CredencialVM
    {
        public int CredencialID { get; set; }
        public string NombreServicio { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string ContrasenaCifrada { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string UrlAcceso { get; set; } = "";
        public string NotasSeguras { get; set; } = "";
        public DateTime? FechaVencimiento { get; set; }
        public DateTime UltimaActualizacion { get; set; }

        // Columnas calculadas para el DataGrid
        public string ContrasenaMask => "••••••••";

        public string VencimientoFormateado
        {
            get
            {
                if (!FechaVencimiento.HasValue) return "Sin vencimiento";
                int dias = (FechaVencimiento.Value - DateTime.Today).Days;
                if (dias < 0) return "Vencida";
                if (dias == 0) return "Hoy";
                if (dias <= 30) return $"En {dias} días";
                return FechaVencimiento.Value.ToString("dd/MM/yyyy");
            }
        }
    }

}