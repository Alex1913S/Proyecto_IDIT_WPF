using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Presentación
{
    public partial class Employee_Viewer : UserControl
    {
        // ── Capa de negocio (mismos objetos que el WinForms) ──────────────────
        private readonly CN_Colaboradores _cnColaboradores = new CN_Colaboradores();
        private readonly ColaboradorDominio _colaboradorDominio = new ColaboradorDominio();

        // ── Estado de paginación (igual que See_Assets) ───────────────────────
        private List<DataRow> _registrosFiltrados = new List<DataRow>();
        private int _paginaActual = 1;
        private const int _registrosPorPagina = 12;
        private int _totalPaginas = 1;

        // ── Filtro activo ─────────────────────────────────────────────────────
        private string _filtroRol = "";      // "" = Todos, "Administrador", "Operador", "Empleado"
        private string _filtroBusqueda = "";

        // ── Tabla completa desde BD ───────────────────────────────────────────
        private DataTable _tablaCompleta;

        public Employee_Viewer()
        {
            InitializeComponent();
        }

        // ═════════════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═════════════════════════════════════════════════════════════════════
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDatos();
        }

        // ═════════════════════════════════════════════════════════════════════
        // CARGA Y FILTRADO DE DATOS
        // ═════════════════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            try
            {
                // Reutiliza MostrarColaboradores() igual que el WinForms
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
                MessageBox.Show($"Error al cargar colaboradores: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aplica el filtro de rol + búsqueda y actualiza los contadores de tabs.
        /// </summary>
        private void AplicarFiltros()
        {
            if (_tablaCompleta == null) return;

            IEnumerable<DataRow> filas = _tablaCompleta.AsEnumerable();

            // Filtro de búsqueda (cédula, nombre, cargo)
            if (!string.IsNullOrWhiteSpace(_filtroBusqueda))
            {
                string criterio = _filtroBusqueda.ToLower();
                filas = filas.Where(r =>
                    (r["DocumentoIdentidad"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["NombreCompleto"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["Cargo"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["CorreoCorporativo"]?.ToString() ?? "").ToLower().Contains(criterio));
            }

            // Filtro de rol
            if (!string.IsNullOrEmpty(_filtroRol))
                filas = filas.Where(r =>
                    (r["NombrePerfil"]?.ToString() ?? "").Equals(_filtroRol, StringComparison.OrdinalIgnoreCase));

            _registrosFiltrados = filas.ToList();

            // Actualizar badges de los tabs con conteo real
            ActualizarBadgesTabs();

            // Resetear a página 1 y renderizar
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

        // ═════════════════════════════════════════════════════════════════════
        // PAGINACIÓN (mismo patrón que See_Assets)
        // ═════════════════════════════════════════════════════════════════════
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

            // Proyectar a objetos anónimos para el binding del DataGrid
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

                // Guardamos datos extra para el panel lateral (no columnas visibles)
                _Nombres = r["Nombres"]?.ToString() ?? "",
                _Apellidos = r["Apellidos"]?.ToString() ?? "",
                _FotoBytes = r["Foto"] != DBNull.Value ? (byte[])r["Foto"] : null,
                _DepartamentoID = r["DepartamentoID"] != DBNull.Value ? Convert.ToInt32(r["DepartamentoID"]) : 0,
                _UbicacionID = r["UbicacionID"] != DBNull.Value ? Convert.ToInt32(r["UbicacionID"]) : 0,
                _PerfilID = r["PerfilID"] != DBNull.Value ? Convert.ToInt32(r["PerfilID"]) : 0,
                _FechaIngresoCruda = r["FechaIngreso"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["FechaIngreso"]) : null,
                _EstadoRaw = r["Estado"]
            }).ToList<dynamic>();

            // Info de paginador
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
                    Foreground = i == _paginaActual ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8)),
                    Background = i == _paginaActual
                                    ? new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x45))
                                    : Brushes.Transparent
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

        // ═════════════════════════════════════════════════════════════════════
        // SELECCIÓN EN EL DATAGRID → Panel lateral
        // ═════════════════════════════════════════════════════════════════════
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
                // Nombre y cargo
                TxtPanelNombre.Text = fila.NombreCompleto ?? "—";
                TxtPanelCargo.Text = fila.Cargo ?? "—";
                TxtPanelCedula.Text = fila.DocumentoIdentidad ?? "—";
                TxtPanelCorreo.Text = fila.CorreoCorporativo ?? "—";
                TxtPanelDepartamento.Text = fila.NombreDepartamento ?? "—";
                TxtPanelFechaIngreso.Text = fila._FechaIngresoCruda != null
                    ? ((DateTime)fila._FechaIngresoCruda).ToString("dd/MM/yyyy")
                    : "—";

                // Perfil con color de badge
                string perfil = fila.NombrePerfil ?? "—";
                TxtPanelPerfil.Text = perfil;
                TxtPanelPerfil.Foreground = perfil switch
                {
                    "Administrador" => new SolidColorBrush(Color.FromRgb(0xE8, 0x9A, 0x24)),
                    "Operador" => new SolidColorBrush(Color.FromRgb(0x34, 0xAA, 0xDC)),
                    "Empleado" => new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64)),
                    _ => new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8))
                };

                // Estado con color
                string estado = fila.EstadoTexto ?? "—";
                TxtPanelEstado.Text = estado;
                TxtPanelEstado.Foreground = estado == "Activo"
                    ? new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64))
                    : new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));

                // Inicial del avatar o foto
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

                BtnEditar.IsEnabled = true;
                BtnEliminar.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos del panel: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // FILTROS POR TAB
        // ═════════════════════════════════════════════════════════════════════
        private void FilterTab_Click(object sender, RoutedEventArgs e)
        {
            // Habilitar todos los tabs primero
            BtnTabTodos.IsEnabled = true;
            BtnTabAdministradores.IsEnabled = true;
            BtnTabOperadores.IsEnabled = true;
            BtnTabEmpleados.IsEnabled = true;

            // Deshabilitar el activo (efecto "seleccionado")
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

        // ═════════════════════════════════════════════════════════════════════
        // BÚSQUEDA
        // ═════════════════════════════════════════════════════════════════════
        private void TxtBuscarColaborador_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filtroBusqueda = TxtBuscarColaborador.Text.Trim();
            AplicarFiltros();
        }

        // ═════════════════════════════════════════════════════════════════════
        // BOTONES ACCIÓN
        // ═════════════════════════════════════════════════════════════════════
        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (DgColaboradores.SelectedItem == null) return;
            dynamic fila = DgColaboradores.SelectedItem;

            // Aquí puedes abrir tu ventana/UC de edición pasándole la cédula
            MessageBox.Show($"Editar colaborador: {fila.DocumentoIdentidad}\n(Conecta aquí tu formulario de edición)",
                            "Editar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (DgColaboradores.SelectedItem == null) return;
            dynamic fila = DgColaboradores.SelectedItem;

            var confirmacion = MessageBox.Show(
                $"¿Eliminar permanentemente al colaborador con identificación {fila.DocumentoIdentidad}?\n\nEsta acción es irreversible.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmacion == MessageBoxResult.Yes)
            {
                try
                {
                    bool ok = _cnColaboradores.EliminarColaborador(fila.DocumentoIdentidad);
                    if (ok)
                    {
                        MessageBox.Show("Colaborador eliminado correctamente.", "Éxito",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarDatos();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo completar la eliminación.", "Error",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error de Restricción",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de exportación a Excel pendiente de implementar.",
                            "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════
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

        private string ConvertirEstado(object valor)
        {
            if (valor == null || valor == DBNull.Value) return "—";
            if (valor is bool b) return b ? "Activo" : "Inactivo";
            string s = valor.ToString().ToLower();
            return (s == "true" || s == "1" || s == "activo") ? "Activo" : "Inactivo";
        }

        private string ObtenerIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return "?";
            var partes = nombreCompleto.Trim().Split(' ');
            if (partes.Length >= 2)
                return $"{partes[0][0]}{partes[1][0]}".ToUpper();
            return partes[0][0].ToString().ToUpper();
        }

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
    }
}