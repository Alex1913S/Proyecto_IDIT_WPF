using Dominio;
using Microsoft.Data.SqlClient;
using Presentación.UserControls;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;
using System.Globalization;
using Presentación.Controls;


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

        private DataTable _dtIngresosDia;
        private DataTable _dtIngresosMes;
        private DataTable _dtIngresosAnio;
        private string _filtroGraficoActual = "Día";

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

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
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

            // 🔥 Como este método (Dashboard_Loaded) ya es 'async void', 
            // ahora sí te dejará usar el 'await' aquí sin ningún error de compilación.
            await CargarKPIsAsync();

            ConfigurarInterfazPorPerfil();
        }

        private async Task CargarKPIsAsync()
        {
            try
            {
                var activos = new UsuarioDominio.ActivosDominio();
                var colaboradores = new ColaboradorDominio();

                decimal valorTotal = await Task.Run(() => activos.ObtenerValorTotalInventario());
                int totalActivos = await Task.Run(() => activos.ObtenerTotalActivos());
                int totalColaboradores = await Task.Run(() => colaboradores.ObtenerTotalColaboradores());
                decimal pctGarantias = await Task.Run(() => activos.ObtenerPorcentajeGarantiasVigentes());
                DataTable dtCategorias = await Task.Run(() => activos.ObtenerTop5CategoriasPorCantidad());

                // 🔥 NUEVO: ingresos por período para el gráfico
                _dtIngresosDia = await Task.Run(() => activos.ObtenerIngresosPorDia());
                _dtIngresosMes = await Task.Run(() => activos.ObtenerIngresosPorMes());
                _dtIngresosAnio = await Task.Run(() => activos.ObtenerIngresosPorAnio());

                NumC1.Text = "$" + valorTotal.ToString("N0", new CultureInfo("es-CO"));
                NumC2.Text = $"{totalActivos} Unidades";
                NumC3.Text = $"{totalColaboradores} Colaboradores";
                NumC4.Text = $"{pctGarantias}%";

                RenderizarDistribucionCategorias(dtCategorias);
                RenderizarGraficoIngresos(_filtroGraficoActual); // 🔥 pinta el gráfico con datos reales
            }
            catch (Exception)
            {
                // Manejo de excepciones silencioso o log estructurado
            }
        }

        private void RenderizarDistribucionCategorias(DataTable dt)
        {
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

        private void RenderizarGraficoIngresos(string filtro)
        {
            _filtroGraficoActual = filtro;

            var bars = new[] { Bar1, Bar2, Bar3, Bar4, Bar5, Bar6 };
            var labels = new[] { AxisL1, AxisL2, AxisL3, AxisL4, AxisL5, AxisL6 };

            string[] etiquetas = new string[6];
            int[] valores = new int[6];
            var culturaEs = new CultureInfo("es-ES");

            if (filtro == "Día")
            {
                DateTime hoy = DateTime.Today;
                var nombresDias = culturaEs.DateTimeFormat.AbbreviatedDayNames;
                for (int i = 0; i < 6; i++)
                {
                    DateTime fecha = hoy.AddDays(-5 + i);
                    etiquetas[i] = CapitalizarPrimera(nombresDias[(int)fecha.DayOfWeek]);
                    valores[i] = ObtenerCantidadPorFecha(_dtIngresosDia, fecha);
                }
            }
            else if (filtro == "Mes")
            {
                DateTime mesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var nombresMeses = culturaEs.DateTimeFormat.AbbreviatedMonthNames;
                for (int i = 0; i < 6; i++)
                {
                    DateTime mes = mesActual.AddMonths(-5 + i);
                    etiquetas[i] = CapitalizarPrimera(nombresMeses[mes.Month - 1]);
                    valores[i] = ObtenerCantidadPorFecha(_dtIngresosMes, mes);
                }
            }
            else // "Año"
            {
                int anioActual = DateTime.Today.Year;
                for (int i = 0; i < 6; i++)
                {
                    int anio = anioActual - 5 + i;
                    etiquetas[i] = anio == anioActual ? $"{anio}*" : anio.ToString();
                    valores[i] = ObtenerCantidadPorAnio(_dtIngresosAnio, anio);
                }
            }

            int maxValor = Math.Max(1, valores.Max());
            const double alturaMaxima = 140;

            for (int i = 0; i < 6; i++)
            {
                labels[i].Text = etiquetas[i];
                bars[i].Height = valores[i] == 0 ? 4 : Math.Max(8, (valores[i] / (double)maxValor) * alturaMaxima);
                bars[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B93FF"));
                bars[i].ToolTip = $"{etiquetas[i]}: {valores[i]} activo(s) ingresado(s)";
            }

            // El año en curso solo refleja datos reales hasta el mes presente
            AxisL6.ToolTip = filtro == "Año"
                ? "* Año en curso: cifra parcial hasta el mes actual"
                : null;
        }

        private int ObtenerCantidadPorFecha(DataTable dt, DateTime fechaBuscada)
        {
            if (dt == null) return 0;
            foreach (DataRow row in dt.Rows)
            {
                if (row["Periodo"] == DBNull.Value) continue;
                if (Convert.ToDateTime(row["Periodo"]).Date == fechaBuscada.Date)
                    return Convert.ToInt32(row["Cantidad"]);
            }
            return 0;
        }

        private int ObtenerCantidadPorAnio(DataTable dt, int anio)
        {
            if (dt == null) return 0;
            foreach (DataRow row in dt.Rows)
            {
                if (row["Periodo"] == DBNull.Value) continue;
                if (Convert.ToInt32(row["Periodo"]) == anio)
                    return Convert.ToInt32(row["Cantidad"]);
            }
            return 0;
        }

        private string CapitalizarPrimera(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).TrimEnd('.');

        /// <summary>
        /// Centraliza la visibilidad de los componentes usando búsquedas dinámicas seguras por palabras clave.
        /// Evita errores si los botones no tienen un x:Name directo en el XAML.
        /// </summary>
        private void ConfigurarInterfazPorPerfil()
        {
            if (string.IsNullOrWhiteSpace(_rol)) return;

            string rolFormateado = _rol.Trim().ToUpper();

            // El Administrador SIEMPRE tiene acceso total; no pasa por el motor de permisos.
            if (rolFormateado == "ADMINISTRADOR")
            {
                EstablecerVisibilidadBoton("Nuevo", Visibility.Visible);
                EstablecerVisibilidadBoton("Asign", Visibility.Visible);
                EstablecerVisibilidadBoton("Assign", Visibility.Visible);
                EstablecerVisibilidadBoton("Empleado", Visibility.Visible);
                EstablecerVisibilidadBoton("Colaborador", Visibility.Visible);
                EstablecerVisibilidadBoton("Auditor", Visibility.Visible);
                EstablecerVisibilidadBoton("Auditoria", Visibility.Visible);
                return;
            }

            // Operador y Empleado: la visibilidad depende EXCLUSIVAMENTE
            // de lo configurado en el Panel de Permisos (PermisosService).
            string rolPermiso = NormalizarRolPermiso(_rol);

            bool puedeNuevoActivo = PermisosService.Tiene(rolPermiso, "act_crear_acceso");
            bool puedeAsignaciones = PermisosService.Tiene(rolPermiso, "asi_menu_ver");
            bool puedeColaboradores = PermisosService.Tiene(rolPermiso, "col_menu_ver");
            bool puedeAuditoria = PermisosService.Tiene(rolPermiso, "aud_menu_ver");

            EstablecerVisibilidadBoton("Nuevo", puedeNuevoActivo ? Visibility.Visible : Visibility.Collapsed);
            EstablecerVisibilidadBoton("Asign", puedeAsignaciones ? Visibility.Visible : Visibility.Collapsed);
            EstablecerVisibilidadBoton("Assign", puedeAsignaciones ? Visibility.Visible : Visibility.Collapsed);
            EstablecerVisibilidadBoton("Empleado", puedeColaboradores ? Visibility.Visible : Visibility.Collapsed);
            EstablecerVisibilidadBoton("Colaborador", puedeColaboradores ? Visibility.Visible : Visibility.Collapsed);
            EstablecerVisibilidadBoton("Auditor", puedeAuditoria ? Visibility.Visible : Visibility.Collapsed);
            EstablecerVisibilidadBoton("Auditoria", puedeAuditoria ? Visibility.Visible : Visibility.Collapsed);

            if (SubmenuActivos != null && !puedeNuevoActivo)
                SubmenuActivos.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Busca botones en el árbol visual del menú lateral y aplica visibilidad por coincidencias.
        /// </summary>
        private void EstablecerVisibilidadBoton(string palabraClave, Visibility visibilidad)
        {
            if (MenuStackPanel != null)
            {
                foreach (var child in MenuStackPanel.Children)
                {
                    if (VerificarYAplicarBoton(child, palabraClave, visibilidad)) continue;

                    if (child is Border border && border.Child is Panel subPanel)
                    {
                        foreach (var subChild in subPanel.Children)
                        {
                            VerificarYAplicarBoton(subChild, palabraClave, visibilidad);
                        }
                    }
                }
            }

            if (SubmenuActivos != null)
            {
                if (SubmenuActivos.Child is Panel panelInterno)
                {
                    foreach (var child in panelInterno.Children)
                    {
                        VerificarYAplicarBoton(child, palabraClave, visibilidad);
                    }
                }
                else
                {
                    VerificarYAplicarBoton(SubmenuActivos.Child, palabraClave, visibilidad);
                }
            }
        }

        private string NormalizarRolPermiso(string rol)
        {
            if (string.IsNullOrWhiteSpace(rol)) return "Empleado";
            string r = rol.Trim().ToUpper();
            if (r == "OPERADOR") return "Operador";
            if (r == "EMPLEADO") return "Empleado";
            return rol.Trim();
        }

        /// <summary>
        /// Compara el x:Name o el contenido de texto interno de un control con la palabra clave.
        /// </summary>
        private bool VerificarYAplicarBoton(object element, string palabraClave, Visibility visibilidad)
        {
            if (element is Button btn)
            {
                string nombre = btn.Name ?? "";
                string contenido = "";

                if (btn.Content != null)
                {
                    contenido = btn.Content.ToString();
                    if (btn.Content is StackPanel panel)
                    {
                        contenido = "";
                        foreach (var child in panel.Children)
                        {
                            if (child is TextBlock tb) contenido += " " + tb.Text;
                        }
                    }
                }

                if (nombre.IndexOf(palabraClave, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    contenido.IndexOf(palabraClave, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    btn.Visibility = visibilidad;
                    return true;
                }
            }
            return false;
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

        /// <summary>
        /// Control de Navegación con capa de Seguridad Perimetral integrada.
        /// </summary>
        private void NavegaA(UserControl control, string tituloSeccion = "Panel de Control")
        {
            if (control != null)
            {
                string rolFormateado = (!string.IsNullOrEmpty(_rol)) ? _rol.Trim().ToUpper() : "EMPLEADO";

                // El Panel de Permisos es EXCLUSIVO del Administrador, sin excepción.
                if (control is PermisosPanel && rolFormateado != "ADMINISTRADOR")
                {
                    MessageBox.Show("Acceso denegado. Este módulo está reservado exclusivamente para perfiles de nivel Administrador.",
                        "Seguridad del Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Administrador tiene acceso total y no pasa por el motor de permisos.
                if (rolFormateado != "ADMINISTRADOR" && control is not PermisosPanel)
                {
                    string rolPermiso = NormalizarRolPermiso(_rol);

                    string permisoRequerido = control switch
                    {
                        Employee_Viewer => "col_menu_ver",
                        Audit_Log => "aud_acceso_modulo",
                        View_Create_Assets => "act_crear_acceso",
                        Assign_Inventory => "asi_menu_ver",
                        See_Assets => "act_ver_lista",
                        GestorContrasenas => "cred_menu_ver",
                        _ => null
                    };

                    if (permisoRequerido != null && !PermisosService.Tiene(rolPermiso, permisoRequerido))
                    {
                        MessageBox.Show("Acceso restringido. No cuenta con los privilegios necesarios para acceder a esta sección.",
                            "Seguridad del Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

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

        private void BtnAssignInventory_Click(object sender, RoutedEventArgs e)
            => NavegaA(new Assign_Inventory(), "Asignación de Activos");

        private void BtnAuditoria_Click(object sender, RoutedEventArgs e)
            => NavegaA(new Audit_Log(), "Auditoría");


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

        private void BtnActaEntrega_Click(object sender, RoutedEventArgs e)
    => NavegaA(new Presentación.UserControls.ActaEntrega(), "Actas de Entrega de Equipos");

        private void WorkspaceItem3_Click(object sender, RoutedEventArgs e)
    => NavegaA(new Presentación.UserControls.MantenimientoTI(_colaboradorId), "Mantenimiento TI");

        private void TimeFilter_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            if (sender is RadioButton rb)
            {
                RenderizarGraficoIngresos(rb.Content.ToString());
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
                    if (btn.Content is StackPanel panel)
                    {
                        content = "";
                        foreach (var child in panel.Children) if (child is TextBlock tb) content += tb.Text + " ";
                    }

                    string rolFormateado = (!string.IsNullOrEmpty(_rol)) ? _rol.Trim().ToUpper() : "EMPLEADO";

                    if (content.Contains("Finanzas"))
                    {
                        txtCurrent.Text = "Finanzas & Control";
                        txtSub.Text = "Área Contable";
                    }
                    else if (content.Contains("Seguridad"))
                    {
                        txtCurrent.Text = "Seguridad SGSI";
                        txtSub.Text = "Auditoría de Riesgos";
                    }
                    else if (content.Contains("Soporte"))
                    {
                        if (rolFormateado != "ADMINISTRADOR")
                        {
                            NotificacionService.Error(
                                "El entorno de Soporte Técnico y Mantenimiento TI está reservado para Administradores.",
                                "Acceso Denegado");
                            PopupWorkspace.IsOpen = false;
                            return;
                        }

                        txtCurrent.Text = "Soporte Técnico";
                        txtSub.Text = "Mantenimiento TI";

                        NavegaA(new Presentación.UserControls.MantenimientoTI(_colaboradorId), "Mantenimiento TI");
                    }
                    else if (content.Contains("Gobernanza"))
                    {
                        if (rolFormateado != "ADMINISTRADOR")
                        {
                            NotificacionService.Error(
                                "El entorno de Gobernanza TI y Control de Permisos está reservado para Administradores.",
                                "Acceso Denegado");
                            PopupWorkspace.IsOpen = false;
                            return;
                        }

                        txtCurrent.Text = "Gobernanza TI";
                        txtSub.Text = "Control de Accesos";

                        var permisosPanel = new PermisosPanel();
                        NavegaA(permisosPanel, "Control de Permisos");
                    }
                }
                PopupWorkspace.IsOpen = false;
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var themeIcon = (Path)BtnThemeToggle.Template.FindName("ThemeIcon", BtnThemeToggle);
            var bc = new BrushConverter();

            var txtCategorias = new[] { TxtT1, TxtT2, TxtT3, TxtT4, TxtT5, TxtT6 };
            var valCategorias = new[] { ValT1, ValT2, ValT3, ValT4, ValT5, ValT6 };
            var axisLabels = new[] { AxisL1, AxisL2, AxisL3, AxisL4, AxisL5, AxisL6 };
            var kpiCards = new[] { Card1, Card2, Card3, Card4 };
            var kpiTitles = new[] { TxtC1, TxtC2, TxtC3, TxtC4 };
            var kpiNumbers = new[] { NumC1, NumC2, NumC3, NumC4 };
            var contentPanels = new[] { GridContainerBorder, TypesContainerBorder, MapContainerBorder };

            if (isDarkMode)
            {
                // MODO CLARO
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

                if (NavWorkspaceContent.Content is Employee_Viewer ev)
                    ev.AplicarTema(true);

                isDarkMode = false;
            }
            else
            {
                // MODO OSCURO
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

                if (NavWorkspaceContent.Content is Employee_Viewer ev)
                    ev.AplicarTema(false);

                isDarkMode = true;
            }
        }

        private void AplicarEstilosBotonesSidebar(StackPanel container, bool modoClaro)
        {
            var bc = new BrushConverter();

            foreach (var child in container.Children)
            {
                if (child is Button btn)
                {
                    if (modoClaro)
                    {
                        btn.Background = (SolidColorBrush)bc.ConvertFromString("#20FFFFFF");
                        btn.Foreground = Brushes.White;
                    }
                    else
                    {
                        btn.Background = Brushes.Transparent;
                        btn.Foreground = (SolidColorBrush)bc.ConvertFromString("#A0A0B8");
                    }
                }

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

        private void AplicarColorIconosSidebar(StackPanel container, Brush color)
        {
            foreach (var child in container.Children)
            {
                if (child is Button btn && btn.IsLoaded)
                {
                    var iconPresenter = btn.Template?.FindName("IconPresenter", btn) as ContentPresenter;
                    if (iconPresenter != null)
                    {
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
    }
}