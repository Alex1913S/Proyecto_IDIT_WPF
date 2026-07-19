using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Dominio.UsuarioDominio;

namespace Presentación
{
    public partial class See_Assets : UserControl
    {
        // ── Estado y llaves de selección de datos ──────────────────────────
        private object _activoSeleccionadoId = null;
        private string _estadoActivoSeleccionado = "";

        // ── Almacenamiento de tabla base traída del Servidor SQL ───────────
        private DataTable _dtTodosLosActivos = null;
        private readonly ActivosDominio _activosDominio = new ActivosDominio();

        // ── Flags e hilos de control de renderizado rápido ─────────────────
        private bool _cargando = true;
        private string _filtroEstadoActual = "Todos";

        // ── Paginación ───────────────────────────────────────────────────
        private int _paginaActual = 1;
        private readonly int _registrosPorPagina = 10;
        private int _totalPaginas = 1;

        // ── 🔒 Control de acceso por rol ────────────────────────────────────
        private readonly string _rol;
        private bool EsAdministrador =>
            !string.IsNullOrWhiteSpace(_rol) && _rol.Trim().ToUpper() == "ADMINISTRADOR";

        // ── Estado del panel de edición ─────────────────────────────────────
        private Guid _activoIdEnEdicion = Guid.Empty;
        private byte[] _facturaCompraOriginal = null;   // la que ya tenía el activo en BD
        private byte[] _facturaCompraNueva = null;       // solo si el usuario reemplaza el PDF
        private bool _facturaFueReemplazada = false;

        // ═════════════════════════════════════════════════════════════════
        // CONSTRUCTORES
        // ═════════════════════════════════════════════════════════════════

        public See_Assets()
        {
            InitializeComponent();

            // 🔥 CORRECCIÓN AQUÍ: 
            // En lugar de llamar a this("Empleado"), asignamos el rol directamente.
            // IMPORTANTE: Cambia "Administrador" por la variable global donde guardas el usuario logueado.
            // Ejemplo: _rol = VariableGlobalSesion.RolUsuario;
            _rol = "Administrador";

            this.Loaded += See_Assets_Loaded;
        }

        public See_Assets(string rol)
        {
            InitializeComponent();
            _rol = rol;
            this.Loaded += See_Assets_Loaded;
        }

        // ═════════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═════════════════════════════════════════════════════════════════
        private void See_Assets_Loaded(object sender, RoutedEventArgs e)
        {
            _cargando = true;

            // 🔒 Solo el Administrador ve los botones de Editar / Eliminar
            BtnEditarActivo.Visibility = EsAdministrador ? Visibility.Visible : Visibility.Collapsed;
            BtnEliminarActivo.Visibility = EsAdministrador ? Visibility.Visible : Visibility.Collapsed;

            RefrescarGrid();
            _cargando = false;
        }

        public void RefrescarGrid()
        {
            try
            {
                _dtTodosLosActivos = _activosDominio.ListarActivos();
                CalcularTotalesBadges();

                _paginaActual = 1;
                AplicarFiltrosCombinados();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar el inventario de activos: {ex.Message}",
                                "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalcularTotalesBadges()
        {
            if (_dtTodosLosActivos == null) return;

            int todos = _dtTodosLosActivos.Rows.Count;
            int asignados = 0;
            int bodega = 0;
            int mantenimiento = 0;

            foreach (DataRow row in _dtTodosLosActivos.Rows)
            {
                string estado = row["EstadoOperativo"]?.ToString() ?? "";
                if (estado == "Asignado") asignados++;
                else if (estado == "En Bodega") bodega++;
                else if (estado == "En Mantenimiento") mantenimiento++;
            }

            BtnTabTodos.Tag = todos.ToString();
            BtnTabAsignados.Tag = asignados.ToString();
            BtnTabBodega.Tag = bodega.ToString();
            BtnTabMantenimiento.Tag = mantenimiento.ToString();
        }

        private void AplicarFiltrosCombinados()
        {
            if (_dtTodosLosActivos == null) return;

            DataView dvFiltrado = new DataView(_dtTodosLosActivos);
            List<string> reglasFiltro = new List<string>();

            // Regla A: Pestaña superior seleccionada
            if (_filtroEstadoActual != "Todos")
            {
                reglasFiltro.Add($"EstadoOperativo = '{_filtroEstadoActual.Replace("'", "''")}'");
            }

            // Regla B: Búsqueda dinámica en vivo
            if (!string.IsNullOrWhiteSpace(TxtBuscarActivo.Text))
            {
                string criterio = TxtBuscarActivo.Text.Replace("'", "''");
                reglasFiltro.Add($"(EtiquetaActivo LIKE '%{criterio}%' OR NumeroSerie LIKE '%{criterio}%' OR Marca LIKE '%{criterio}%' OR Modelo LIKE '%{criterio}%' OR Procesador LIKE '%{criterio}%')");
            }

            if (reglasFiltro.Count > 0)
                dvFiltrado.RowFilter = string.Join(" AND ", reglasFiltro);
            else
                dvFiltrado.RowFilter = "";

            int totalRegistrosFiltrados = dvFiltrado.Count;
            _totalPaginas = (int)Math.Ceiling((double)totalRegistrosFiltrados / _registrosPorPagina);
            if (_totalPaginas < 1) _totalPaginas = 1;

            if (_paginaActual > _totalPaginas) _paginaActual = _totalPaginas;

            DataTable dtPaginaSlices = _dtTodosLosActivos.Clone();
            int indiceInicio = (_paginaActual - 1) * _registrosPorPagina;
            int indiceFin = Math.Min(indiceInicio + _registrosPorPagina, totalRegistrosFiltrados);

            for (int i = indiceInicio; i < indiceFin; i++)
            {
                dtPaginaSlices.ImportRow(dvFiltrado[i].Row);
            }

            DgActivos.ItemsSource = dtPaginaSlices.DefaultView;
            ActualizarControlesPaginacion(totalRegistrosFiltrados);
        }

        private void ActualizarControlesPaginacion(int totalRegistros)
        {
            if (TxtContadorRegistros == null || TxtInfoPagina == null || PnlNumerosPagina == null) return;

            TxtContadorRegistros.Text = $"Mostrando {totalRegistros} registros";
            TxtInfoPagina.Text = $"Página {_paginaActual} de {_totalPaginas}";

            BtnPaginaAnterior.IsEnabled = (_paginaActual > 1);
            BtnPaginaSiguiente.IsEnabled = (_paginaActual < _totalPaginas);

            int maxBotonesVisibles = 5;
            int paginaInicio = 1;
            int paginaFin = _totalPaginas;

            if (_totalPaginas > maxBotonesVisibles)
            {
                paginaInicio = _paginaActual - 2;
                paginaFin = _paginaActual + 2;

                if (paginaInicio < 1)
                {
                    paginaInicio = 1;
                    paginaFin = maxBotonesVisibles;
                }
                else if (paginaFin > _totalPaginas)
                {
                    paginaFin = _totalPaginas;
                    paginaInicio = _totalPaginas - maxBotonesVisibles + 1;
                }
            }

            PnlNumerosPagina.Children.Clear();
            Style estiloBotonPager = (Style)this.FindResource("PagerButtonStyle");

            for (int i = paginaInicio; i <= paginaFin; i++)
            {
                Button btnNumero = new Button
                {
                    Content = i.ToString(),
                    Tag = i,
                    Style = estiloBotonPager
                };

                btnNumero.Click += BtnPagina_Click;

                if (i == _paginaActual)
                {
                    btnNumero.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F45"));
                    btnNumero.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E89A24"));
                }
                else
                {
                    btnNumero.Background = Brushes.Transparent;
                    btnNumero.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0B8"));
                }

                PnlNumerosPagina.Children.Add(btnNumero);
            }
        }

        private void BtnPagina_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _paginaActual = Convert.ToInt32(btn.Tag);
                AplicarFiltrosCombinados();
            }
        }

        private void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                AplicarFiltrosCombinados();
            }
        }

        private void BtnPaginaSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _totalPaginas)
            {
                _paginaActual++;
                AplicarFiltrosCombinados();
            }
        }

        private void FilterTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botonPresionado)
            {
                BtnTabTodos.IsEnabled = true;
                BtnTabAsignados.IsEnabled = true;
                BtnTabBodega.IsEnabled = true;
                BtnTabMantenimiento.IsEnabled = true;

                botonPresionado.IsEnabled = false;

                if (botonPresionado == BtnTabTodos) _filtroEstadoActual = "Todos";
                else if (botonPresionado == BtnTabAsignados) _filtroEstadoActual = "Asignado";
                else if (botonPresionado == BtnTabBodega) _filtroEstadoActual = "En Bodega";
                else if (botonPresionado == BtnTabMantenimiento) _filtroEstadoActual = "En Mantenimiento";

                _paginaActual = 1;
                AplicarFiltrosCombinados();
            }
        }

        private void TxtBuscarActivo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_cargando)
            {
                _paginaActual = 1;
                AplicarFiltrosCombinados();
            }
        }

        private void BtnDescargarFactura_Click(object sender, RoutedEventArgs e)
        {
            if (_activoSeleccionadoId == null) return;
            DescargarFactura(_activoSeleccionadoId);
        }

        private void DgActivos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgActivos.SelectedItem == null)
            {
                _activoSeleccionadoId = null;
                _estadoActivoSeleccionado = "";
                BtnDescargarFactura.IsEnabled = false;
                BtnEditarActivo.IsEnabled = false;
                BtnEliminarActivo.IsEnabled = false;
                return;
            }

            if (DgActivos.SelectedItem is DataRowView fila)
            {
                _activoSeleccionadoId = fila["ActivoID"];
                _estadoActivoSeleccionado = fila["EstadoOperativo"]?.ToString() ?? "";

                bool tieneFactura = fila.Row.Table.Columns.Contains("FacturaCompra")
                                 && fila["FacturaCompra"] != DBNull.Value
                                 && fila["FacturaCompra"] != null;

                BtnDescargarFactura.IsEnabled = tieneFactura;

                // 🔒 Solo Administrador puede editar/eliminar, y solo con selección válida
                BtnEditarActivo.IsEnabled = EsAdministrador;
                BtnEliminarActivo.IsEnabled = EsAdministrador;
            }
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exportando el set de datos filtrados a Microsoft Excel...",
                            "SGSI Asset Management", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DescargarFactura(object activoId)
        {
            try
            {
                if (DgActivos.SelectedItem is not DataRowView fila)
                {
                    MessageBox.Show("Selecciona un activo primero.",
                        "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (fila["FacturaCompra"] == DBNull.Value || fila["FacturaCompra"] == null)
                {
                    MessageBox.Show("Este activo no tiene factura registrada.",
                        "Sin factura", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                byte[] pdf = (byte[])fila["FacturaCompra"];
                string etiq = fila["EtiquetaActivo"]?.ToString() ?? "factura";

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Guardar factura de compra",
                    FileName = $"Factura_{etiq}.pdf",
                    DefaultExt = ".pdf",
                    Filter = "Archivos PDF (*.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllBytes(dialog.FileName, pdf);
                    MessageBox.Show($"Factura guardada en:\n{dialog.FileName}",
                        "Descarga exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al descargar la factura:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // 🔒 EDITAR ACTIVO (solo Administrador)
        // ═════════════════════════════════════════════════════════════════
        private void BtnEditarActivo_Click(object sender, RoutedEventArgs e)
        {
            if (!EsAdministrador)
            {
                MessageBox.Show("Solo un Administrador puede editar activos.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DgActivos.SelectedItem is not DataRowView fila)
            {
                MessageBox.Show("Selecciona un activo primero.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                CargarCombosEdicion();
                PrellenarFormularioEdicion(fila);

                PanelEdicionOverlay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el formulario de edición:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Carga los combos de Categoría, Ubicación y Proveedor para el panel de edición.
        /// Reutiliza ConexionSql tal como lo hace View_Create_Assets.
        /// </summary>
        private void CargarCombosEdicion()
        {
            var datos = new AccesoDatos.ConexionSql();

            // ── 1. Categorías ──────────────────────────────────────────────
            EdCbCategoria.DisplayMemberPath = "Nombre";              // 1° Qué texto mostrar
            EdCbCategoria.SelectedValuePath = "CategoriaID";         // 2° Qué ID guardar tras bambalinas
            datos.ConsultaDatos("SELECT CategoriaID, Nombre FROM ITAM.CategoriasActivo", "Categorias");
            EdCbCategoria.ItemsSource = datos.Ds.Tables["Categorias"].DefaultView; // 3° Cargar datos al final

            // ── 2. Ubicaciones ─────────────────────────────────────────────
            EdCbUbicacion.DisplayMemberPath = "NombreNomenclatura";
            EdCbUbicacion.SelectedValuePath = "UbicacionID";
            datos.ConsultaDatos("SELECT UbicacionID, NombreNomenclatura FROM Core.Ubicaciones", "Ubicaciones");
            EdCbUbicacion.ItemsSource = datos.Ds.Tables["Ubicaciones"].DefaultView;

            // ── 3. Proveedores (Para prevenir) ─────────────────────────────
            EdCbProveedor.DisplayMemberPath = "RazonSocial";
            EdCbProveedor.SelectedValuePath = "ProveedorID";
            datos.ConsultaDatos("SELECT ProveedorID, RazonSocial FROM Core.Proveedores", "Proveedores");
            EdCbProveedor.ItemsSource = datos.Ds.Tables["Proveedores"].DefaultView;
        }
        /// <summary>
        /// Prellena todos los campos del panel de edición con los datos
        /// del activo seleccionado en el DataGrid.
        /// </summary>
        private void PrellenarFormularioEdicion(DataRowView fila)
        {
            _activoIdEnEdicion = fila["ActivoID"] is Guid g ? g : Guid.Parse(fila["ActivoID"].ToString());

            // ── Información general ─────────────────────────────────────
            EdCbCategoria.SelectedValue = fila["CategoriaID"] != DBNull.Value ? (object)Convert.ToInt32(fila["CategoriaID"]) : null;
            EdCbUbicacion.SelectedValue = fila["UbicacionID"] != DBNull.Value ? (object)Convert.ToInt32(fila["UbicacionID"]) : null;

            SeleccionarComboItemPorTexto(EdCbEstado, fila["EstadoOperativo"]?.ToString());

            EdTxtMarca.Text = LimpiarGuion(fila["Marca"]);
            EdTxtModelo.Text = LimpiarGuion(fila["Modelo"]);
            EdTxtSerie.Text = LimpiarGuion(fila["NumeroSerie"]);

            EdTxtCosto.Text = fila["Costo"] != DBNull.Value
                ? Convert.ToDecimal(fila["Costo"]).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "";

            EdDpFecha.SelectedDate = fila["FechaAdquisicion"] != DBNull.Value
                ? Convert.ToDateTime(fila["FechaAdquisicion"])
                : (DateTime?)null;

            // Proveedor: la vista de ListarActivos no siempre trae ProveedorID directamente;
            // si tu SP/consulta lo expone, se selecciona; si no, queda sin seleccionar.
            if (fila.Row.Table.Columns.Contains("ProveedorID") && fila["ProveedorID"] != DBNull.Value)
                EdCbProveedor.SelectedValue = Convert.ToInt32(fila["ProveedorID"]);
            else
                EdCbProveedor.SelectedIndex = -1;

            // ── Hardware ─────────────────────────────────────────────────
            EdTxtProcesador.Text = LimpiarGuion(fila["Procesador"]);
            EdTxtRam.Text = LimpiarGuion(fila["MemoriaRAM"]);
            EdTxtDisco1.Text = LimpiarGuion(fila["Almacenamiento1"]);
            EdTxtDisco2.Text = LimpiarGuion(fila["Almacenamiento2"]);
            EdTxtGrafica.Text = LimpiarGuion(fila["TarjetaGrafica"]);
            EdTxtSo.Text = LimpiarGuion(fila["SistemaOperativo"]);
            EdTxtMac.Text = LimpiarGuion(fila["DireccionMAC"]);
            EdTxtIp.Text = LimpiarGuion(fila["DireccionIP_Estatica"]);
            EdTxtResolucion.Text = LimpiarGuion(fila["ResolucionPantalla"]);

            // ── Factura PDF ──────────────────────────────────────────────
            _facturaCompraOriginal = (fila.Row.Table.Columns.Contains("FacturaCompra")
                                       && fila["FacturaCompra"] != DBNull.Value)
                                       ? (byte[])fila["FacturaCompra"]
                                       : null;
            _facturaCompraNueva = null;
            _facturaFueReemplazada = false;

            EdTxtNombreFactura.Text = _facturaCompraOriginal != null
                ? "Factura actual cargada — selecciona un archivo para reemplazarla"
                : "Sin factura registrada";
            EdTxtNombreFactura.Foreground = _facturaCompraOriginal != null
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xB8));

            EdBtnVerPdf.IsEnabled = _facturaCompraOriginal != null;
        }

        /// <summary>
        /// Ayuda a convertir valores '—' (usados como placeholder en la consulta SQL)
        /// de vuelta a cadena vacía para no mostrarlos en los TextBox de edición.
        /// </summary>
        private static string LimpiarGuion(object valor)
        {
            string texto = valor == DBNull.Value || valor == null ? "" : valor.ToString();
            return texto == "—" || texto == "S/N" ? "" : texto;
        }

        private static void SeleccionarComboItemPorTexto(ComboBox combo, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) { combo.SelectedIndex = -1; return; }

            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), texto, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            combo.SelectedIndex = -1;
        }

        private void EdBtnSeleccionarPdf_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar nueva factura de compra",
                Filter = "Archivos PDF (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var info = new System.IO.FileInfo(dialog.FileName);

                if (info.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("El archivo supera el límite de 10 MB. Selecciona un PDF más pequeño.",
                        "Archivo demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _facturaCompraNueva = System.IO.File.ReadAllBytes(dialog.FileName);
                _facturaFueReemplazada = true;

                EdTxtNombreFactura.Text = info.Name;
                EdTxtNombreFactura.Foreground = Brushes.White;
                EdBtnVerPdf.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer el archivo:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EdBtnVerPdf_Click(object sender, RoutedEventArgs e)
        {
            byte[] pdfAVer = _facturaFueReemplazada ? _facturaCompraNueva : _facturaCompraOriginal;
            if (pdfAVer == null) return;

            try
            {
                string tmp = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"factura_preview_{System.IO.Path.GetRandomFileName()}.pdf");

                System.IO.File.WriteAllBytes(tmp, pdfAVer);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tmp,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el PDF:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Cierra el overlay de edición sin guardar cambios.
        /// También se usa como manejador del clic sobre el fondo oscuro y el botón "X".
        /// </summary>
        private void OverlayEdicion_Click(object sender, RoutedEventArgs e)
        {
            CerrarPanelEdicion();
        }

        private void OverlayEdicion_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CerrarPanelEdicion();
        }

        private void CerrarPanelEdicion()
        {
            PanelEdicionOverlay.Visibility = Visibility.Collapsed;
            _activoIdEnEdicion = Guid.Empty;
            _facturaCompraOriginal = null;
            _facturaCompraNueva = null;
            _facturaFueReemplazada = false;
        }

        /// <summary>
        /// Valida y persiste los cambios del activo en edición.
        /// </summary>
        private void BtnGuardarEdicion_Click(object sender, RoutedEventArgs e)
        {
            if (!EsAdministrador)
            {
                MessageBox.Show("Solo un Administrador puede guardar cambios sobre un activo.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_activoIdEnEdicion == Guid.Empty)
            {
                MessageBox.Show("No hay un activo válido en edición.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ── Validaciones básicas ──────────────────────────────────────
            if (EdCbCategoria.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar una categoría.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                EdCbCategoria.Focus();
                return;
            }

            if (EdCbUbicacion.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar una ubicación.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                EdCbUbicacion.Focus();
                return;
            }

            if (EdCbEstado.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar el estado operativo del activo.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                EdCbEstado.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(EdTxtCosto.Text) && !decimal.TryParse(
                    EdTxtCosto.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("El costo debe ser un valor numérico válido.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                EdTxtCosto.Focus();
                return;
            }

            try
            {
                int categoriaId = (int)EdCbCategoria.SelectedValue;
                int ubicacionId = (int)EdCbUbicacion.SelectedValue;
                string estadoOperativo = ((ComboBoxItem)EdCbEstado.SelectedItem).Content.ToString();
                int? proveedorId = EdCbProveedor.SelectedValue == null ? null : (int?)Convert.ToInt32(EdCbProveedor.SelectedValue);

                string marca = EdTxtMarca.Text.Trim();
                string modelo = EdTxtModelo.Text.Trim();
                string serie = EdTxtSerie.Text.Trim();

                decimal? costo = string.IsNullOrWhiteSpace(EdTxtCosto.Text)
                    ? null
                    : (decimal?)decimal.Parse(EdTxtCosto.Text, System.Globalization.CultureInfo.InvariantCulture);

                DateTime? fecha = EdDpFecha.SelectedDate;

                string procesador = EdTxtProcesador.Text.Trim();
                string ram = EdTxtRam.Text.Trim();
                string disco1 = EdTxtDisco1.Text.Trim();
                string disco2 = EdTxtDisco2.Text.Trim();
                string grafica = EdTxtGrafica.Text.Trim();
                string so = EdTxtSo.Text.Trim();
                string mac = EdTxtMac.Text.Trim();
                string ip = EdTxtIp.Text.Trim();
                string resolucion = EdTxtResolucion.Text.Trim();

                // Etiqueta: la capa de dominio exige que no esté vacía en ModificarActivo.
                // Si tu grid no expone EtiquetaActivo editable, conservamos la existente.
                string etiquetaActual = ObtenerEtiquetaDelGrid(_activoIdEnEdicion) ?? serie;

                byte[] facturaFinal = _facturaFueReemplazada ? _facturaCompraNueva : null; // null = conservar la que ya existe (ver acceso a datos con COALESCE)

                var resultado = _activosDominio.ModificarActivo(
                    _activoIdEnEdicion, categoriaId, ubicacionId, etiquetaActual,
                    marca, modelo, serie, proveedorId,
                    fecha, costo, estadoOperativo,
                    procesador, ram, disco1, disco2,
                    grafica, so, mac, ip, resolucion,
                    facturaFinal
                );

                MessageBox.Show(resultado.Mensaje,
                    resultado.Exitoso ? "Éxito" : "Error de Validación",
                    MessageBoxButton.OK,
                    resultado.Exitoso ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (resultado.Exitoso)
                {
                    CerrarPanelEdicion();
                    RefrescarGrid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al guardar los cambios:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Error del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Busca la etiqueta actual del activo en la tabla completa cargada en memoria,
        /// para no perderla al guardar (ModificarActivo la exige no vacía).
        /// </summary>
        private string ObtenerEtiquetaDelGrid(Guid activoId)
        {
            if (_dtTodosLosActivos == null) return null;

            foreach (DataRow row in _dtTodosLosActivos.Rows)
            {
                if (row["ActivoID"] is Guid g && g == activoId)
                    return row["EtiquetaActivo"]?.ToString();
            }
            return null;
        }

        // ═════════════════════════════════════════════════════════════════
        // 🔒 ELIMINAR (baja lógica) ACTIVO (solo Administrador)
        // ═════════════════════════════════════════════════════════════════
        private void BtnEliminarActivo_Click(object sender, RoutedEventArgs e)
        {
            if (!EsAdministrador)
            {
                MessageBox.Show("Solo un Administrador puede eliminar activos.",
                    "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_activoSeleccionadoId == null)
            {
                MessageBox.Show("Selecciona un activo primero.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmar = MessageBox.Show(
                "¿Dar de baja el activo seleccionado?\n\n" +
                "Esta acción cambia su estado a 'De Baja' y lo saca del inventario disponible.\n" +
                "No se permite si el activo está actualmente 'Asignado'.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar != MessageBoxResult.Yes) return;

            try
            {
                Guid activoId = _activoSeleccionadoId is Guid g ? g : Guid.Parse(_activoSeleccionadoId.ToString());

                var resultado = _activosDominio.EliminarActivoLogico(activoId, _estadoActivoSeleccionado);

                MessageBox.Show(resultado.Mensaje,
                    resultado.Exitoso ? "Éxito" : "No permitido",
                    MessageBoxButton.OK,
                    resultado.Exitoso ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (resultado.Exitoso)
                    RefrescarGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar el activo:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}