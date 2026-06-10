using Dominio;
using Presentación.UserControls;
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
        private readonly string _nombre;
        private readonly string _apellido;
        private readonly string _rol;
        private readonly string _cargo;
        private readonly byte[] _foto;
        private readonly int _colaboradorId;

        private bool isDarkMode = true;
        private bool isSidebarCollapsed = false;

        public Dashboard(string username, string accesskey, string rol, string Company_Position, byte[] PictureBPhoto, int colaboradorId)
        {
            InitializeComponent();

            _nombre = username;
            _apellido = accesskey;
            _rol = rol;
            _cargo = Company_Position;
            _foto = PictureBPhoto;
            _colaboradorId = colaboradorId;

            this.Loaded += Dashboard_Loaded;
        }

        public Dashboard() : this("Usuario", "Demo", "Administrador", "Desarrollador TI", null, 0)
        {
        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            if (this.TxtUserName != null)
                TxtUserName.Text = $"{_nombre} {_apellido}";

            if (this.TxtUserRole != null)
                TxtUserRole.Text = $"{_rol} / {_cargo}";

            if (this.TxtUserInitials != null && !string.IsNullOrWhiteSpace(_nombre))
                TxtUserInitials.Text = _nombre.Substring(0, Math.Min(2, _nombre.Length)).ToUpper();

            var culturaEspanol = new System.Globalization.CultureInfo("es-ES");
            string fechaFormateada = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", culturaEspanol);
            LblDate.Text = char.ToUpper(fechaFormateada[0]) + fechaFormateada.Substring(1);
            BtnThemeToggle.IsEnabled = false;
            BtnThemeToggle.Visibility = Visibility.Collapsed;


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
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream(_foto))
                    {
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();

                    ImgUserPhoto.Source = bitmap;
                    ImgUserPhoto.Visibility = Visibility.Visible;
                    if (TxtUserInitials != null) TxtUserInitials.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                if (ImgUserPhoto != null) ImgUserPhoto.Visibility = Visibility.Collapsed;
                if (TxtUserInitials != null) TxtUserInitials.Visibility = Visibility.Visible;
            }
        }

        private void NavegaA(UserControl control, string tituloSeccion = "Panel de Control")
        {
            if (LblMainTitle != null) LblMainTitle.Text = tituloSeccion;

            if (control == null)
            {
                NavWorkspaceContent.Content = null;
                NavWorkspaceContent.Visibility = Visibility.Collapsed;
                PanelInicioView.Visibility = Visibility.Visible;
            }
            else
            {
                PanelInicioView.Visibility = Visibility.Collapsed;
                NavWorkspaceContent.Content = control;
                NavWorkspaceContent.Visibility = Visibility.Visible;
            }
        }

        private void BtnInicio_Click(object sender, RoutedEventArgs e)
            => NavegaA(null, "Panel de Control");

        private void BtnVerTodosActivos_Click(object sender, RoutedEventArgs e)
            => NavegaA(new See_Assets(), "Inventario de Activos");

        private void BtnNuevoActivo_Click(object sender, RoutedEventArgs e)
            => NavegaA(new View_Create_Assets(), "Registrar Nuevo Activo");

        private void BtnEmpleado_Click(object sender, RoutedEventArgs e)
            => NavegaA(new Employee_Viewer(), "Gestión de Colaboradores");

        private void BtnGestorContrasenas_Click(object sender, RoutedEventArgs e)
            => NavegaA(new GestorContrasenas(_colaboradorId), "Gestor de Contraseñas Seguras");

        // Evento para cargar el módulo de asignación de inventario
        private void BtnAssignInventory_Click(object sender, RoutedEventArgs e) =>
            NavegaA(new Assign_Inventory(), "Asignación de Activos");

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

        private void BtnWorkspaceSelector_Click(object sender, RoutedEventArgs e)
            => PopupWorkspace.IsOpen = !PopupWorkspace.IsOpen;

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

        // ═══════════════════════════════════════════════════════════════════════════
        // MODO CLARO / OSCURO
        // Cambios aplicados:
        //   - Sidebar → azul #4B93FF (modo claro) con botones blancos + texto azul
        //   - Cards (1-4) → efecto relieve blanco semitransparente
        //   - GridContainerBorder, TypesContainerBorder, MapContainerBorder → mismo relieve
        //   - TxtC1-TxtC4 → color oscuro visible en modo claro
        //   - Zona del contenedor principal sigue blanca (#F4F6F9)
        // ═══════════════════════════════════════════════════════════════════════════
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var themeIcon = (Path)BtnThemeToggle.Template.FindName("ThemeIcon", BtnThemeToggle);
            var bc = new BrushConverter();

            var txtCategorias = new[] { TxtT1, TxtT2, TxtT3, TxtT4, TxtT5, TxtT6 };
            var valCategorias = new[] { ValT1, ValT2, ValT3, ValT4, ValT5, ValT6 };
            var axisLabels = new[] { AxisL1, AxisL2, AxisL3, AxisL4, AxisL5, AxisL6 };
            var whiteBars = new[] { Bar1, Bar3, Bar5 };
            var kpiCards = new[] { Card1, Card2, Card3, Card4 };
            var kpiTitles = new[] { TxtC1, TxtC2, TxtC3, TxtC4 };
            var kpiNumbers = new[] { NumC1, NumC2, NumC3, NumC4 };
            var contentPanels = new[] { GridContainerBorder, TypesContainerBorder, MapContainerBorder };

            if (isDarkMode)
            {
                // ════════════ MODO CLARO ════════════

                MainWindowBorder.Background = (SolidColorBrush)bc.ConvertFromString("#F4F6F9");
                MainWindowBorder.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");

                SidebarBorder.Background = (SolidColorBrush)bc.ConvertFromString("#4B93FF");

                TxtUserName.Foreground = Brushes.White;
                TxtUserRole.Foreground = (SolidColorBrush)bc.ConvertFromString("#D6E8FF");

                if (themeIcon != null) themeIcon.Fill = Brushes.White;

                AplicarEstilosBotonesSidebar(MenuStackPanel, modoClaro: true);
                AplicarColorIconosSidebar(MenuStackPanel, Brushes.White);

                SubmenuActivos.Background = (SolidColorBrush)bc.ConvertFromString("#3A7FE0");
                SubmenuActivos.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#2563EB");

                var cardLightBg = (SolidColorBrush)bc.ConvertFromString("#E8F0FF");
                var cardLightBorder = (SolidColorBrush)bc.ConvertFromString("#BFCFE8");
                foreach (var card in kpiCards)
                {
                    card.Background = cardLightBg;
                    card.BorderBrush = cardLightBorder;
                    card.BorderThickness = new Thickness(1);
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 18,
                        ShadowDepth = 5,
                        Opacity = 0.18,
                        Color = Color.FromRgb(0x4B, 0x93, 0xFF),
                        Direction = 270
                    };
                }

                foreach (var t in kpiTitles) if (t != null) t.Foreground = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                foreach (var n in kpiNumbers) if (n != null) n.Foreground = (SolidColorBrush)bc.ConvertFromString("#22223B");

                var panelLightBg = (SolidColorBrush)bc.ConvertFromString("#EDF2FF");
                var panelLightBorder = (SolidColorBrush)bc.ConvertFromString("#C3D3F0");
                foreach (var panel in contentPanels)
                {
                    panel.Background = panelLightBg;
                    panel.BorderBrush = panelLightBorder;
                    panel.BorderThickness = new Thickness(1);
                    panel.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 20,
                        ShadowDepth = 6,
                        Opacity = 0.16,
                        Color = Color.FromRgb(0x4B, 0x93, 0xFF),
                        Direction = 270
                    };
                }

                SolidColorBrush darkText = (SolidColorBrush)bc.ConvertFromString("#1E3A5F");
                SolidColorBrush greyText = (SolidColorBrush)bc.ConvertFromString("#4A6080");
                SolidColorBrush darkGreyBars = (SolidColorBrush)bc.ConvertFromString("#4B93FF");

                LblMainTitle.Foreground = darkText;
                LblDate.Foreground = greyText;
                LblChartTitle.Foreground = darkText;
                LblChartSub.Foreground = greyText;
                LblTypesTitle.Foreground = darkText;
                TxtReg1.Foreground = darkText;
                TxtReg2.Foreground = darkText;
                TxtReg3.Foreground = darkText;

                foreach (var t in txtCategorias) if (t != null) t.Foreground = darkText;
                foreach (var v in valCategorias) if (v != null) v.Foreground = greyText;
                foreach (var axis in axisLabels) if (axis != null) axis.Foreground = greyText;
                foreach (var bar in whiteBars) if (bar != null) bar.Background = darkGreyBars;

                TimeFilterPanel.Background = (SolidColorBrush)bc.ConvertFromString("#D6E4FF");

                BtnWorkspaceSelector.Background = Brushes.White;
                BtnWorkspaceSelector.BorderBrush = (SolidColorBrush)bc.ConvertFromString("#CBD5E1");
                PopupBorder.Background = Brushes.White;
                TxtPopupHeader.Foreground = (SolidColorBrush)bc.ConvertFromString("#6C757D");
                BtnCloseWindow.Background = (SolidColorBrush)bc.ConvertFromString("#E2E8F0");

                var txtSel = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentWorkspace", BtnWorkspaceSelector);
                var txtSelSub = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentSub", BtnWorkspaceSelector);
                if (txtSel != null) txtSel.Foreground = darkText;
                if (txtSelSub != null) txtSelSub.Foreground = greyText;

                // ── Propagar tema a UserControls activos ─────────────────────
                if (NavWorkspaceContent.Content is Employee_Viewer ev)
                    ev.AplicarTema(true);

                isDarkMode = false;
            }
            else
            {
                // ════════════ MODO OSCURO (restaurar) ════════════

                MainWindowBorder.Background = (SolidColorBrush)bc.ConvertFromString("#08213a");
                MainWindowBorder.BorderBrush = Brushes.White;

                SidebarBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");

                TxtUserName.Foreground = Brushes.White;
                TxtUserRole.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");

                if (themeIcon != null) themeIcon.Fill = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");

                AplicarEstilosBotonesSidebar(MenuStackPanel, modoClaro: false);
                AplicarColorIconosSidebar(MenuStackPanel, (SolidColorBrush)bc.ConvertFromString("#A0A0B8"));

                SubmenuActivos.Background = (SolidColorBrush)bc.ConvertFromString("#071d34");
                SubmenuActivos.BorderBrush = Brushes.White;

                var cardDarkBg = (SolidColorBrush)bc.ConvertFromString("#09274c");
                var cardDarkBorder = (SolidColorBrush)bc.ConvertFromString("#25FFFFFF");
                foreach (var card in kpiCards)
                {
                    card.Background = cardDarkBg;
                    card.BorderBrush = cardDarkBorder;
                    card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 18,
                        ShadowDepth = 4,
                        Opacity = 0.35,
                        Color = Color.FromRgb(0x00, 0x00, 0x20),
                        Direction = 270
                    };
                }

                foreach (var t in kpiTitles) if (t != null) t.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                foreach (var n in kpiNumbers) if (n != null) n.Foreground = Brushes.White;

                var panelDarkBg = (SolidColorBrush)bc.ConvertFromString("#09274c");
                var panelDarkBorder = (SolidColorBrush)bc.ConvertFromString("#25FFFFFF");
                foreach (var panel in contentPanels)
                {
                    panel.Background = panelDarkBg;
                    panel.BorderBrush = panelDarkBorder;
                    panel.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 20,
                        ShadowDepth = 5,
                        Opacity = 0.4,
                        Color = Color.FromRgb(0x00, 0x00, 0x20),
                        Direction = 270
                    };
                }

                LblMainTitle.Foreground = Brushes.White;
                LblDate.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                LblChartTitle.Foreground = Brushes.White;
                LblChartSub.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                LblTypesTitle.Foreground = Brushes.White;
                TxtReg1.Foreground = Brushes.White;
                TxtReg2.Foreground = Brushes.White;
                TxtReg3.Foreground = Brushes.White;

                foreach (var t in txtCategorias) if (t != null) t.Foreground = Brushes.White;
                foreach (var v in valCategorias) if (v != null) v.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                foreach (var axis in axisLabels) if (axis != null) axis.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                foreach (var bar in whiteBars) if (bar != null) bar.Background = Brushes.White;

                TimeFilterPanel.Background = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");

                BtnWorkspaceSelector.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                BtnWorkspaceSelector.BorderBrush = Brushes.White;
                PopupBorder.Background = (SolidColorBrush)bc.ConvertFromString("#09274c");
                TxtPopupHeader.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                BtnCloseWindow.Background = (SolidColorBrush)bc.ConvertFromString("#0d3a5c");

                var txtSel = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentWorkspace", BtnWorkspaceSelector);
                var txtSelSub = (TextBlock)BtnWorkspaceSelector.Template.FindName("TxtCurrentSub", BtnWorkspaceSelector);
                if (txtSel != null) txtSel.Foreground = Brushes.White;
                if (txtSelSub != null) txtSelSub.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");

                // ── Propagar tema a UserControls activos ─────────────────────
                if (NavWorkspaceContent.Content is Employee_Viewer ev)
                    ev.AplicarTema(false);

                isDarkMode = true;
            }
        }


        // ═══════════════════════════════════════════════════════════════════════════
        // HELPER: Aplica estilos visuales a los botones del sidebar según el modo
        // En modo claro: fondo blanco semitransparente, texto/ícono en azul oscuro
        // En modo oscuro: restaura transparente con foreground #A0A0B8
        // ═══════════════════════════════════════════════════════════════════════════
        private void AplicarEstilosBotonesSidebar(StackPanel container, bool modoClaro)
        {
            var bc = new BrushConverter();

            foreach (var child in container.Children)
            {
                if (child is Button btn)
                {
                    if (modoClaro)
                    {
                        // Fondo: blanco semitransparente para dar sensación de botón sobre fondo azul
                        btn.Background = (SolidColorBrush)bc.ConvertFromString("#20FFFFFF");
                        btn.Foreground = Brushes.White;
                    }
                    else
                    {
                        btn.Background = Brushes.Transparent;
                        btn.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                    }
                }

                // Submenú también
                if (child is Border border && border.Child is StackPanel subPanel)
                {
                    if (modoClaro)
                        border.Background = (SolidColorBrush)bc.ConvertFromString("#3A7FE0");

                    foreach (var sub in subPanel.Children)
                    {
                        if (sub is Button subBtn)
                        {
                            subBtn.Foreground = modoClaro ? Brushes.White
                                : (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                        }
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // HELPER: Cambia el color de los íconos (Path) dentro de los botones del sidebar
        // Usa reflection sobre el ContentTemplate del botón para encontrar el Path
        // ═══════════════════════════════════════════════════════════════════════════
        private void AplicarColorIconosSidebar(StackPanel container, Brush color)
        {
            foreach (var child in container.Children)
            {
                if (child is Button btn && btn.IsLoaded)
                {
                    // Buscar el ContentPresenter nombrado "IconPresenter" en el template
                    var iconPresenter = btn.Template?.FindName("IconPresenter", btn) as ContentPresenter;
                    if (iconPresenter != null)
                    {
                        // Recorrer el visual tree del ContentPresenter buscando Path
                        var path = EncontrarPath(iconPresenter);
                        if (path != null) path.Fill = color;
                    }
                }
            }
        }

        private Path EncontrarPath(DependencyObject padre)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(padre); i++)
            {
                var hijo = System.Windows.Media.VisualTreeHelper.GetChild(padre, i);
                if (hijo is Path path) return path;
                var resultado = EncontrarPath(hijo);
                if (resultado != null) return resultado;
            }
            return null;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
            => Environment.Exit(0);

        private void LogOut_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void CargarKPIs()
        {
            var activos = new UsuarioDominio.ActivosDominio();
            var colaboradores = new ColaboradorDominio();

            decimal valorTotal = activos.ObtenerValorTotalInventario();
            NumC1.Text = "$" + valorTotal.ToString("N0", new System.Globalization.CultureInfo("es-CO"));

            int totalActivos = activos.ObtenerTotalActivos();
            NumC2.Text = $"{totalActivos} Unidades";

            int totalColaboradores = colaboradores.ObtenerTotalColaboradores();
            NumC3.Text = $"{totalColaboradores} Colaboradores";

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

            if (!System.IO.File.Exists(rutaHtml) || !System.IO.File.Exists(rutaSvg))
            {
                System.Diagnostics.Debug.WriteLine("Error: Faltan archivos en el directorio de salida.");
                return;
            }

            string htmlContent = System.IO.File.ReadAllText(rutaHtml);
            string svgContent = System.IO.File.ReadAllText(rutaSvg, System.Text.Encoding.UTF8);

            htmlContent = htmlContent.Replace("%%MUNDO_SVG%%", svgContent);
            MapaBrowser.NavigateToString(htmlContent);
        }
    }
}