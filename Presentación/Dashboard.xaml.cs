using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentación
{
    /// <summary>
    /// Lógica de interacción para Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        private bool isDarkMode = true;
        private bool isSidebarCollapsed = false;

        public Dashboard()
        {
            InitializeComponent();
        }

        // =================================================================
        // LÓGICA DE COLAPSO INTERNO DEL MENÚ LATERAL (SÓLO ICONOS)
        // =================================================================
        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (!isSidebarCollapsed)
            {
                SidebarColumn.Width = new GridLength(65);
                UserInfoPanel.Visibility = Visibility.Collapsed;
                SubmenuActivos.Visibility = Visibility.Collapsed;
                ToggleTextInStackPanel(MenuStackPanel, Visibility.Collapsed);
                isSidebarCollapsed = true;
            }
            else
            {
                SidebarColumn.Width = new GridLength(240);
                UserInfoPanel.Visibility = Visibility.Visible;
                ToggleTextInStackPanel(MenuStackPanel, Visibility.Visible);
                isSidebarCollapsed = false;
            }
        }

        private void ToggleTextInStackPanel(StackPanel container, Visibility visibility)
        {
            foreach (var child in container.Children)
            {
                if (child is Button btn)
                {
                    if (btn.Template.FindName("BtnText", btn) is TextBlock txt)
                    {
                        txt.Visibility = visibility;
                    }
                }
            }
        }

        private void BtnActivosParent_Click(object sender, RoutedEventArgs e)
        {
            if (!isSidebarCollapsed)
            {
                SubmenuActivos.Visibility = SubmenuActivos.Visibility == Visibility.Collapsed
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // =================================================================
        // CONTROLADOR DEL GRÁFICO DINÁMICO (DÍA / MES / AÑO)
        // =================================================================
        private void TimeFilter_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            if (sender is RadioButton radioButton)
            {
                string filterType = radioButton.Content.ToString();

                if (filterType == "Día")
                {
                    Bar1.Height = 70; Bar2.Height = 110; Bar3.Height = 45; Bar4.Height = 130; Bar5.Height = 85; Bar6.Height = 100;
                    AxisL1.Text = "Lun"; AxisL2.Text = "Mar"; AxisL3.Text = "Mié"; AxisL4.Text = "Jue"; AxisL5.Text = "Vie"; AxisL6.Text = "Sáb";
                }
                else if (filterType == "Mes")
                {
                    Bar1.Height = 120; Bar2.Height = 60; Bar3.Height = 95; Bar4.Height = 40; Bar5.Height = 115; Bar6.Height = 135;
                    AxisL1.Text = "Ene"; AxisL2.Text = "Feb"; AxisL3.Text = "Mar"; AxisL4.Text = "Abr"; AxisL5.Text = "May"; AxisL6.Text = "Jun";
                }
                else if (filterType == "Año")
                {
                    Bar1.Height = 40; Bar2.Height = 80; Bar3.Height = 130; Bar4.Height = 90; Bar5.Height = 60; Bar6.Height = 120;
                    AxisL1.Text = "2024"; AxisL2.Text = "2025"; AxisL3.Text = "2026"; AxisL4.Text = "2027"; AxisL5.Text = "2028"; AxisL6.Text = "En curso";
                }
            }
        }

        // =================================================================
        // NAVEGACIÓN Y ACCIONES DEL WORKSPACE SELECTOR
        // =================================================================
        private void BtnWorkspaceSelector_Click(object sender, RoutedEventArgs e)
        {
            PopupWorkspace.IsOpen = !PopupWorkspace.IsOpen;
        }

        private void WorkspaceItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var txtCurrent = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentWorkspace", BtnWorkspaceSelector);
                var txtSub = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentSub", BtnWorkspaceSelector);

                if (txtCurrent != null && txtSub != null)
                {
                    string buttonContent = btn.Content.ToString();

                    // Se cambia la comparación exacta por .Contains debido al salto de línea del XAML
                    if (buttonContent.Contains("Finanzas"))
                    {
                        txtCurrent.Text = "Finanzas & Control";
                        txtSub.Text = "Área Contable";
                    }
                    else if (buttonContent.Contains("Seguridad"))
                    {
                        txtCurrent.Text = "Seguridad SGSI";
                        txtSub.Text = "Auditoría de Riesgos";
                    }
                    else if (buttonContent.Contains("Soporte"))
                    {
                        txtCurrent.Text = "Soporte Técnico";
                        txtSub.Text = "Mantenimiento TI";
                    }
                }
                PopupWorkspace.IsOpen = false;
            }
        }

        // =================================================================
        // INTERRUPTOR COMPLETO DE TEMA (CLARO / OSCURO)
        // =================================================================
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var themeIcon = (Path)BtnThemeToggle.Template.FindName("ThemeIcon", BtnThemeToggle);
            var bc = new BrushConverter();

            if (isDarkMode)
            {
                MainWindowBorder.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");
                SidebarBorder.Background = (SolidColorBrush)bc.ConvertFromString("#FFFFFF");
                SubmenuActivos.Background = (SolidColorBrush)bc.ConvertFromString("#F8F9FA");
                SubmenuActivos.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");

                Card1.Background = Brushes.White; Card2.Background = Brushes.White;
                Card3.Background = Brushes.White; Card4.Background = Brushes.White;

                // Se corrigió la referencia aquí: apuntando correctamente a GridContainerBorder
                GridContainerBorder.Background = Brushes.White;
                TypesContainerBorder.Background = Brushes.White;
                MapContainerBorder.Background = Brushes.White;
                TimeFilterPanel.Background = (SolidColorBrush)bc.ConvertFromString("#F1F5F9");
                ColombiaVectorPath.Fill = (SolidColorBrush)bc.ConvertFromString("#E2E8F0");
                ColombiaVectorPath.Stroke = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");

                SolidColorBrush lightThemeText = (SolidColorBrush)bc.ConvertFromString("#22223B");
                LblMainTitle.Foreground = lightThemeText;
                TxtUserName.Foreground = lightThemeText;
                LblChartTitle.Foreground = lightThemeText;
                LblTypesTitle.Foreground = lightThemeText;
                LblMapTitle.Foreground = lightThemeText;

                TxtT1.Foreground = lightThemeText; TxtT2.Foreground = lightThemeText;
                TxtReg1.Foreground = lightThemeText; TxtReg2.Foreground = lightThemeText; TxtReg3.Foreground = lightThemeText;

                BtnWorkspaceSelector.Background = Brushes.White;
                PopupBorder.Background = Brushes.White;
                TxtPopupHeader.Foreground = (SolidColorBrush)bc.ConvertFromString("#6C757D");
                BtnCloseWindow.Background = (SolidColorBrush)bc.ConvertFromString("#E2E8F0");

                var txtSelectorTitle = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentWorkspace", BtnWorkspaceSelector);
                if (txtSelectorTitle != null) txtSelectorTitle.Foreground = lightThemeText;

                // Cambiar dinámicamente el color del icono SVG del tema
                if (themeIcon != null) themeIcon.Fill = lightThemeText;

                MainWindowBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");
                BtnWorkspaceSelector.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");
                isDarkMode = false;
            }
            else
            {
                MainWindowBorder.Background = (SolidColorBrush)bc.ConvertFromString("#060621");
                SidebarBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                SubmenuActivos.Background = (SolidColorBrush)bc.ConvertFromString("#090924");
                SubmenuActivos.BorderBrush = Brushes.White;

                Card1.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                Card2.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                Card3.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                Card4.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");

                // Se corrigió la referencia aquí: apuntando correctamente a GridContainerBorder
                GridContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                TypesContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                MapContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                TimeFilterPanel.Background = (SolidColorBrush)bc.ConvertFromString("#151538");
                ColombiaVectorPath.Fill = (SolidColorBrush)bc.ConvertFromString("#151538");
                ColombiaVectorPath.Stroke = (SolidColorBrush)bc.ConvertFromString("#2D2D5A");

                LblMainTitle.Foreground = Brushes.White;
                TxtUserName.Foreground = Brushes.White;
                LblChartTitle.Foreground = Brushes.White;
                LblTypesTitle.Foreground = Brushes.White;
                LblMapTitle.Foreground = Brushes.White;

                TxtT1.Foreground = Brushes.White; TxtT2.Foreground = Brushes.White;
                TxtReg1.Foreground = Brushes.White; TxtReg2.Foreground = Brushes.White; TxtReg3.Foreground = Brushes.White;

                BtnWorkspaceSelector.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                PopupBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                TxtPopupHeader.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                BtnCloseWindow.Background = (SolidColorBrush)bc.ConvertFromString("#151538");

                MainWindowBorder.BorderBrush = Brushes.White;
                BtnWorkspaceSelector.BorderBrush = Brushes.White;

                // Restaurar el color original del icono SVG en modo oscuro
                if (themeIcon != null) themeIcon.Fill = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");

                isDarkMode = true;
            }
        }

        // =================================================================
        // ACCIONES BASE DE LA VENTANA Y NAVEGACIÓN ENTRE VISTAS
        // =================================================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }

        // =================================================================
        // NAVEGACIÓN Y CARGA DE VISTAS DINÁMICAS (CORREGIDO)
        // =================================================================

        private void BtnVerTodosActivos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Ocultamos el contenedor que tiene los gráficos y el mapa
                if (GridContainerBorder != null)
                {
                    GridContainerBorder.Visibility = Visibility.Collapsed;
                }

                if (ContenedorPrincipal != null)
                {
                    // 2. Hacemos visible el contenedor de las vistas hijas
                    ContenedorPrincipal.Visibility = Visibility.Visible;

                    // 3. Forzamos expansión total en el Grid Padre
                    ContenedorPrincipal.HorizontalAlignment = HorizontalAlignment.Stretch;
                    ContenedorPrincipal.VerticalAlignment = VerticalAlignment.Stretch;

                    // CRUCIAL: Obligamos al contenedor a ocupar desde la fila 1 hasta la parte inferior del Dashboard
                    Grid.SetRow(ContenedorPrincipal, 1);
                    Grid.SetRowSpan(ContenedorPrincipal, 2);
                    Grid.SetColumnSpan(ContenedorPrincipal, 2);

                    // 4. Limpiamos cualquier control anterior
                    ContenedorPrincipal.Children.Clear();

                    // 5. Instanciamos la vista de consulta
                    var vistaActivos = new See_Assets();
                    vistaActivos.HorizontalAlignment = HorizontalAlignment.Stretch;
                    vistaActivos.VerticalAlignment = VerticalAlignment.Stretch;

                    // 6. Agregamos al layout
                    ContenedorPrincipal.Children.Add(vistaActivos);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el inventario: {ex.Message}");
            }
        }

        private void BtnNuevoActivo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridContainerBorder != null)
                {
                    GridContainerBorder.Visibility = Visibility.Collapsed;
                }

                if (ContenedorPrincipal != null)
                {
                    ContenedorPrincipal.Visibility = Visibility.Visible;

                    ContenedorPrincipal.HorizontalAlignment = HorizontalAlignment.Stretch;
                    ContenedorPrincipal.VerticalAlignment = VerticalAlignment.Stretch;

                    // Expandimos también el formulario de creación para que no se corte
                    Grid.SetRow(ContenedorPrincipal, 1);
                    Grid.SetRowSpan(ContenedorPrincipal, 2);
                    Grid.SetColumnSpan(ContenedorPrincipal, 2);

                    ContenedorPrincipal.Children.Clear();

                    var vistaCrear = new View_Create_Assets();
                    vistaCrear.HorizontalAlignment = HorizontalAlignment.Stretch;
                    vistaCrear.VerticalAlignment = VerticalAlignment.Stretch;

                    ContenedorPrincipal.Children.Add(vistaCrear);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el formulario de registro: {ex.Message}");
            }
        }
    }
}