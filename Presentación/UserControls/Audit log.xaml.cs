using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Presentación
{
    public partial class Audit_Log : UserControl
    {
        // ── Capa de negocio ───────────────────────────────────────────────
        private readonly AuditoriaDominio _dominio = new AuditoriaDominio();

        // ── Datos ─────────────────────────────────────────────────────────
        private DataTable _tablaCompleta;

        // ── Paginación ────────────────────────────────────────────────────
        private List<DataRow> _registrosFiltrados = new List<DataRow>();
        private int _paginaActual = 1;
        private const int _registrosPorPagina = 15;
        private int _totalPaginas = 1;

        // ─────────────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────────
        public Audit_Log()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────────────
        // CARGA INICIAL
        // ─────────────────────────────────────────────────────────────────
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Rango predeterminado: último mes
            DpDesde.SelectedDate = DateTime.Today.AddDays(-30);
            DpHasta.SelectedDate = DateTime.Today;
        }

        // ─────────────────────────────────────────────────────────────────
        // CONSULTAR LOGS
        // ─────────────────────────────────────────────────────────────────
        private void BtnConsultar_Click(object sender, RoutedEventArgs e)
        {
            if (DpDesde.SelectedDate == null || DpHasta.SelectedDate == null)
            {
                MessageBox.Show("Debe seleccionar ambas fechas (Desde y Hasta) para filtrar.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DpDesde.SelectedDate > DpHasta.SelectedDate)
            {
                MessageBox.Show("La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _tablaCompleta = _dominio.ListarLogsAuditoria(
                    DpDesde.SelectedDate.Value,
                    DpHasta.SelectedDate.Value);

                _paginaActual = 1;
                CargarPagina();
                LimpiarPanelDetalle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar los logs de auditoría:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // LIMPIAR FILTROS
        // ─────────────────────────────────────────────────────────────────
        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            DpDesde.SelectedDate = DateTime.Today.AddDays(-30);
            DpHasta.SelectedDate = DateTime.Today;

            _tablaCompleta = null;
            _registrosFiltrados.Clear();

            DgLogs.ItemsSource = null;
            DgLogs.Visibility = Visibility.Collapsed;
            PanelVacio.Visibility = Visibility.Visible;
            PanelDiffBorder.Visibility = Visibility.Collapsed;

            TxtTotalRegistros.Text = "0";
            TxtInfoPagina.Text = "Página 1 de 1";
            TxtContadorRegistros.Text = "Mostrando 0 registros";
            PnlNumerosPagina.Children.Clear();
            BtnPaginaAnterior.IsEnabled = false;
            BtnPaginaSiguiente.IsEnabled = false;

            LimpiarPanelDetalle();
        }

        // ─────────────────────────────────────────────────────────────────
        // PAGINACIÓN
        // ─────────────────────────────────────────────────────────────────
        private void CargarPagina()
        {
            if (_tablaCompleta == null) return;

            _registrosFiltrados = _tablaCompleta.AsEnumerable().ToList();
            int total = _registrosFiltrados.Count;

            TxtTotalRegistros.Text = total.ToString();

            _totalPaginas = (int)Math.Ceiling(total / (double)_registrosPorPagina);
            if (_totalPaginas < 1) _totalPaginas = 1;
            if (_paginaActual > _totalPaginas) _paginaActual = _totalPaginas;

            var pagina = _registrosFiltrados
                .Skip((_paginaActual - 1) * _registrosPorPagina)
                .Take(_registrosPorPagina)
                .Select(r => new AuditLogVM
                {
                    LogID = r["LogID"] != DBNull.Value ? Convert.ToInt32(r["LogID"]) : 0,
                    TablaAfectada = r["TablaAfectada"]?.ToString() ?? "—",
                    RegistroID = r["RegistroID"]?.ToString() ?? "—",
                    Accion = r["Accion"]?.ToString() ?? "—",
                    UsuarioBD = r["UsuarioBD"]?.ToString() ?? "—",
                    FechaAccion = r["FechaAccion"] != DBNull.Value
                                    ? Convert.ToDateTime(r["FechaAccion"]) : DateTime.MinValue,
                    DetalleAnterior = r["DetalleAnterior"]?.ToString() ?? "",
                    DetalleNuevo = r["DetalleNuevo"]?.ToString() ?? "",
                })
                .ToList();

            if (total == 0)
            {
                DgLogs.Visibility = Visibility.Collapsed;
                PanelVacio.Visibility = Visibility.Visible;
            }
            else
            {
                PanelVacio.Visibility = Visibility.Collapsed;
                DgLogs.Visibility = Visibility.Visible;
                DgLogs.ItemsSource = pagina;
            }

            TxtInfoPagina.Text = $"Página {_paginaActual} de {_totalPaginas}";
            TxtContadorRegistros.Text = $"Mostrando {pagina.Count} de {total} registros";
            BtnPaginaAnterior.IsEnabled = _paginaActual > 1;
            BtnPaginaSiguiente.IsEnabled = _paginaActual < _totalPaginas;

            GenerarBotonesPagina();
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
                btn.Click += (s, ev) => { _paginaActual = numPag; CargarPagina(); };
                PnlNumerosPagina.Children.Add(btn);
            }
        }

        private void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1) { _paginaActual--; CargarPagina(); }
        }

        private void BtnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _totalPaginas) { _paginaActual++; CargarPagina(); }
        }

        // ─────────────────────────────────────────────────────────────────
        // SELECCIÓN EN DATAGRID
        // ─────────────────────────────────────────────────────────────────
        private void DgLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgLogs.SelectedItem is not AuditLogVM vm)
            {
                LimpiarPanelDetalle();
                return;
            }

            // Panel lateral
            TxtPanelTabla.Text = vm.TablaAfectada;
            TxtPanelUsuario.Text = vm.UsuarioBD;
            TxtPanelFecha.Text = vm.FechaAccion != DateTime.MinValue
                ? vm.FechaAccion.ToString("dd/MM/yyyy HH:mm:ss") : "—";
            TxtPanelRegistroId.Text = vm.RegistroID;

            TxtPanelAccion.Text = vm.Accion;
            TxtPanelAccion.Foreground = vm.Accion switch
            {
                "INSERT" => new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64)),
                "UPDATE" => new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00)),
                "DELETE" => new SolidColorBrush(Color.FromRgb(0xE7, 0x13, 0x3F)),
                "SELECT" => new SolidColorBrush(Color.FromRgb(0x34, 0xAA, 0xDC)),
                _ => new SolidColorBrush(Color.FromRgb(0xA0, 0xC4, 0xE0))
            };

            // Panel diff (expandible)
            bool tieneDiff = !string.IsNullOrWhiteSpace(vm.DetalleAnterior)
                          || !string.IsNullOrWhiteSpace(vm.DetalleNuevo);

            if (tieneDiff)
            {
                TxtDetalleAnterior.Text = string.IsNullOrWhiteSpace(vm.DetalleAnterior)
                    ? "(sin datos previos)" : vm.DetalleAnterior;
                TxtDetalleNuevo.Text = string.IsNullOrWhiteSpace(vm.DetalleNuevo)
                    ? "(sin datos nuevos)" : vm.DetalleNuevo;
                PanelDiffBorder.Visibility = Visibility.Visible;
            }
            else
            {
                PanelDiffBorder.Visibility = Visibility.Collapsed;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // EXPORTAR
        // ─────────────────────────────────────────────────────────────────
        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de exportación a Excel pendiente de implementar.",
                "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────
        private void LimpiarPanelDetalle()
        {
            TxtPanelTabla.Text = "—";
            TxtPanelAccion.Text = "—";
            TxtPanelAccion.Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xC4, 0xE0));
            TxtPanelUsuario.Text = "—";
            TxtPanelFecha.Text = "—";
            TxtPanelRegistroId.Text = "—";
            PanelDiffBorder.Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────────────────────────
        // TEMA CLARO / OSCURO
        // ─────────────────────────────────────────────────────────────────
        public void AplicarTema(bool modoClaro)
        {
            var bc = new BrushConverter();

            if (modoClaro)
            {
                RootGrid.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                TxtTitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtTotalRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#2F80ED");
                DgLogs.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                DgLogs.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
            }
            else
            {
                RootGrid.Background = Brushes.Transparent;
                PanelLateralBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                PanelLateralBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                TxtTitulo.Foreground = Brushes.White;
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtContadorRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtTotalRegistros.Foreground = (SolidColorBrush)bc.ConvertFromString("#2F80ED");
                DgLogs.Background = Brushes.Transparent;
                DgLogs.Foreground = Brushes.White;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // VIEW MODEL — fila del DataGrid de auditoría
    // ─────────────────────────────────────────────────────────────────────
    public class AuditLogVM
    {
        public int LogID { get; set; }
        public string TablaAfectada { get; set; } = "";
        public string RegistroID { get; set; } = "";
        public string Accion { get; set; } = "";
        public string UsuarioBD { get; set; } = "";
        public DateTime FechaAccion { get; set; }
        public string DetalleAnterior { get; set; } = "";
        public string DetalleNuevo { get; set; } = "";
    }
}