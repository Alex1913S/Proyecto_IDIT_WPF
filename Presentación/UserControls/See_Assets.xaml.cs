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
        // Estado y llaves de selección de datos
        private object _activoSeleccionadoId = null;
        private string _estadoActivoSeleccionado = "";

        // Almacenamiento de tabla base traída del Servidor SQL
        private DataTable _dtTodosLosActivos = null;
        private readonly ActivosDominio _activosDominio = new ActivosDominio();

        // Flags e hilos de control de renderizado rápido
        private bool _cargando = true;
        private string _filtroEstadoActual = "Todos";

        // CONFIGURACIÓN DE PARÁMETROS MATEMÁTICOS DE PAGINACIÓN
        private int _paginaActual = 1;
        private readonly int _registrosPorPagina = 10;
        private int _totalPaginas = 1;

        public See_Assets()
        {
            InitializeComponent();
            this.Loaded += See_Assets_Loaded;
        }

        private void See_Assets_Loaded(object sender, RoutedEventArgs e)
        {
            _cargando = true;
            RefrescarGrid();
            _cargando = false;
        }

        public void RefrescarGrid()
        {
            try
            {
                // Consumir la capa de Dominio del Proyecto de 3 Capas
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

            // Unificar strings de criterios
            if (reglasFiltro.Count > 0)
                dvFiltrado.RowFilter = string.Join(" AND ", reglasFiltro);
            else
                dvFiltrado.RowFilter = "";

            // Calcular paginación sobre el set resultante final
            int totalRegistrosFiltrados = dvFiltrado.Count;
            _totalPaginas = (int)Math.Ceiling((double)totalRegistrosFiltrados / _registrosPorPagina);
            if (_totalPaginas < 1) _totalPaginas = 1;

            if (_paginaActual > _totalPaginas) _paginaActual = _totalPaginas;

            // Extraer el segmento exacto de 10 registros
            DataTable dtPaginaSlices = _dtTodosLosActivos.Clone();
            int indiceInicio = (_paginaActual - 1) * _registrosPorPagina;
            int indiceFin = Math.Min(indiceInicio + _registrosPorPagina, totalRegistrosFiltrados);

            for (int i = indiceInicio; i < indiceFin; i++)
            {
                dtPaginaSlices.ImportRow(dvFiltrado[i].Row);
            }

            // Enlazar los 10 registros procesados a la vista
            DgActivos.ItemsSource = dtPaginaSlices.DefaultView;

            // Invocar renderizador de la botonera dinámica
            ActualizarControlesPaginacion(totalRegistrosFiltrados);
        }

        private void ActualizarControlesPaginacion(int totalRegistros)
        {
            if (TxtContadorRegistros == null || TxtInfoPagina == null || PnlNumerosPagina == null) return;

            TxtContadorRegistros.Text = $"Mostrando {totalRegistros} registros";
            TxtInfoPagina.Text = $"Página {_paginaActual} de {_totalPaginas}";

            BtnPaginaAnterior.IsEnabled = (_paginaActual > 1);
            BtnPaginaSiguiente.IsEnabled = (_paginaActual < _totalPaginas);

            // CALCULAR RANGO DE LA VENTANA DESLIZANTE (MÁXIMO 5 BOTONES NUMÉRICOS)
            int maxBotonesVisibles = 5;
            int paginaInicio = 1;
            int paginaFin = _totalPaginas;

            if (_totalPaginas > maxBotonesVisibles)
            {
                paginaInicio = _paginaActual - 2;
                paginaFin = _paginaActual + 2;

                // Controlar desbordamiento por la izquierda
                if (paginaInicio < 1)
                {
                    paginaInicio = 1;
                    paginaFin = maxBotonesVisibles;
                }
                // Controlar desbordamiento por la derecha
                else if (paginaFin > _totalPaginas)
                {
                    paginaFin = _totalPaginas;
                    paginaInicio = _totalPaginas - maxBotonesVisibles + 1;
                }
            }

            // Limpiar los botones anteriores
            PnlNumerosPagina.Children.Clear();

            // Buscar estilo unificado del XAML
            Style estiloBotonPager = (Style)this.FindResource("PagerButtonStyle");

            // Crear botones numéricos dinámicos al vuelo
            for (int i = paginaInicio; i <= paginaFin; i++)
            {
                Button btnNumero = new Button
                {
                    Content = i.ToString(),
                    Tag = i,
                    Style = estiloBotonPager
                };

                btnNumero.Click += BtnPagina_Click;

                // Aplicar paleta de colores según estado activo/inactivo
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
                return;
            }

            if (DgActivos.SelectedItem is DataRowView fila)
            {
                _activoSeleccionadoId = fila["ActivoID"];
                _estadoActivoSeleccionado = fila["EstadoOperativo"]?.ToString() ?? "";

                // ✅ Verificar que la columna existe y tiene datos
                bool tieneFactura = fila.Row.Table.Columns.Contains("FacturaCompra")
                                 && fila["FacturaCompra"] != DBNull.Value
                                 && fila["FacturaCompra"] != null;

                BtnDescargarFactura.IsEnabled = tieneFactura;
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
                // Leer desde la fila ya cargada en el DataGrid (sin viaje extra a la BD)
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

                // Opción A: Guardar en disco con SaveFileDialog
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
    }
}