using Dominio;
using Microsoft.Win32;
using Presentación.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Presentación.UserControls
{
    public partial class MantenimientoTI : UserControl
    {
        private readonly MantenimientoDominio _dominio = new();
        private readonly ColaboradorDominio _colaboradorDominio = new();

        private DataTable _tablaCompleta;
        private string _filtroEstado = null;
        private string _filtroBusqueda = "";
        private int _colaboradorSesionId;

        // Fila seleccionada
        private int _mantenimientoIdSeleccionado;
        private Guid _activoIdSeleccionado;
        private string _estadoSeleccionado;

        public MantenimientoTI()
        {
            InitializeComponent();
        }

        public MantenimientoTI(int colaboradorSesionId) : this()
        {
            _colaboradorSesionId = colaboradorSesionId;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarCombosResponsables();
            CargarKPIs();
            CargarDatos();
        }

        // ═════════════════════════════════════════════════════════
        // CARGA
        // ═════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            try
            {
                _tablaCompleta = _dominio.Listar(_filtroEstado, _filtroBusqueda);
                DgMantenimientos.ItemsSource = _tablaCompleta.DefaultView;
                ActualizarBadges();
                MostrarSinSeleccion();
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"Error al cargar mantenimientos:\n{ex.Message}");
            }
        }

        private void CargarKPIs()
        {
            try
            {
                var dt = _dominio.ObtenerKPIs();
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                TxtKpiTotal.Text = r["TotalActivos"]?.ToString() ?? "0";
                TxtKpiAbiertos.Text = r["Abiertos"]?.ToString() ?? "0";
                TxtKpiVencidos.Text = r["Vencidos"]?.ToString() ?? "0";
                TxtKpiPromedio.Text = r["PromedioDiasResolucion"] == DBNull.Value
                    ? "—" : Convert.ToDouble(r["PromedioDiasResolucion"]).ToString("0.#");
            }
            catch { /* KPIs no bloquean el módulo */ }
        }

        private void ActualizarBadges()
        {
            if (_tablaCompleta == null) return;
            var todos = _tablaCompleta.AsEnumerable();
            BtnTabTodos.Tag = todos.Count().ToString();
            BtnTabAbierto.Tag = todos.Count(r => r["Estado"].ToString() == "Abierto").ToString();
            BtnTabProceso.Tag = todos.Count(r => r["Estado"].ToString() == "En Proceso").ToString();
            BtnTabRepuesto.Tag = todos.Count(r => r["Estado"].ToString() == "Esperando Repuesto").ToString();
            BtnTabCerrado.Tag = todos.Count(r => r["Estado"].ToString() == "Cerrado").ToString();
        }

        private void CargarCombosResponsables()
        {
            try
            {
                var dt = _colaboradorDominio.ListarDepartamentos(); // placeholder si no tienes lista directa de colaboradores activos
                // Si tienes un método ObtenerColaboradores (como en Assign_Inventory), úsalo aquí:
                var acceso = new AccesoDatos.AsignarActivoAccesoDatos();
                var dtColab = acceso.ObtenerColaboradores();
                CbResponsable.ItemsSource = null;
                CbResponsable.Items.Clear();
                foreach (DataRow row in dtColab.Rows)
                {
                    CbResponsable.Items.Add(new ComboItemGenerico
                    {
                        Display = row["NombreCompleto"]?.ToString() ?? "",
                        Value = Convert.ToInt32(row["ColaboradorID"])
                    });
                }
                CbResponsable.DisplayMemberPath = "Display";
                CbResponsable.SelectedValuePath = "Value";

                var dtActivos = _dominio.ObtenerActivosDisponibles();
                CbActivo.ItemsSource = null;
                CbActivo.Items.Clear();
                foreach (DataRow row in dtActivos.Rows)
                {
                    string etiqueta = row["EtiquetaActivo"]?.ToString();
                    string marca = row["Marca"]?.ToString();
                    string modelo = row["Modelo"]?.ToString();
                    CbActivo.Items.Add(new ComboItemGenerico
                    {
                        Display = string.IsNullOrWhiteSpace(etiqueta) ? $"{marca} {modelo}".Trim() : etiqueta,
                        Value = Guid.Parse(row["ActivoID"].ToString())
                    });
                }
                CbActivo.DisplayMemberPath = "Display";
                CbActivo.SelectedValuePath = "Value";
            }
            catch (Exception ex)
            {
                NotificacionService.Advertencia($"Error al cargar catálogos:\n{ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════
        // FILTROS
        // ═════════════════════════════════════════════════════════
        private void FilterTab_Click(object sender, RoutedEventArgs e)
        {
            BtnTabTodos.IsEnabled = true;
            BtnTabAbierto.IsEnabled = true;
            BtnTabProceso.IsEnabled = true;
            BtnTabRepuesto.IsEnabled = true;
            BtnTabCerrado.IsEnabled = true;

            if (sender is not Button btn) return;
            btn.IsEnabled = false;

            _filtroEstado = btn.Name switch
            {
                "BtnTabAbierto" => "Abierto",
                "BtnTabProceso" => "En Proceso",
                "BtnTabRepuesto" => "Esperando Repuesto",
                "BtnTabCerrado" => "Cerrado",
                _ => null
            };
            CargarDatos();
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filtroBusqueda = TxtBuscar.Text.Trim();
            CargarDatos();
        }

        // ═════════════════════════════════════════════════════════
        // SELECCIÓN Y DETALLE
        // ═════════════════════════════════════════════════════════
        private void DgMantenimientos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgMantenimientos.SelectedItem is not DataRowView fila)
            {
                MostrarSinSeleccion();
                return;
            }

            _mantenimientoIdSeleccionado = Convert.ToInt32(fila["MantenimientoID"]);
            _activoIdSeleccionado = (Guid)fila["ActivoID"];
            _estadoSeleccionado = fila["Estado"].ToString();

            TxtDetEquipo.Text = $"{fila["EtiquetaActivo"]} — {fila["EquipoNombre"]}";
            TxtDetEstado.Text = $"{fila["Estado"]}  ·  {fila["TipoMantenimiento"]}  ·  Prioridad {fila["Prioridad"]}";
            TxtDetDescripcion.Text = string.IsNullOrWhiteSpace(fila["Descripcion"]?.ToString()) ? "—" : fila["Descripcion"].ToString();
            TxtDetDiagnostico.Text = fila["DiagnosticoTecnico"] == DBNull.Value || string.IsNullOrWhiteSpace(fila["DiagnosticoTecnico"]?.ToString())
                ? "Sin diagnóstico registrado aún." : fila["DiagnosticoTecnico"].ToString();
            TxtDetCosto.Text = fila["CostoReparacion"] == DBNull.Value ? "—" : Convert.ToDecimal(fila["CostoReparacion"]).ToString("C0");
            TxtDetGarantia.Text = Convert.ToBoolean(fila["GarantiaAplicada"]) ? "Sí aplica" : "No aplica";

            SeleccionarComboItem(CbNuevoEstado, _estadoSeleccionado);
            TxtComentarioCambio.Text = "";
            TxtCostoFinal.Text = "";
            ChkGarantia.IsChecked = false;

            CargarHistorial();
            CargarFotos();

            PanelSinSeleccion.Visibility = Visibility.Collapsed;
            PanelFormulario.Visibility = Visibility.Collapsed;
            PanelDetalle.Visibility = Visibility.Visible;
        }

        private void CargarHistorial()
        {
            var dt = _dominio.ObtenerHistorial(_mantenimientoIdSeleccionado);
            IcHistorial.ItemsSource = dt.DefaultView.Cast<DataRowView>()
                .OrderByDescending(r => Convert.ToDateTime(r["FechaCambio"]))
                .ToList();
        }

        private void CargarFotos()
        {
            var dt = _dominio.ObtenerFotos(_mantenimientoIdSeleccionado);
            var lista = new List<FotoVM>();
            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new FotoVM
                {
                    Descripcion = r["Descripcion"]?.ToString() ?? "",
                    Imagen = BytesToImagen((byte[])r["Imagen"])
                });
            }
            IcFotos.ItemsSource = lista;
        }

        private void MostrarSinSeleccion()
        {
            PanelDetalle.Visibility = Visibility.Collapsed;
            PanelFormulario.Visibility = Visibility.Collapsed;
            PanelSinSeleccion.Visibility = Visibility.Visible;
        }

        // ═════════════════════════════════════════════════════════
        // NUEVO INGRESO
        // ═════════════════════════════════════════════════════════
        private void BtnNuevo_Click(object sender, RoutedEventArgs e)
        {
            CbActivo.SelectedIndex = -1;
            CbTipo.SelectedIndex = -1;
            CbPrioridad.SelectedIndex = 1;
            CbResponsable.SelectedIndex = -1;
            DpFechaEstimada.SelectedDate = DateTime.Today.AddDays(3);
            TxtDescripcion.Clear();

            PanelSinSeleccion.Visibility = Visibility.Collapsed;
            PanelDetalle.Visibility = Visibility.Collapsed;
            PanelFormulario.Visibility = Visibility.Visible;
        }

        private void BtnCancelarForm_Click(object sender, RoutedEventArgs e) => MostrarSinSeleccion();

        private void BtnGuardarMantenimiento_Click(object sender, RoutedEventArgs e)
        {
            if (CbActivo.SelectedItem is not ComboItemGenerico activo)
            {
                NotificacionService.Advertencia("Selecciona el activo a ingresar a mantenimiento.");
                return;
            }
            if (CbTipo.SelectedItem == null)
            {
                NotificacionService.Advertencia("Selecciona el tipo de mantenimiento.");
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtDescripcion.Text))
            {
                NotificacionService.Advertencia("Describe el problema o la razón del ingreso.");
                return;
            }

            Guid activoId = (Guid)activo.Value;
            string tipo = ((ComboBoxItem)CbTipo.SelectedItem).Content.ToString();
            string prioridad = ((ComboBoxItem)CbPrioridad.SelectedItem).Content.ToString();
            int? responsableId = (CbResponsable.SelectedItem as ComboItemGenerico)?.Value is int rid ? rid : (int?)null;

            var resultado = _dominio.Crear(activoId, tipo, prioridad, TxtDescripcion.Text.Trim(),
                responsableId, null, DpFechaEstimada.SelectedDate, _colaboradorSesionId);

            if (resultado.Exitoso)
                NotificacionService.Exito(resultado.Mensaje, "Éxito");
            else
                NotificacionService.Error(resultado.Mensaje);

            if (resultado.Exitoso)
            {
                CargarCombosResponsables();
                CargarKPIs();
                CargarDatos();
                MostrarSinSeleccion();
            }
        }

        // ═════════════════════════════════════════════════════════
        // CAMBIO DE ESTADO
        // ═════════════════════════════════════════════════════════
        private void BtnActualizarEstado_Click(object sender, RoutedEventArgs e)
        {
            if (CbNuevoEstado.SelectedItem == null)
            {
                NotificacionService.Advertencia("Selecciona el nuevo estado.");
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtComentarioCambio.Text))
            {
                NotificacionService.Advertencia("Agrega un comentario describiendo el cambio (esto queda en el historial de auditoría).");
                return;
            }

            string nuevoEstado = ((ComboBoxItem)CbNuevoEstado.SelectedItem).Content.ToString();

            if (nuevoEstado == "Cerrado")
            {
                var confirmar = MessageBox.Show(
                    "¿Confirmas el cierre de este mantenimiento? El activo volverá a estar disponible en bodega.",
                    "Confirmar cierre", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmar != MessageBoxResult.Yes) return;
            }

            decimal? costo = decimal.TryParse(TxtCostoFinal.Text, out var c) ? c : (decimal?)null;

            var (resultado, historialId) = _dominio.CambiarEstado(
                _mantenimientoIdSeleccionado, _activoIdSeleccionado, _estadoSeleccionado, nuevoEstado,
                TxtComentarioCambio.Text.Trim(), costo, ChkGarantia.IsChecked == true,
                null, _colaboradorSesionId);

            if (resultado.Exitoso)
                NotificacionService.Exito(resultado.Mensaje, "Éxito");
            else
                NotificacionService.Error(resultado.Mensaje);

            if (resultado.Exitoso)
            {
                _ultimoHistorialId = historialId;
                CargarKPIs();
                CargarDatos();
            }
        }

        private int _ultimoHistorialId = 0;

        // ═════════════════════════════════════════════════════════
        // FOTOS
        // ═════════════════════════════════════════════════════════
        private void BtnAdjuntarFoto_Click(object sender, RoutedEventArgs e)
        {
            if (_mantenimientoIdSeleccionado <= 0) return;

            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar foto de evidencia",
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                byte[] bytes = File.ReadAllBytes(dialog.FileName);
                string descripcion = Path.GetFileNameWithoutExtension(dialog.FileName);

                _dominio.AgregarFoto(_mantenimientoIdSeleccionado,
                    _ultimoHistorialId > 0 ? _ultimoHistorialId : (int?)null, bytes, descripcion);

                CargarFotos();
            }
            catch (Exception ex)
            {
                NotificacionService.Advertencia($"No se pudo adjuntar la foto:\n{ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════
        private void SeleccionarComboItem(ComboBox combo, string contenido)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content.ToString() == contenido)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
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

        public class ComboItemGenerico
        {
            public string Display { get; set; }
            public object Value { get; set; }
            public override string ToString() => Display;
        }

        public class FotoVM
        {
            public string Descripcion { get; set; }
            public BitmapImage Imagen { get; set; }
        }
    }
}