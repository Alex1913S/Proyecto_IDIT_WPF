using Dominio;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentación
{
    public partial class Dashboard : Window
    {
        // Variables globales para almacenar la sesión del usuario logueado
        private readonly string _nombre;
        private readonly string _apellido;
        private readonly string _rol;
        private readonly string _cargo;
        private readonly byte[] _foto;
        private readonly int _colaboradorId;

        private bool isDarkMode = true;
        private bool isSidebarCollapsed = false;

        // CONSTRUCTOR PRINCIPAL: Invocado desde la pantalla de Login
        public Dashboard(string username, string accesskey, string rol, string Company_Position, byte[] PictureBPhoto, int colaboradorId)
        {
            InitializeComponent();

            // Mapeo de parámetros
            _nombre = username;
            _apellido = accesskey;
            _rol = rol;
            _cargo = Company_Position;
            _foto = PictureBPhoto;
            _colaboradorId = colaboradorId;

            // Suscribir al evento de carga de la UI
            this.Loaded += Dashboard_Loaded;
        }

        // CONSTRUCTOR SECUNDARIO: Mantiene compatibilidad con el diseñador visual de Visual Studio
        public Dashboard() : this("Usuario", "Demo", "Administrador", "Desarrollador TI", null, 0)
        {
            // El '0' al final cubre el parámetro requerido 'colaboradorId'
        }

        // Asignación de credenciales dinámicas al inicializar la ventana
        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Asigna Nombre y Apellido completo
            if (this.TxtUserName != null)
                TxtUserName.Text = $"{_nombre} {_apellido}";

            // Asigna Rol y Cargo en el subtexto del perfil
            if (this.TxtUserRole != null)
                TxtUserRole.Text = $"{_rol} / {_cargo}";

            // Genera las iniciales basadas en el nombre proporcionado
            if (this.TxtUserInitials != null && !string.IsNullOrWhiteSpace(_nombre))
            {
                TxtUserInitials.Text = _nombre.Substring(0, Math.Min(2, _nombre.Length)).ToUpper();
            }

            // ASIGNAR FECHA REAL: Formato exacto "Lunes, 08 de junio de 2026"
            var culturaEspanol = new System.Globalization.CultureInfo("es-ES");
            string fechaFormateada = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", culturaEspanol);
            LblDate.Text = char.ToUpper(fechaFormateada[0]) + fechaFormateada.Substring(1);


            // Carga la imagen binaria si existe
            CargarFotoPerfil();
            CargarKPIs();
            CargarMapaColombia();
        }

        private void CargarFotoPerfil()
        {
            if (this.ImgUserPhoto == null) return;

            try
            {
                if (_foto != null && _foto.Length > 0)
                {
                    BitmapImage bitmap = new BitmapImage();

                    // Nota: Se usa la ruta completa del espacio de nombres para evitar colisiones con System.Windows.Shapes.Path
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(_foto))
                    {
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();

                    // Muestra el componente de imagen y oculta las iniciales de respaldo
                    ImgUserPhoto.Source = bitmap;
                    ImgUserPhoto.Visibility = Visibility.Visible;
                    if (TxtUserInitials != null) TxtUserInitials.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                // Fallback seguro por si los bytes de la DB están corruptos
                if (ImgUserPhoto != null) ImgUserPhoto.Visibility = Visibility.Collapsed;
                if (TxtUserInitials != null) TxtUserInitials.Visibility = Visibility.Visible;
            }
        }


        // HELPER DE NAVEGACIÓN CENTRAL
        // Toda la navegación pasa por aquí — nunca tocar ContenedorPrincipal
        private void NavegaA(UserControl control)
        {
            if (control == null)
            {
                // Volver al panel de inicio
                NavWorkspaceContent.Content = null;
                NavWorkspaceContent.Visibility = Visibility.Collapsed;
                PanelInicioView.Visibility = Visibility.Visible;
            }
            else
            {
                // Mostrar el UserControl — ocupa todo el Row 1
                PanelInicioView.Visibility = Visibility.Collapsed;
                NavWorkspaceContent.Content = control;
                NavWorkspaceContent.Visibility = Visibility.Visible;
            }
        }

        // NAVEGACIÓN DESDE EL MENÚ
        private void BtnInicio_Click(object sender, RoutedEventArgs e)
        {
            NavegaA(null);
        }

        private void BtnVerTodosActivos_Click(object sender, RoutedEventArgs e)
        {
            NavegaA(new See_Assets());
        }

        private void BtnNuevoActivo_Click(object sender, RoutedEventArgs e)
        {
            NavegaA(new View_Create_Assets());
        }

        private void BtnEmpleado_Click(object sender, RoutedEventArgs e)
        {
            NavegaA(new Employee_Viewer());
        }

        // En Dashboard.xaml.cs, dentro de BtnGestorContrasenas_Click (o el botón que uses):
        private void BtnGestorContrasenas_Click(object sender, RoutedEventArgs e)
        {
            NavegaA(new GestorContrasenas(_colaboradorId)); // pasa el ID del usuario logueado
        }


        // COLAPSO DEL SIDEBAR
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
                        txt.Visibility = visibility;
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
        // GRÁFICO DINÁMICO (DÍA / MES / AÑO)
        // =================================================================
        private void TimeFilter_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            if (sender is RadioButton rb)
            {
                string f = rb.Content.ToString();
                if (f == "Día")
                {
                    Bar1.Height = 70; Bar2.Height = 110; Bar3.Height = 45;
                    Bar4.Height = 130; Bar5.Height = 85; Bar6.Height = 100;
                    AxisL1.Text = "Lun"; AxisL2.Text = "Mar"; AxisL3.Text = "Mié";
                    AxisL4.Text = "Jue"; AxisL5.Text = "Vie"; AxisL6.Text = "Sáb";
                }
                else if (f == "Mes")
                {
                    Bar1.Height = 120; Bar2.Height = 60; Bar3.Height = 95;
                    Bar4.Height = 40; Bar5.Height = 115; Bar6.Height = 135;
                    AxisL1.Text = "Ene"; AxisL2.Text = "Feb"; AxisL3.Text = "Mar";
                    AxisL4.Text = "Abr"; AxisL5.Text = "May"; AxisL6.Text = "Jun";
                }
                else if (f == "Año")
                {
                    Bar1.Height = 40; Bar2.Height = 80; Bar3.Height = 130;
                    Bar4.Height = 90; Bar5.Height = 60; Bar6.Height = 120;
                    AxisL1.Text = "2024"; AxisL2.Text = "2025"; AxisL3.Text = "2026";
                    AxisL4.Text = "2027"; AxisL5.Text = "2028"; AxisL6.Text = "En curso";
                }
            }
        }

        // =================================================================
        // WORKSPACE SELECTOR
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
                    string content = btn.Content.ToString();
                    if (content.Contains("Finanzas")) { txtCurrent.Text = "Finanzas & Control"; txtSub.Text = "Área Contable"; }
                    else if (content.Contains("Seguridad")) { txtCurrent.Text = "Seguridad SGSI"; txtSub.Text = "Auditoría de Riesgos"; }
                    else if (content.Contains("Soporte")) { txtCurrent.Text = "Soporte Técnico"; txtSub.Text = "Mantenimiento TI"; }
                }
                PopupWorkspace.IsOpen = false;
            }
        }

        // =================================================================
        // TEMA CLARO / OSCURO
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

                GridContainerBorder.Background = Brushes.White;
                TypesContainerBorder.Background = Brushes.White;
                MapContainerBorder.Background = Brushes.White;
                TimeFilterPanel.Background = (SolidColorBrush)bc.ConvertFromString("#F1F5F9");
                //ColombiaVectorPath.Fill = (SolidColorBrush)bc.ConvertFromString("#E2E8F0");
                //ColombiaVectorPath.Stroke = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");

                SolidColorBrush lt = (SolidColorBrush)bc.ConvertFromString("#22223B");
                LblMainTitle.Foreground = lt; TxtUserName.Foreground = lt;
                LblChartTitle.Foreground = lt; LblTypesTitle.Foreground = lt;
                //LblMapTitle.Foreground = lt;
                TxtT1.Foreground = lt; TxtT2.Foreground = lt;
                TxtReg1.Foreground = lt; TxtReg2.Foreground = lt; TxtReg3.Foreground = lt;

                BtnWorkspaceSelector.Background = Brushes.White;
                PopupBorder.Background = Brushes.White;
                TxtPopupHeader.Foreground = (SolidColorBrush)bc.ConvertFromString("#6C757D");
                BtnCloseWindow.Background = (SolidColorBrush)bc.ConvertFromString("#E2E8F0");

                var txtSel = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentWorkspace", BtnWorkspaceSelector);
                if (txtSel != null) txtSel.Foreground = lt;
                if (themeIcon != null) themeIcon.Fill = lt;

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

                GridContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                TypesContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                MapContainerBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                TimeFilterPanel.Background = (SolidColorBrush)bc.ConvertFromString("#151538");
                //ColombiaVectorPath.Fill = (SolidColorBrush)bc.ConvertFromString("#151538");
                //ColombiaVectorPath.Stroke = (SolidColorBrush)bc.ConvertFromString("#2D2D5A");

                LblMainTitle.Foreground = Brushes.White; TxtUserName.Foreground = Brushes.White;
                LblChartTitle.Foreground = Brushes.White; LblTypesTitle.Foreground = Brushes.White;
                //LblMapTitle.Foreground = Brushes.White;
                TxtT1.Foreground = Brushes.White; TxtT2.Foreground = Brushes.White;
                TxtReg1.Foreground = Brushes.White; TxtReg2.Foreground = Brushes.White; TxtReg3.Foreground = Brushes.White;

                BtnWorkspaceSelector.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                PopupBorder.Background = (SolidColorBrush)bc.ConvertFromString("#0b0b2d");
                TxtPopupHeader.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                BtnCloseWindow.Background = (SolidColorBrush)bc.ConvertFromString("#151538");
                MainWindowBorder.BorderBrush = Brushes.White;
                BtnWorkspaceSelector.BorderBrush = Brushes.White;

                if (themeIcon != null) themeIcon.Fill = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                isDarkMode = true;
            }
        }

        // =================================================================
        // VENTANA
        // =================================================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CargarKPIs()
        {
            var activos = new UsuarioDominio.ActivosDominio();
            var colaboradores = new ColaboradorDominio();

            // Card1 - Valor total inventario
            decimal valorTotal = activos.ObtenerValorTotalInventario();
            NumC1.Text = "$" + valorTotal.ToString("N0",new System.Globalization.CultureInfo("es-CO"));

            // Card2 - Total activos
            int totalActivos = activos.ObtenerTotalActivos();
            NumC2.Text = $"{totalActivos} Unidades";

            // Card3 - Total colaboradores
            int totalColaboradores = colaboradores.ObtenerTotalColaboradores();
            NumC3.Text = $"{totalColaboradores} Colaboradores";

            // Card4 - Garantías vigentes
            decimal pctGarantias = activos.ObtenerPorcentajeGarantiasVigentes();
            NumC4.Text = $"{pctGarantias}%";

            CargarDistribucionCategorias();
        }

        private void CargarDistribucionCategorias()
        {
            var dominio = new UsuarioDominio.ActivosDominio();
            DataTable dt = dominio.ObtenerTop5CategoriasPorCantidad();

            var txts = new[] { TxtT1, TxtT2, TxtT3, TxtT4, TxtT5, TxtT6 };
            var vals = new[] { ValT1, ValT2, ValT3, ValT4, ValT5, ValT6 };
            var pbs = new[] { PbT1, PbT2, PbT3, PbT4, PbT5, PbT6 };

            int maxCantidad = 1;
            foreach (DataRow row in dt.Rows)
            {
                int qty = Convert.ToInt32(row["Cantidad"]);
                if (qty > maxCantidad) maxCantidad = qty;
            }

            for (int i = 0; i < Math.Min(dt.Rows.Count, 6); i++)
            {
                int cantidad = Convert.ToInt32(dt.Rows[i]["Cantidad"]);
                txts[i].Text = dt.Rows[i]["Categoria"].ToString();
                vals[i].Text = $"{cantidad} Uds";
                pbs[i].Value = (double)cantidad / maxCantidad * 100;
            }
        }

        private void CargarMapaColombia()
        {
            string rutaHtml = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapaColombia.html");
            string rutaSvg = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "colombia.svg");

            // Validar que ambos archivos existan en la carpeta de ejecución (bin/Debug)
            if (!System.IO.File.Exists(rutaHtml) || !System.IO.File.Exists(rutaSvg))
            {
                System.Diagnostics.Debug.WriteLine("Error: Faltan archivos en el directorio de salida.");
                return;
            }

            string htmlContent = System.IO.File.ReadAllText(rutaHtml);
            string svgContent = System.IO.File.ReadAllText(rutaSvg, System.Text.Encoding.UTF8);

            // Inyectamos el código SVG puro directamente en el DIV del mapa
            htmlContent = htmlContent.Replace("%%MUNDO_SVG%%", svgContent);

            // Enviamos el resultado final al control del formulario
            MapaBrowser.NavigateToString(htmlContent);
        }
    }
}