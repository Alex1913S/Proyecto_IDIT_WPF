using Dominio;
using Presentación.Controls;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Presentación.UserControls
{
    public partial class Enseres : UserControl
    {
        private readonly EnseresDominio _dominio = new();
        private DataTable _tablaCompleta;
        private Guid _enserIdEnEdicion = Guid.Empty;
        private bool _modoEdicion = false;

        public Enseres()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarUbicaciones();
            CargarDatos();
        }

        private void CargarUbicaciones()
        {
            try
            {
                var dt = _dominio.ListarUbicaciones();
                CbUbicacion.ItemsSource = null;
                CbUbicacion.Items.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    CbUbicacion.Items.Add(new ComboItemSimple
                    {
                        Display = row["NombreNomenclatura"]?.ToString() ?? "",
                        Value = Convert.ToInt32(row["UbicacionID"])
                    });
                }
                CbUbicacion.DisplayMemberPath = "Display";
                CbUbicacion.SelectedValuePath = "Value";
            }
            catch (Exception ex)
            {
                NotificacionService.Advertencia($"Error al cargar ubicaciones:\n{ex.Message}");
            }
        }

        private void CargarDatos(string busqueda = null)
        {
            try
            {
                _tablaCompleta = _dominio.Listar(busqueda);
                DgEnseres.ItemsSource = _tablaCompleta.DefaultView;

                int total = _dominio.ObtenerTotal();
                TxtTotalEnseres.Text = $"{total} unidades";
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"Error al cargar el inventario de enseres:\n{ex.Message}");
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarDatos(TxtBuscar.Text.Trim());
        }

        private void DgEnseres_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selección informativa; la edición real se dispara desde el botón ✏ de la fila
        }

        private void BtnEditarFila_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not DataRowView fila) return;

            _modoEdicion = true;
            _enserIdEnEdicion = (Guid)fila["EnserID"];

            TxtFormTitulo.Text = "Editar Enser";
            BtnGuardar.Content = "Actualizar Enser";
            BtnCancelarEdicion.Visibility = Visibility.Visible;

            TxtNombre.Text = fila["Nombre"]?.ToString() ?? "";
            CbCategoria.Text = fila["CategoriaEnser"]?.ToString() ?? "";
            TxtCantidad.Text = fila["Cantidad"]?.ToString() ?? "1";
            TxtNumInventario.Text = fila["NumeroInventario"]?.ToString() ?? "";
            TxtCosto.Text = fila["Costo"] == DBNull.Value ? "" : Convert.ToDecimal(fila["Costo"]).ToString();
            TxtObservaciones.Text = fila["Observaciones"]?.ToString() ?? "";
            DpFecha.SelectedDate = fila["FechaAdquisicion"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(fila["FechaAdquisicion"]);

            SeleccionarComboTexto(CbEstado, fila["EstadoFisico"]?.ToString());

            if (fila["UbicacionID"] != DBNull.Value)
                SeleccionarUbicacion(Convert.ToInt32(fila["UbicacionID"]));
            else
                CbUbicacion.SelectedIndex = -1;
        }

        private async void BtnEliminarFila_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not DataRowView fila) return;

            Guid enserId = (Guid)fila["EnserID"];
            string nombre = fila["Nombre"]?.ToString() ?? "";

            bool confirmado = await NotificacionService.Confirmar(
                $"¿Dar de baja el enser «{nombre}»?", "Confirmar eliminación");

            if (!confirmado) return;

            var resultado = _dominio.Eliminar(enserId, nombre);
            if (resultado.Exitoso) NotificacionService.Exito(resultado.Mensaje);
            else NotificacionService.Error(resultado.Mensaje);

            if (resultado.Exitoso) CargarDatos(TxtBuscar.Text.Trim());
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nombre = TxtNombre.Text.Trim();
                string categoria = CbCategoria.Text?.Trim();
                int? ubicacionId = (CbUbicacion.SelectedItem as ComboItemSimple)?.Value is int uid ? uid : (int?)null;
                string estado = (CbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Bueno";
                string numInventario = TxtNumInventario.Text.Trim();
                string observaciones = TxtObservaciones.Text.Trim();
                DateTime? fecha = DpFecha.SelectedDate;

                if (!int.TryParse(TxtCantidad.Text.Trim(), out int cantidad))
                {
                    NotificacionService.Advertencia("La cantidad debe ser un número entero válido.");
                    return;
                }

                decimal? costo = decimal.TryParse(TxtCosto.Text.Trim(), out var c) ? c : (decimal?)null;

                ResultadoEnser resultado = _modoEdicion
                    ? _dominio.Editar(_enserIdEnEdicion, nombre, categoria, ubicacionId, cantidad,
                        estado, numInventario, fecha, costo, observaciones)
                    : _dominio.Crear(nombre, categoria, ubicacionId, cantidad,
                        estado, numInventario, fecha, costo, observaciones);

                if (resultado.Exitoso) NotificacionService.Exito(resultado.Mensaje);
                else NotificacionService.Advertencia(resultado.Mensaje);

                if (resultado.Exitoso)
                {
                    LimpiarFormulario();
                    CargarDatos(TxtBuscar.Text.Trim());
                }
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"Error al guardar el enser:\n{ex.Message}");
            }
        }

        private void BtnCancelarEdicion_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            _modoEdicion = false;
            _enserIdEnEdicion = Guid.Empty;

            TxtFormTitulo.Text = "Nuevo Enser";
            BtnGuardar.Content = "Guardar Enser";
            BtnCancelarEdicion.Visibility = Visibility.Collapsed;

            TxtNombre.Clear();
            CbCategoria.SelectedIndex = -1;
            CbCategoria.Text = "";
            CbUbicacion.SelectedIndex = -1;
            TxtCantidad.Text = "1";
            TxtNumInventario.Clear();
            CbEstado.SelectedIndex = 0;
            DpFecha.SelectedDate = null;
            TxtCosto.Clear();
            TxtObservaciones.Clear();
        }

        private void SeleccionarComboTexto(ComboBox combo, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) { combo.SelectedIndex = 0; return; }
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString() == texto) { combo.SelectedItem = item; return; }
            }
            combo.SelectedIndex = 0;
        }

        private void SeleccionarUbicacion(int ubicacionId)
        {
            foreach (ComboItemSimple item in CbUbicacion.Items)
            {
                if (Convert.ToInt32(item.Value) == ubicacionId) { CbUbicacion.SelectedItem = item; return; }
            }
            CbUbicacion.SelectedIndex = -1;
        }

        public class ComboItemSimple
        {
            public string Display { get; set; }
            public object Value { get; set; }
            public override string ToString() => Display;
        }
    }
}