using Dominio; // Capa de negocio original
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Dominio.UsuarioDominio;


namespace Presentación
{
    public partial class View_Create_Assets : UserControl
    {

        // Instancia global de la capa de negocio
        private readonly ActivosDominio _dominio = new ActivosDominio();

        public View_Create_Assets()
        {
            InitializeComponent();

            // Inicializaciones de arranque seguras
            DpFecha.SelectedDate = DateTime.Today;
            CargarCombosActivo();
        }

        /// <summary>
        /// Mapea y consume los datos directo de las tablas relacionales del SGSI
        /// </summary>
        private void CargarCombosActivo()
        {
            try
            {
                var datos = new AccesoDatos.ConexionSql();

                // 1. Cargar Categorías
                datos.ConsultaDatos("SELECT CategoriaID, Nombre FROM ITAM.CategoriasActivo", "Categorias");
                CbCategoria.DisplayMemberPath = "Nombre";
                CbCategoria.SelectedValuePath = "CategoriaID";
                CbCategoria.ItemsSource = datos.Ds.Tables["Categorias"].DefaultView;

                // 2. Cargar Ubicaciones
                datos.ConsultaDatos("SELECT UbicacionID, NombreNomenclatura FROM Core.Ubicaciones", "Ubicaciones");
                CbUbicacion.DisplayMemberPath = "NombreNomenclatura";
                CbUbicacion.SelectedValuePath = "UbicacionID";
                CbUbicacion.ItemsSource = datos.Ds.Tables["Ubicaciones"].DefaultView;

                // 3. Cargar Proveedores
                datos.ConsultaDatos("SELECT ProveedorID, RazonSocial FROM Core.Proveedores", "Proveedores");
                CbProveedor.DisplayMemberPath = "RazonSocial";
                CbProveedor.SelectedValuePath = "ProveedorID";
                CbProveedor.ItemsSource = datos.Ds.Tables["Proveedores"].DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar catálogos del SGSI:\n{ex.Message}",
                                "Error de Persistencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Control de Flujo del Asistente (Siguiente / Guardar)
        /// </summary>
        private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (WizardTabControl.SelectedIndex == 0)
            {
                // Validación del Paso 1 antes de permitir avanzar
                if (!ValidarPasoBase()) return;

                // Avanzar al Paso 2 (Hardware)
                WizardTabControl.SelectedIndex = 1;

                // Actualización visual del Wizard Header a tono Activo
                Step2Indicator.Opacity = 1.0;
                CircleStep2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E89A24"));
                CircleStep2.BorderThickness = new Thickness(0);
                TxtNum2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060621"));
                TxtStep2Title.Foreground = Brushes.White;
                Step1Indicator.Opacity = 0.7;

                BtnAtras.Visibility = Visibility.Visible;
                BtnSiguiente.Content = "Guardar Activo";
            }
            else
            {
                // EJECUCIÓN FINAL DE GUARDADO (Consumiendo Dominio)
                ProcesarGuardadoActivo();
            }
        }

        /// <summary>
        /// Control de Flujo (Regresar al paso anterior)
        /// </summary>
        private void BtnAtras_Click(object sender, RoutedEventArgs e)
        {
            if (WizardTabControl.SelectedIndex == 1)
            {
                WizardTabControl.SelectedIndex = 0;

                // Restablecer estados visuales del Header
                Step1Indicator.Opacity = 1.0;
                Step2Indicator.Opacity = 0.5;
                CircleStep2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151538"));
                CircleStep2.BorderThickness = new Thickness(1);
                TxtNum2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0B8"));
                TxtStep2Title.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0B8"));

                BtnAtras.Visibility = Visibility.Collapsed;
                BtnSiguiente.Content = "Siguiente";
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult rta = MessageBox.Show("¿Desea cancelar el registro actual? Se perderán los cambios.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (rta == MessageBoxResult.Yes)
            {
                LimpiarFormulario();
                RegresarAlPanelInicial();
            }
        }

        private bool ValidarPasoBase()
        {
            if (CbCategoria.SelectedIndex == -1)
            {
                MessageBox.Show("Debe seleccionar una categoría para el activo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CbCategoria.Focus();
                return false;
            }
            if (CbUbicacion.SelectedIndex == -1)
            {
                MessageBox.Show("Debe asignar una ubicación física inicial.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                CbUbicacion.Focus();
                return false;
            }
            if (!string.IsNullOrWhiteSpace(TxtCosto.Text) && !decimal.TryParse(TxtCosto.Text, out _))
            {
                MessageBox.Show("El costo del activo debe ser un valor numérico válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCosto.Focus();
                return false;
            }
            return true;
        }

        private void ProcesarGuardadoActivo()
        {
            try
            {
                // Extracción segura de tipos anulables (Nullables)
                decimal? costo = string.IsNullOrWhiteSpace(TxtCosto.Text) ? null : decimal.Parse(TxtCosto.Text);
                DateTime? fecha = DpFecha.SelectedDate;
                int? proveedorId = CbProveedor.SelectedValue == null ? null : (int?)Convert.ToInt32(CbProveedor.SelectedValue);

                // Inyección y consumo de la capa Dominio con los 19 parámetros exactos
                var resultado = _dominio.CrearActivo(
                    // Campos del Paso 1 (ActivosBase)
                    (int)CbCategoria.SelectedValue,                            // 1 (int)
                    (int)CbUbicacion.SelectedValue,                            // 2 (int)
                    TxtEtiqueta.Text.Trim(),                                   // 3 (string) <-- ¡FALTABA ESTE PARÁMETRO AQUÍ!
                    TxtMarca.Text.Trim(),                                      // 4 (string)
                    TxtModelo.Text.Trim(),                                     // 5 (string)
                    TxtSerie.Text.Trim(),                                      // 6 (string)
                    proveedorId,                                               // 7 (int?)
                    fecha,                                                     // 8 (DateTime?)
                    costo,                                                     // 9 (decimal?)
                    ((ComboBoxItem)CbEstado.SelectedItem).Content.ToString(),  // 10 (string)

                    // Campos del Paso 2 (EspecificacionesHardware)
                    TxtProcesador.Text.Trim(),                                 // 11 (string)
                    TxtRam.Text.Trim(),                                        // 12 (string)
                    TxtDisco1.Text.Trim(),                                     // 13 (string)
                    TxtDisco2.Text.Trim(),                                     // 14 (string)
                    TxtGrafica.Text.Trim(),                                    // 15 (string)
                    TxtSo.Text.Trim(),                                         // 16 (string)
                    TxtMac.Text.Trim(),                                        // 17 (string)
                    TxtIp.Text.Trim(),                                         // 18 (string)
                    TxtResolucion.Text.Trim()                                  // 19 (string) <-- ¡Ahora sí cae en su posición correcta!
                );

                MessageBox.Show(resultado.Mensaje, resultado.Exitoso ? "Éxito" : "Error de Integridad",
                                MessageBoxButton.OK, resultado.Exitoso ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (resultado.Exitoso)
                {
                    LimpiarFormulario();
                    RegresarAlPanelInicial();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al procesar la transacción:\n{ex.Message}", "Error Interno", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimpiarFormulario()
        {
            // Reset Controles Paso 1
            CbCategoria.SelectedIndex = -1;
            CbUbicacion.SelectedIndex = -1;
            CbProveedor.SelectedIndex = -1;
            CbEstado.SelectedIndex = 0;
            TxtCosto.Clear();
            DpFecha.SelectedDate = DateTime.Today;

            // Reset Controles Paso 2
            TxtMarca.Clear(); TxtModelo.Clear(); TxtSerie.Clear(); TxtMac.Clear(); TxtIp.Clear();
            TxtProcesador.Clear(); TxtRam.Clear(); TxtDisco1.Clear(); TxtDisco2.Clear();
            TxtGrafica.Clear(); TxtSo.Clear(); TxtResolucion.Clear();
        }

        private void RegresarAlPanelInicial()
        {
            // Buscamos la ventana de WinForms que aloja este componente WPF
            var miDashboard = System.Windows.Window.GetWindow(this) as Dashboard;

            if (miDashboard != null)
            {

            }
        }

        public void BtnEstadistica_Click(object sender, EventArgs e)
        {

        }


    }
}