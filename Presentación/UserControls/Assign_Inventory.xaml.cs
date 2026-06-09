using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentación.UserControls
{
    /// <summary>
    /// Lógica de interacción para Assign_Inventory.xaml
    /// </summary>
    public partial class Assign_Inventory : UserControl
    {
        private bool _modoEdicion = false;

        public Assign_Inventory()
        {
            InitializeComponent();
        }

        // ═════════════════════════════════════════════════════════════════
        // LÓGICA DE INTERFAZ (PANEL LATERAL)
        // ═════════════════════════════════════════════════════════════════

        private void BtnNuevaAsignacion_Click(object sender, RoutedEventArgs e)
        {
            _modoEdicion = false;
            TxtTituloPanel.Text = "Nueva Asignación";
            PanelEdicion.Visibility = Visibility.Visible;
            LimpiarFormulario();
        }

        private void BtnEditarAsignacion_Click(object sender, RoutedEventArgs e)
        {
            _modoEdicion = true;
            TxtTituloPanel.Text = "Editar Asignación";
            PanelEdicion.Visibility = Visibility.Visible;

            // Aquí iría la lógica para pre-poblar los datos en el formulario
            // basándote en la fila seleccionada del DgAsignaciones.
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            PanelEdicion.Visibility = Visibility.Collapsed;
            LimpiarFormulario();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para guardar en Base de Datos usando Capa de Dominio

            MessageBox.Show("Asignación guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            PanelEdicion.Visibility = Visibility.Collapsed;
        }

        private void LimpiarFormulario()
        {
            CmbColaborador.SelectedIndex = -1;
            CmbActivo.SelectedIndex = -1;
            DpFechaAsignacion.SelectedDate = null;
            TxtObservaciones.Clear();
        }

        // ═════════════════════════════════════════════════════════════════
        // IMPLEMENTACIÓN DEL TEMA (ITHEMEABLE)
        // ═════════════════════════════════════════════════════════════════

        public void AplicarTema(bool modoClaro)
        {
            var bc = new BrushConverter();

            if (modoClaro)
            {
                // ════════════ MODO CLARO ════════════
                RootGrid.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");

                // Paneles
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                PanelEdicion.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                PanelEdicion.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");

                // Buscador
                BusquedaBorder.Background = (SolidColorBrush)bc.ConvertFromString("#E8F0FF");
                BusquedaBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#BFCFE8");
                TxtBuscar.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtBuscar.CaretBrush = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");

                // Textos
                TxtTitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                TxtTituloPanel.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#4A6080");

                // DataGrid
                DgAsignaciones.Background = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                DgAsignaciones.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                DgAsignaciones.HorizontalGridLinesBrush = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");

                // Estilos DataGrid - Modo Claro
                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#EDF2FF")));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#1E3A5F")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#C3D3F0")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));

                var tHover = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
                tHover.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#D6E4FF")));
                rowStyle.Triggers.Add(tHover);

                var tSelected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
                tSelected.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#BFCFE8")));
                tSelected.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#1E3A5F")));
                rowStyle.Triggers.Add(tSelected);

                DgAsignaciones.RowStyle = rowStyle;

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#EDF2FF")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#4A6080")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#C3D3F0")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
                DgAsignaciones.ColumnHeaderStyle = headerStyle;
            }
            else
            {
                // ════════════ MODO OSCURO (Restaurar XAML) ════════════
                RootGrid.Background = Brushes.Transparent;

                // Paneles
                GridBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                GridBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                PanelEdicion.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                PanelEdicion.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");

                // Buscador
                BusquedaBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");
                BusquedaBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#1a4a7a");
                TxtBuscar.Foreground = Brushes.White;
                TxtBuscar.CaretBrush = Brushes.White;

                // Textos
                TxtTitulo.Foreground = Brushes.White;
                TxtSubtitulo.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");
                TxtTituloPanel.Foreground = Brushes.White;
                TxtInfoPagina.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0C4E0");

                // DataGrid
                DgAsignaciones.Background = Brushes.Transparent;
                DgAsignaciones.Foreground = Brushes.White;
                DgAsignaciones.HorizontalGridLinesBrush = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");

                // Estilos DataGrid - Modo Oscuro
                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, Brushes.Transparent));
                rowStyle.Setters.Add(new Setter(DataGridRow.ForegroundProperty, Brushes.White));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#0d3a5c")));
                rowStyle.Setters.Add(new Setter(DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));

                var tHover = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
                tHover.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#0e3560")));
                rowStyle.Triggers.Add(tHover);

                var tSelected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
                tSelected.Setters.Add(new Setter(DataGridRow.BackgroundProperty, (SolidColorBrush)bc.ConvertFromString("#1a4a7a")));
                tSelected.Setters.Add(new Setter(DataGridRow.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#E89A24")));
                rowStyle.Triggers.Add(tSelected);

                DgAsignaciones.RowStyle = rowStyle;

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, Brushes.Transparent));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, (SolidColorBrush)bc.ConvertFromString("#A0C4E0")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, (SolidColorBrush)bc.ConvertFromString("#0e3560")));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
                DgAsignaciones.ColumnHeaderStyle = headerStyle;
            }
        }
    }
}
