using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Presentación.UserControls
{
    public partial class Assign_Inventory : UserControl
    {
        // ── Capa de negocio ───────────────────────────────────────────────
        private readonly AsignarActivoDominio _dominio = new AsignarActivoDominio();

        // ── Tabla completa desde BD ───────────────────────────────────────
        private DataTable _tablaCompleta;

        // ── Paginación ────────────────────────────────────────────────────
        private List<DataRow> _registrosFiltrados = new List<DataRow>();
        private int _paginaActual = 1;
        private const int _registrosPorPagina = 12;
        private int _totalPaginas = 1;

        // ── Filtro de búsqueda ────────────────────────────────────────────
        private string _filtroBusqueda = "";

        // ── Estado CRUD ───────────────────────────────────────────────────
        private bool _modoEdicion = false;

        // ── IDs seleccionados para la asignación ──────────────────────────
        private Guid? _activoIdSeleccionado = null;
        private int? _colaboradorIdSeleccionado = null;

        // ═════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═════════════════════════════════════════════════════════════════

        public Assign_Inventory()
        {
            InitializeComponent();
            this.Loaded += Assign_Inventory_Loaded;
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═════════════════════════════════════════════════════════════════

        private void Assign_Inventory_Loaded(object sender, RoutedEventArgs e)
        {
            CargarCombos();
            CargarDatos();
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA DE COMBOS (Activos disponibles y Colaboradores)
        // ═════════════════════════════════════════════════════════════════

        private void CargarCombos()
        {
            try
            {
                // ── Activos en estado "En Bodega" ─────────────────────────
                var dtActivos = _dominio.ObtenerActivosDisponibles();
                CmbActivo.DisplayMemberPath = "EtiquetaActivo";
                CmbActivo.SelectedValuePath = "ActivoID";
                CmbActivo.ItemsSource = dtActivos.DefaultView;

                // ── Colaboradores activos ─────────────────────────────────
                var dtColaboradores = _dominio.ObtenerColaboradores();
                CmbColaborador.DisplayMemberPath = "NombreCompleto";
                CmbColaborador.SelectedValuePath = "ColaboradorID";
                CmbColaborador.ItemsSource = dtColaboradores.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los catálogos:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA DEL DATAGRID (Asignaciones activas)
        // ═════════════════════════════════════════════════════════════════

        private void CargarDatos()
        {
            try
            {
                _tablaCompleta = ObtenerAsignacionesActivas();
                _filtroBusqueda = "";
                TxtBuscar.Text = "";
                _paginaActual = 1;
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las asignaciones:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Obtiene la tabla de asignaciones activas.
        /// Reemplaza el cuerpo con una consulta real a ITAM.Asignaciones
        /// JOIN Core.Colaboradores JOIN ITAM.ActivosBase cuando dispongas de esa vista o SP.
        /// </summary>
        private DataTable ObtenerAsignacionesActivas()
        {
            var dt = new DataTable();
            dt.Columns.Add("AsignacionID", typeof(int));
            dt.Columns.Add("NombreColaborador", typeof(string));
            dt.Columns.Add("NombreActivo", typeof(string));
            dt.Columns.Add("FechaAsignacion", typeof(DateTime));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Observaciones", typeof(string));
            dt.Columns.Add("ActivoID", typeof(Guid));
            dt.Columns.Add("ColaboradorID", typeof(int));

            // TODO: sustituir por:
            //   var acceso = new AccesoDatos.AsignarActivoAccesoDatos();
            //   return acceso.ObtenerAsignacionesActivas();
            return dt;
        }

        // ═════════════════════════════════════════════════════════════════
        // FILTRADO Y PAGINACIÓN
        // ═════════════════════════════════════════════════════════════════

        private void AplicarFiltros()
        {
            if (_tablaCompleta == null) return;

            IEnumerable<DataRow> filas = _tablaCompleta.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_filtroBusqueda))
            {
                string criterio = _filtroBusqueda.ToLower();
                filas = filas.Where(r =>
                    (r["NombreColaborador"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["NombreActivo"]?.ToString() ?? "").ToLower().Contains(criterio) ||
                    (r["Estado"]?.ToString() ?? "").ToLower().Contains(criterio));
            }

            _registrosFiltrados = filas.ToList();
            _paginaActual = 1;
            RenderizarPagina();
        }

        private void RenderizarPagina()
        {
            int total = _registrosFiltrados.Count;
            _totalPaginas = (int)Math.Ceiling(total / (double)_registrosPorPagina);
            if (_totalPaginas < 1) _totalPaginas = 1;
            if (_paginaActual > _totalPaginas) _paginaActual = _totalPaginas;

            var pagina = _registrosFiltrados
                .Skip((_paginaActual - 1) * _registrosPorPagina)
                .Take(_registrosPorPagina)
                .Select(r => new
                {
                    AsignacionID = r["AsignacionID"],
                    NombreColaborador = r["NombreColaborador"]?.ToString() ?? "",
                    NombreActivo = r["NombreActivo"]?.ToString() ?? "",
                    FechaAsignacion = r["FechaAsignacion"] != DBNull.Value
                                            ? Convert.ToDateTime(r["FechaAsignacion"])
                                            : (DateTime?)null,
                    Estado = r["Estado"]?.ToString() ?? "",
                    Observaciones = r["Observaciones"]?.ToString() ?? "",
                    _ActivoID = r["ActivoID"] != DBNull.Value ? (Guid?)r["ActivoID"] : null,
                    _ColaboradorID = r["ColaboradorID"] != DBNull.Value ? (int?)Convert.ToInt32(r["ColaboradorID"]) : null,
                })
                .ToList<dynamic>();

            DgAsignaciones.ItemsSource = pagina;

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

        // ═════════════════════════════════════════════════════════════════
        // PAGINADOR — botones anterior / siguiente
        // ═════════════════════════════════════════════════════════════════

        private void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1) { _paginaActual--; RenderizarPagina(); }
        }

        private void BtnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _totalPaginas) { _paginaActual++; RenderizarPagina(); }
        }

        // ═════════════════════════════════════════════════════════════════
        // BÚSQUEDA EN VIVO
        // ═════════════════════════════════════════════════════════════════

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filtroBusqueda = TxtBuscar.Text.Trim();
            AplicarFiltros();
        }

        // ═════════════════════════════════════════════════════════════════
        // SELECCIÓN EN DATAGRID → Panel lateral detalle
        // ═════════════════════════════════════════════════════════════════

        private void DgAsignaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgAsignaciones.SelectedItem == null)
            {
                LimpiarPanelLateral();
                return;
            }

            try
            {
                dynamic fila = DgAsignaciones.SelectedItem;

                TxtPanelTitulo.Text = fila.NombreActivo ?? "—";
                TxtPanelEstado.Text = fila.Estado ?? "—";
                TxtPanelColaborador.Text = fila.NombreColaborador ?? "—";
                TxtPanelActivo.Text = fila.NombreActivo ?? "—";
                TxtPanelFecha.Text = fila.FechaAsignacion != null
                                                 ? ((DateTime)fila.FechaAsignacion).ToString("dd/MM/yyyy")
                                                 : "—";
                TxtPanelObservaciones.Text = string.IsNullOrWhiteSpace((string)fila.Observaciones)
                                                 ? "—"
                                                 : (string)fila.Observaciones;

                _activoIdSeleccionado = fila._ActivoID;
                _colaboradorIdSeleccionado = fila._ColaboradorID;

                BtnEditarAsignacion.IsEnabled = true;
                MostrarDetalle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el detalle:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — NUEVA ASIGNACIÓN
        // ═════════════════════════════════════════════════════════════════

        private void BtnNuevaAsignacion_Click(object sender, RoutedEventArgs e)
        {
            _modoEdicion = false;
            TxtTituloPanel.Text = "Nueva Asignación";
            BtnGuardar.Content = "Guardar Asignación";
            LimpiarFormulario();
            MostrarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — EDITAR ASIGNACIÓN
        // ═════════════════════════════════════════════════════════════════

        private void BtnEditarAsignacion_Click(object sender, RoutedEventArgs e)
        {
            if (DgAsignaciones.SelectedItem == null) return;

            _modoEdicion = true;
            TxtTituloPanel.Text = "Editar Asignación";
            BtnGuardar.Content = "Actualizar Asignación";

            dynamic fila = DgAsignaciones.SelectedItem;
            DpFechaAsignacion.SelectedDate = fila.FechaAsignacion as DateTime?;
            TxtObservaciones.Text = fila.Observaciones ?? "";

            if (fila._ActivoID != null)
                SeleccionarComboItem(CmbActivo, "ActivoID", fila._ActivoID);

            if (fila._ColaboradorID != null)
                SeleccionarComboItemInt(CmbColaborador, "ColaboradorID", (int)fila._ColaboradorID);

            MostrarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // CRUD — GUARDAR (crear o actualizar)
        // ═════════════════════════════════════════════════════════════════

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ── Recolección de valores ────────────────────────────────
                Guid? activoId = null;
                if (CmbActivo.SelectedValue != null)
                    activoId = Guid.Parse(CmbActivo.SelectedValue.ToString()!);

                int? colaboradorId = null;
                if (CmbColaborador.SelectedValue != null)
                    colaboradorId = Convert.ToInt32(CmbColaborador.SelectedValue);

                DateTime fechaAsignacion = DpFechaAsignacion.SelectedDate ?? DateTime.Today;
                string observaciones = TxtObservaciones.Text.Trim();

                // ── Enviar a Dominio (validaciones incluidas) ─────────────
                var resultado = _dominio.Registrar(activoId, colaboradorId, fechaAsignacion, observaciones);

                MessageBox.Show(resultado.Mensaje,
                    resultado.Exitoso ? "Asignación Registrada" : "Error de Validación",
                    MessageBoxButton.OK,
                    resultado.Exitoso ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (resultado.Exitoso)
                {
                    // Recargar combos: el activo ya no estará disponible
                    CargarCombos();
                    CargarDatos();
                    OcultarFormulario();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al procesar la asignación:\n{ex.Message}",
                                "Error del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // CANCELAR FORMULARIO
        // ═════════════════════════════════════════════════════════════════

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            OcultarFormulario();
        }

        // ═════════════════════════════════════════════════════════════════
        // HELPERS DE UI
        // ═════════════════════════════════════════════════════════════════

        private void MostrarFormulario()
        {
            PanelDetalle.Visibility = Visibility.Collapsed;
            PanelFormulario.Visibility = Visibility.Visible;
            BtnEditarAsignacion.IsEnabled = false;
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
            CmbActivo.SelectedIndex = -1;
            CmbColaborador.SelectedIndex = -1;
            DpFechaAsignacion.SelectedDate = DateTime.Today;
            TxtObservaciones.Clear();
        }

        private void LimpiarPanelLateral()
        {
            TxtPanelTitulo.Text = "Selecciona una asignación";
            TxtPanelEstado.Text = "—";
            TxtPanelColaborador.Text = "—";
            TxtPanelActivo.Text = "—";
            TxtPanelFecha.Text = "—";
            TxtPanelObservaciones.Text = "—";
            BtnEditarAsignacion.IsEnabled = false;
            _activoIdSeleccionado = null;
            _colaboradorIdSeleccionado = null;
        }

        /// <summary>Selecciona item del combo por valor Guid.</summary>
        private void SeleccionarComboItem(ComboBox combo, string columna, object valor)
        {
            if (combo.ItemsSource is DataView dv)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (dv[i][columna]?.ToString() == valor?.ToString())
                    { combo.SelectedIndex = i; return; }
                }
            }
            combo.SelectedIndex = -1;
        }

        /// <summary>Selecciona item del combo por valor int.</summary>
        private void SeleccionarComboItemInt(ComboBox combo, string columna, int valor)
        {
            if (combo.ItemsSource is DataView dv)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (Convert.ToInt32(dv[i][columna]) == valor)
                    { combo.SelectedIndex = i; return; }
                }
            }
            combo.SelectedIndex = -1;
        }

        // ═════════════════════════════════════════════════════════════════
        // TEMA CLARO / OSCURO
        // ═════════════════════════════════════════════════════════════════

        public void AplicarTema(bool modoClaro)
        {
            var bc = new BrushConverter();

            if (modoClaro)
            {
                RootGrid.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                BusquedaBorder.Background = (SolidColorBrush)bc.ConvertFromString("#E8F0FF");
                BusquedaBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#BFCFE8");
                TxtBuscar.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtBuscar.CaretBrush = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtTitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtTituloPanel.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtPanelTitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelColaborador.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelActivo.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelFecha.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtPanelObservaciones.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                DgAsignaciones.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                DgAsignaciones.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                DgAsignaciones.HorizontalGridLinesBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                var rs = BuildRowStyle(bc, "#EDF2FF", "#1E3A5F", "#C3D3F0", "#D6E4FF", "#BFCFE8", "#1E3A5F");
                DgAsignaciones.RowStyle = rs;
                DgAsignaciones.ColumnHeaderStyle = BuildHeaderStyle(bc, "#EDF2FF", "#4A6080", "#C3D3F0");
            }
            else
            {
                RootGrid.Background = Brushes.Transparent;
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                BusquedaBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                BusquedaBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#1a4a7a");
                TxtBuscar.Foreground = Brushes.White;
                TxtBuscar.CaretBrush = Brushes.White;
                TxtTitulo.Foreground = Brushes.White;
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtTituloPanel.Foreground = Brushes.White;
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtPanelTitulo.Foreground = Brushes.White;
                TxtPanelColaborador.Foreground = Brushes.White;
                TxtPanelActivo.Foreground = Brushes.White;
                TxtPanelFecha.Foreground = Brushes.White;
                TxtPanelObservaciones.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                DgAsignaciones.Background = Brushes.Transparent;
                DgAsignaciones.Foreground = Brushes.White;
                DgAsignaciones.HorizontalGridLinesBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                var rs = BuildRowStyle(bc, "Transparent", "White", "#0d3a5c", "#0e3560", "#1a4a7a", "#E89A24");
                DgAsignaciones.RowStyle = rs;
                DgAsignaciones.ColumnHeaderStyle = BuildHeaderStyle(bc, "Transparent", "#A0C4E0", "#0e3560");
            }
        }

        private static Style BuildRowStyle(BrushConverter bc,
            string bg, string fg, string border, string hoverBg, string selBg, string selFg)
        {
            var s = new Style(typeof(DataGridRow));
            s.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                bg == "Transparent" ? Brushes.Transparent : (SolidColorBrush)bc.ConvertFromString(bg)));
            s.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                fg == "White" ? Brushes.White : (SolidColorBrush)bc.ConvertFromString(fg)));
            s.Setters.Add(new Setter(DataGridRow.BorderBrushProperty,
                (SolidColorBrush)bc.ConvertFromString(border)));
            s.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));

            var th = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            th.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString(hoverBg)));
            s.Triggers.Add(th);

            var ts = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            ts.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString(selBg)));
            ts.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString(selFg)));
            s.Triggers.Add(ts);
            return s;
        }

        private static Style BuildHeaderStyle(BrushConverter bc, string bg, string fg, string border)
        {
            var h = new Style(typeof(DataGridColumnHeader));
            h.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty,
                bg == "Transparent" ? Brushes.Transparent : (SolidColorBrush)bc.ConvertFromString(bg)));
            h.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString(fg)));
            h.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
            h.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString(border)));
            h.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            h.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            return h;
        }
    }
}
