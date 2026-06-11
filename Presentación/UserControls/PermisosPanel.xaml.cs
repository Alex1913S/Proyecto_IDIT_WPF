using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Presentación
{
    // ═══════════════════════════════════════════════════════════════════
    // MODELO DE DATOS — Permiso atómico
    // ═══════════════════════════════════════════════════════════════════
    public class PermisoItem
    {
        public string Id { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public bool Activo { get; set; }
    }

    public class GrupoPermisos
    {
        public string Nombre { get; set; } = "";
        public string Icono { get; set; } = "";        // Ruta de Path data para el ícono decorativo
        public List<PermisoItem> Permisos { get; set; } = new();
    }

    public class ModuloPermisos
    {
        public string Id { get; set; } = "";
        public string Nombre { get; set; } = "";
        public List<GrupoPermisos> Grupos { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════
    // SERVICIO DE PERMISOS — Singleton para el ciclo de vida del proceso
    // En producción: persistir en SQL tabla Seguridad.PermisosRol
    // ═══════════════════════════════════════════════════════════════════
    public static class PermisosService
    {
        // Diccionario: [Rol -> [PermisoId -> bool]]
        private static Dictionary<string, Dictionary<string, bool>> _permisos = new();

        static PermisosService()
        {
            // Inicializar con defaults para cada rol
            foreach (var rol in new[] { "Operador", "Empleado" })
                _permisos[rol] = new Dictionary<string, bool>();

            AplicarDefaultsOperador();
            AplicarDefaultsEmpleado();
        }

        public static bool Tiene(string rol, string permisoId)
        {
            if (_permisos.TryGetValue(rol, out var mapa))
                return mapa.TryGetValue(permisoId, out bool v) && v;
            return false;
        }

        public static void Establecer(string rol, string permisoId, bool valor)
        {
            if (!_permisos.ContainsKey(rol)) _permisos[rol] = new();
            _permisos[rol][permisoId] = valor;
        }

        public static Dictionary<string, bool> ObtenerTodos(string rol)
        {
            if (!_permisos.ContainsKey(rol)) return new();
            return new Dictionary<string, bool>(_permisos[rol]);
        }

        public static void CargarDesdeDict(string rol, Dictionary<string, bool> mapa)
        {
            _permisos[rol] = new Dictionary<string, bool>(mapa);
        }

        // ─── Defaults para Operador: acceso casi total ───────────────
        private static void AplicarDefaultsOperador()
        {
            var ids = new[]
            {
                // Activos
                "act_menu_ver", "act_menu_submenu",
                "act_ver_lista", "act_filtrar_tabs", "act_buscar", "act_paginar",
                "act_descargar_factura", "act_exportar_excel",
                "act_crear_acceso", "act_crear_paso1", "act_crear_paso2",
                "act_subir_pdf", "act_guardar", "act_cancelar",
                "act_editar_acceso", "act_editar_guardar",
                "act_baja_logica",
                // Categorías
                "cat_menu_ver", "cat_ver_lista", "cat_crear", "cat_editar",
                // Colaboradores
                "col_menu_ver",
                "col_ver_lista", "col_filtrar_tabs", "col_buscar", "col_paginar",
                "col_ver_detalle_panel",
                "col_crear", "col_editar", "col_subir_foto", "col_cambiar_password",
                "col_cambiar_perfil", "col_exportar_excel",
                // Asignaciones
                "asi_menu_ver",
                "asi_ver_lista", "asi_buscar", "asi_paginar", "asi_ver_detalle",
                "asi_crear", "asi_editar", "asi_selec_activo", "asi_selec_colaborador",
                "asi_guardar",
                // Contraseñas
                "cred_menu_ver",
                "cred_ver_lista", "cred_filtrar_tabs", "cred_buscar", "cred_paginar",
                "cred_ver_detalle", "cred_revelar_password",
                "cred_crear", "cred_editar", "cred_eliminar",
                "cred_generar_pass", "cred_ver_notas", "cred_editar_vencimiento",
                // Auditoría
                "aud_menu_ver", "aud_acceso_modulo",
                "aud_consultar", "aud_ver_diff", "aud_exportar_excel",
                "aud_limpiar_filtros", "aud_paginar",
                // Dashboard
                "dash_acceso", "dash_sidebar_colapsar", "dash_selector_workspace",
                "dash_ver_kpis", "dash_ver_grafico", "dash_filtrar_tiempo",
                "dash_ver_categorias", "dash_ver_mapa",
                // Login
                "login_tema",
            };
            foreach (var id in ids) _permisos["Operador"][id] = true;
        }

        // ─── Defaults para Empleado: acceso limitado ─────────────────
        private static void AplicarDefaultsEmpleado()
        {
            var ids = new[]
            {
                // Activos: solo lectura
                "act_menu_ver",
                "act_ver_lista", "act_filtrar_tabs", "act_buscar", "act_paginar",
                "act_descargar_factura",
                // Categorías: solo lectura
                "cat_menu_ver", "cat_ver_lista",
                // Colaboradores: solo lectura
                "col_menu_ver",
                "col_ver_lista", "col_filtrar_tabs", "col_buscar", "col_paginar",
                "col_ver_detalle_panel",
                // Contraseñas propias: CRUD completo
                "cred_menu_ver",
                "cred_ver_lista", "cred_filtrar_tabs", "cred_buscar", "cred_paginar",
                "cred_ver_detalle", "cred_revelar_password",
                "cred_crear", "cred_editar", "cred_eliminar",
                "cred_generar_pass", "cred_ver_notas", "cred_editar_vencimiento",
                // Dashboard básico
                "dash_acceso", "dash_sidebar_colapsar",
                "dash_ver_kpis", "dash_ver_grafico", "dash_filtrar_tiempo",
                "dash_ver_categorias", "dash_ver_mapa",
                // Login
                "login_tema",
            };
            foreach (var id in ids) _permisos["Empleado"][id] = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // USER CONTROL — Panel de permisos superpuesto
    // ═══════════════════════════════════════════════════════════════════
    public partial class PermisosPanel : UserControl
    {
        // ── Estado interno ────────────────────────────────────────────
        private string _rolActual = "Operador";
        private ModuloPermisos? _moduloActual;
        private GrupoPermisos? _grupoActual;
        private int _moduloIndex = 0;

        // Copia de trabajo (se confirma al Guardar)
        private Dictionary<string, bool> _copiaTrabajo = new();
        private Dictionary<string, bool> _copiaTrabajoCopiaOriginal = new();

        // ── Catálogo de módulos (mapeado del código real del proyecto) ─
        private readonly List<ModuloPermisos> _modulos = ConstruirModulos();

        // ─────────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────
        public PermisosPanel()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────────
        // APERTURA DEL PANEL
        // ─────────────────────────────────────────────────────────────
        public void Abrir()
        {
            CargarCopiaTrabajo();
            _moduloIndex = 0;
            RenderizarGrupos();
            ActualizarContadores();
            this.Visibility = Visibility.Visible;
        }

        private void CargarCopiaTrabajo()
        {
            _copiaTrabajo = PermisosService.ObtenerTodos(_rolActual);
            _copiaTrabajoCopiaOriginal = new Dictionary<string, bool>(_copiaTrabajo);
        }

        private bool ObtenerPermiso(string id)
            => _copiaTrabajo.TryGetValue(id, out bool v) && v;

        private void EstablecerPermiso(string id, bool valor)
        {
            _copiaTrabajo[id] = valor;
            ActualizarContadores();
        }

        // ─────────────────────────────────────────────────────────────
        // EVENTOS DE ROL Y TAB
        // ─────────────────────────────────────────────────────────────
        private void RolChanged(object sender, RoutedEventArgs e)
        {
            _rolActual = RbOperador.IsChecked == true ? "Operador" : "Empleado";
            CargarCopiaTrabajo();
            RenderizarGrupos();
            ActualizarContadores();
        }

        private void TabModulo_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                _moduloIndex = rb.Name switch
                {
                    "TabActivos" => 0,
                    "TabColaboradores" => 1,
                    "TabAsignaciones" => 2,
                    "TabContrasenas" => 3,
                    "TabAuditoria" => 4,
                    "TabCategorias" => 5,
                    "TabDashboard" => 6,
                    _ => 0
                };
                _grupoActual = null;
                RenderizarGrupos();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RENDERIZAR GRUPOS (panel izquierdo)
        // ─────────────────────────────────────────────────────────────
        private void RenderizarGrupos()
        {
            PnlGrupos.Children.Clear();
            PnlPermisos.Children.Clear();

            if (_moduloIndex >= _modulos.Count) return;
            _moduloActual = _modulos[_moduloIndex];

            foreach (var grupo in _moduloActual.Grupos)
            {
                var g = grupo; // captura
                int activos = g.Permisos.Count(p => ObtenerPermiso(p.Id));
                int total = g.Permisos.Count;
                bool seleccionado = _grupoActual?.Nombre == g.Nombre;

                var btn = new Button
                {
                    Height = 52,
                    Background = seleccionado
                        ? new SolidColorBrush(Color.FromRgb(0x0d, 0x3a, 0x5c))
                        : Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x0d, 0x3a, 0x5c)),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 0, 0, 2),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                };

                var panelBtn = new Grid { Margin = new Thickness(10, 0, 10, 0) };
                panelBtn.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                panelBtn.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nombreTxt = new TextBlock
                {
                    Text = g.Nombre,
                    FontSize = 12,
                    FontWeight = seleccionado ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = seleccionado
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(Color.FromRgb(0xA0, 0xC4, 0xE0)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(nombreTxt, 0);

                var badge = new Border
                {
                    Background = activos == total
                        ? new SolidColorBrush(Color.FromRgb(0x0d, 0x3a, 0x5c))
                        : new SolidColorBrush(Color.FromRgb(0x15, 0x15, 0x38)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var badgeTxt = new TextBlock
                {
                    Text = $"{activos}/{total}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = activos == total
                        ? new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64))
                        : new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00)),
                };
                badge.Child = badgeTxt;
                Grid.SetColumn(badge, 1);

                panelBtn.Children.Add(nombreTxt);
                panelBtn.Children.Add(badge);
                btn.Content = panelBtn;

                btn.Click += (s, e) =>
                {
                    _grupoActual = g;
                    RenderizarGrupos();
                    RenderizarPermisos();
                };

                // Aplicar template sin borde visible
                var template = new ControlTemplate(typeof(Button));
                var factory = new FrameworkElementFactory(typeof(Border));
                factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                factory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                factory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                factory.AppendChild(cpFactory);
                template.VisualTree = factory;
                btn.Template = template;

                PnlGrupos.Children.Add(btn);
            }

            // Auto-seleccionar primer grupo
            if (_grupoActual == null && _moduloActual.Grupos.Count > 0)
            {
                _grupoActual = _moduloActual.Grupos[0];
                RenderizarGrupos();
                RenderizarPermisos();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RENDERIZAR PERMISOS (panel derecho)
        // ─────────────────────────────────────────────────────────────
        private void RenderizarPermisos()
        {
            PnlPermisos.Children.Clear();
            if (_grupoActual == null) return;

            TxtGrupoNombre.Text = _grupoActual.Nombre;
            TxtGrupoDesc.Text = $"{_grupoActual.Permisos.Count} permisos en este grupo — "
                              + $"{_grupoActual.Permisos.Count(p => ObtenerPermiso(p.Id))} activos";

            foreach (var permiso in _grupoActual.Permisos)
            {
                var p = permiso; // captura
                bool activo = ObtenerPermiso(p.Id);

                var fila = new Border
                {
                    Background = activo
                        ? new SolidColorBrush(Color.FromRgb(0x07, 0x1d, 0x34))
                        : Brushes.Transparent,
                    BorderBrush = activo
                        ? new SolidColorBrush(Color.FromRgb(0x1a, 0x4a, 0x7a))
                        : new SolidColorBrush(Color.FromArgb(40, 0x1a, 0x4a, 0x7a)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });

                // Checkbox visual
                var check = new Border
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(4),
                    BorderBrush = activo
                        ? new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED))
                        : new SolidColorBrush(Color.FromRgb(0x1a, 0x4a, 0x7a)),
                    BorderThickness = new Thickness(1.5),
                    Background = activo
                        ? new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED))
                        : Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (activo)
                {
                    check.Child = new Path
                    {
                        Data = Geometry.Parse("M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z"),
                        Fill = new SolidColorBrush(Color.FromRgb(0x08, 0x21, 0x3a)),
                        Width = 10,
                        Height = 10,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                }
                Grid.SetColumn(check, 0);

                // Texto del permiso
                var txt = new TextBlock
                {
                    Text = p.Descripcion,
                    FontSize = 12,
                    Foreground = activo
                        ? new SolidColorBrush(Color.FromRgb(0xA0, 0xC4, 0xE0))
                        : new SolidColorBrush(Color.FromRgb(0x5a, 0x8a, 0xb0)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0),
                };
                Grid.SetColumn(txt, 1);

                // Indicador LED
                var led = new System.Windows.Shapes.Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = activo
                        ? new SolidColorBrush(Color.FromRgb(0x4C, 0xD9, 0x64))
                        : new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x44)),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(led, 2);

                grid.Children.Add(check);
                grid.Children.Add(txt);
                grid.Children.Add(led);
                fila.Child = grid;

                // Evento toggle al hacer clic
                fila.MouseLeftButtonDown += (s, e) =>
                {
                    EstablecerPermiso(p.Id, !ObtenerPermiso(p.Id));
                    RenderizarPermisos();
                    RenderizarGrupos();
                };

                PnlPermisos.Children.Add(fila);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ACCIONES MASIVAS
        // ─────────────────────────────────────────────────────────────
        private void BtnPermitirTodo_Click(object sender, RoutedEventArgs e)
        {
            foreach (var modulo in _modulos)
                foreach (var grupo in modulo.Grupos)
                    foreach (var permiso in grupo.Permisos)
                        EstablecerPermiso(permiso.Id, true);

            RenderizarGrupos();
            RenderizarPermisos();
        }

        private void BtnDenegarTodo_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                $"¿Deseas denegar TODOS los permisos para el rol '{_rolActual}'?\n\n" +
                "Esta acción puede impedir el acceso al sistema.",
                "Confirmar denegación masiva",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            foreach (var modulo in _modulos)
                foreach (var grupo in modulo.Grupos)
                    foreach (var permiso in grupo.Permisos)
                        EstablecerPermiso(permiso.Id, false);

            RenderizarGrupos();
            RenderizarPermisos();
        }

        private void BtnGrupoPermitir_Click(object sender, RoutedEventArgs e)
        {
            if (_grupoActual == null) return;
            foreach (var p in _grupoActual.Permisos)
                EstablecerPermiso(p.Id, true);
            RenderizarGrupos();
            RenderizarPermisos();
        }

        private void BtnGrupoDenegar_Click(object sender, RoutedEventArgs e)
        {
            if (_grupoActual == null) return;
            foreach (var p in _grupoActual.Permisos)
                EstablecerPermiso(p.Id, false);
            RenderizarGrupos();
            RenderizarPermisos();
        }

        // ─────────────────────────────────────────────────────────────
        // GUARDAR / CERRAR
        // ─────────────────────────────────────────────────────────────
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            PermisosService.CargarDesdeDict(_rolActual, _copiaTrabajo);
            _copiaTrabajoCopiaOriginal = new Dictionary<string, bool>(_copiaTrabajo);

            // TODO: persistir en BD → tabla Seguridad.PermisosRol
            // INSERT/UPDATE Seguridad.PermisosRol (PerfilID, PermisoID, Activo)

            MessageBox.Show(
                $"Configuración de permisos guardada para el rol '{_rolActual}'.\n\n" +
                $"Los cambios se aplicarán en el próximo inicio de sesión de los usuarios afectados.",
                "Permisos actualizados",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            this.Visibility = Visibility.Collapsed;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            // Restaurar copia de trabajo sin guardar
            _copiaTrabajo = new Dictionary<string, bool>(_copiaTrabajoCopiaOriginal);
            this.Visibility = Visibility.Collapsed;
        }

        private void Overlay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Clic en el fondo oscuro cierra el panel
            if (e.Source is System.Windows.Shapes.Rectangle)
                BtnCerrar_Click(sender, e);
        }

        // ─────────────────────────────────────────────────────────────
        // CONTADORES
        // ─────────────────────────────────────────────────────────────
        private void ActualizarContadores()
        {
            var todosLosIds = _modulos
                .SelectMany(m => m.Grupos)
                .SelectMany(g => g.Permisos)
                .Select(p => p.Id)
                .Distinct()
                .ToList();

            int activos = todosLosIds.Count(id => ObtenerPermiso(id));
            int total = todosLosIds.Count;

            TxtContadorActivos.Text = activos.ToString();
            TxtContadorTotal.Text = total.ToString();

            // Calcular diferencias respecto al estado original
            int diffs = todosLosIds.Count(id =>
            {
                bool orig = _copiaTrabajoCopiaOriginal.TryGetValue(id, out bool o) && o;
                bool curr = ObtenerPermiso(id);
                return orig != curr;
            });

            TxtEstadoCambios.Text = diffs == 0
                ? "Sin cambios pendientes"
                : $"{diffs} permiso{(diffs != 1 ? "s" : "")} modificado{(diffs != 1 ? "s" : "")}";

            TxtEstadoCambios.Foreground = diffs == 0
                ? new SolidColorBrush(Color.FromRgb(0x5a, 0x8a, 0xb0))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00));
        }

        // ═══════════════════════════════════════════════════════════════
        // CATÁLOGO DE MÓDULOS Y PERMISOS
        // Mapeado exactamente del código del proyecto SGSI
        // ═══════════════════════════════════════════════════════════════
        private static List<ModuloPermisos> ConstruirModulos()
        {
            return new List<ModuloPermisos>
            {
                // ── ACTIVOS ──────────────────────────────────────────────
                new() {
                    Id = "activos", Nombre = "Inventario de Activos",
                    Grupos = new() {
                        new() { Nombre = "Menú lateral", Permisos = new() {
                            new() { Id="act_menu_ver",     Descripcion="Ver ítem 'Activos Tecnológicos' en el sidebar" },
                            new() { Id="act_menu_submenu", Descripcion="Expandir submenú de activos (Ver Todos / Nuevo Activo / Categoría / Licencias)" },
                        }},
                        new() { Nombre = "See_Assets — Ver Inventario", Permisos = new() {
                            new() { Id="act_ver_lista",         Descripcion="Ver listado de activos en el DataGrid" },
                            new() { Id="act_filtrar_tabs",      Descripcion="Filtrar por estado: Todos / Asignados / En Bodega / En Mantenimiento" },
                            new() { Id="act_buscar",            Descripcion="Buscar activos en tiempo real (TxtBuscarActivo)" },
                            new() { Id="act_paginar",           Descripcion="Navegar entre páginas del inventario" },
                            new() { Id="act_descargar_factura", Descripcion="Descargar factura de compra en PDF (BtnDescargarFactura)" },
                            new() { Id="act_exportar_excel",    Descripcion="Exportar inventario filtrado a Excel" },
                        }},
                        new() { Nombre = "View_Create_Assets — Crear Activo", Permisos = new() {
                            new() { Id="act_crear_acceso",  Descripcion="Acceder al formulario 'Nuevo Activo' desde el submenú" },
                            new() { Id="act_crear_paso1",   Descripcion="Completar Paso 1: Categoría, Estado, Ubicación, Proveedor, Costo, Fecha" },
                            new() { Id="act_crear_paso2",   Descripcion="Completar Paso 2: Marca, Modelo, Serie, RAM, Disco, IP, SO, Procesador, Resolución" },
                            new() { Id="act_subir_pdf",     Descripcion="Adjuntar factura de compra en formato PDF (BtnSeleccionarPdf)" },
                            new() { Id="act_ver_pdf",       Descripcion="Previsualizar PDF adjunto antes de guardar (BtnVerPdf)" },
                            new() { Id="act_guardar",       Descripcion="Guardar nuevo activo en base de datos (INSERT transaccional ActivosBase + EspecificacionesHardware)" },
                            new() { Id="act_cancelar",      Descripcion="Cancelar registro en curso y volver al panel inicial" },
                        }},
                        new() { Nombre = "Activo Existente — Editar / Dar de Baja", Permisos = new() {
                            new() { Id="act_editar_acceso",  Descripcion="Acceder al formulario de edición de un activo existente" },
                            new() { Id="act_editar_guardar", Descripcion="Guardar cambios al activo (UPDATE ActivosBase + EspecificacionesHardware)" },
                            new() { Id="act_baja_logica",    Descripcion="Ejecutar baja lógica del activo (EstadoOperativo = 'De Baja') — solo si no está 'Asignado'" },
                        }},
                    }
                },
 
                // ── COLABORADORES ────────────────────────────────────────
                new() {
                    Id = "colaboradores", Nombre = "Gestión de Colaboradores",
                    Grupos = new() {
                        new() { Nombre = "Menú lateral", Permisos = new() {
                            new() { Id="col_menu_ver", Descripcion="Ver ítem 'Empleados' en el sidebar" },
                        }},
                        new() { Nombre = "Employee_Viewer — Visualización", Permisos = new() {
                            new() { Id="col_ver_lista",         Descripcion="Ver listado de colaboradores en el DataGrid" },
                            new() { Id="col_filtrar_tabs",      Descripcion="Filtrar por perfil: Todos / Administradores / Operadores / Empleados" },
                            new() { Id="col_buscar",            Descripcion="Buscar por cédula, nombre, cargo o correo en tiempo real" },
                            new() { Id="col_paginar",           Descripcion="Navegar entre páginas del listado" },
                            new() { Id="col_exportar_excel",    Descripcion="Exportar colaboradores filtrados a Excel" },
                            new() { Id="col_ver_detalle_panel", Descripcion="Ver panel lateral: foto, nombre, cargo, departamento, estado, fecha de ingreso" },
                        }},
                        new() { Nombre = "Employee_Viewer — CRUD", Permisos = new() {
                            new() { Id="col_crear",           Descripcion="Crear nuevo colaborador (botón '+ Nuevo Colaborador')" },
                            new() { Id="col_editar",          Descripcion="Editar colaborador seleccionado (botón 'Editar Colaborador')" },
                            new() { Id="col_eliminar",        Descripcion="Eliminar colaborador permanentemente (DELETE Core.Colaboradores)" },
                            new() { Id="col_subir_foto",      Descripcion="Seleccionar y cargar foto de perfil desde disco (BtnSeleccionarFoto)" },
                            new() { Id="col_cambiar_password", Descripcion="Establecer o cambiar contraseña de acceso al sistema" },
                            new() { Id="col_cambiar_perfil",  Descripcion="Asignar o cambiar perfil / rol del colaborador (CbFPerfil)" },
                            new() { Id="col_cambiar_depto",   Descripcion="Asignar o cambiar departamento y ubicación (CbFDepartamento, CbFUbicacion)" },
                        }},
                    }
                },
 
                // ── ASIGNACIONES ─────────────────────────────────────────
                new() {
                    Id = "asignaciones", Nombre = "Asignación de Inventario",
                    Grupos = new() {
                        new() { Nombre = "Menú lateral", Permisos = new() {
                            new() { Id="asi_menu_ver", Descripcion="Ver ítem 'Asignaciones' en el sidebar" },
                        }},
                        new() { Nombre = "Assign_Inventory — Visualización", Permisos = new() {
                            new() { Id="asi_ver_lista",    Descripcion="Ver listado de asignaciones activas en el DataGrid" },
                            new() { Id="asi_buscar",       Descripcion="Buscar asignaciones por colaborador, activo o estado" },
                            new() { Id="asi_paginar",      Descripcion="Navegar entre páginas" },
                            new() { Id="asi_ver_detalle",  Descripcion="Ver panel lateral: colaborador, activo asignado, fecha y observaciones" },
                        }},
                        new() { Nombre = "Assign_Inventory — CRUD", Permisos = new() {
                            new() { Id="asi_crear",             Descripcion="Registrar nueva asignación (botón '+ Nueva Asignación')" },
                            new() { Id="asi_editar",            Descripcion="Editar asignación existente (BtnEditarAsignacion)" },
                            new() { Id="asi_selec_activo",      Descripcion="Seleccionar activo disponible en el combo (CmbActivo — solo 'En Bodega')" },
                            new() { Id="asi_selec_colaborador", Descripcion="Seleccionar colaborador destino en el combo (CmbColaborador)" },
                            new() { Id="asi_guardar",           Descripcion="Confirmar asignación y actualizar EstadoOperativo a 'Asignado'" },
                            new() { Id="asi_cancelar",          Descripcion="Cancelar formulario de asignación" },
                        }},
                    }
                },
 
                // ── CONTRASEÑAS ──────────────────────────────────────────
                new() {
                    Id = "contrasenas", Nombre = "Gestor de Contraseñas",
                    Grupos = new() {
                        new() { Nombre = "Menú lateral", Permisos = new() {
                            new() { Id="cred_menu_ver", Descripcion="Ver ítem 'Gestor de Contraseñas' en el sidebar" },
                        }},
                        new() { Nombre = "GestorContrasenas — Visualización", Permisos = new() {
                            new() { Id="cred_ver_lista",        Descripcion="Ver listado de credenciales propias en el DataGrid" },
                            new() { Id="cred_filtrar_tabs",     Descripcion="Filtrar por categoría: Todas / Correo / Sistema Interno / Próximas a vencer" },
                            new() { Id="cred_buscar",           Descripcion="Buscar por nombre de servicio, usuario, categoría o URL" },
                            new() { Id="cred_paginar",          Descripcion="Navegar entre páginas" },
                            new() { Id="cred_ver_detalle",      Descripcion="Ver panel lateral: servicio, categoría, URL, usuario, vencimiento, notas" },
                            new() { Id="cred_revelar_password", Descripcion="Revelar contraseña descifrada AES-256 (BtnMostrarPassword — ícono ojo)" },
                            new() { Id="cred_ver_notas",        Descripcion="Ver notas seguras almacenadas junto a la credencial" },
                        }},
                        new() { Nombre = "GestorContrasenas — CRUD", Permisos = new() {
                            new() { Id="cred_crear",             Descripcion="Crear nueva credencial (botón '+ Nueva Credencial')" },
                            new() { Id="cred_editar",            Descripcion="Editar credencial existente (BtnEditar)" },
                            new() { Id="cred_eliminar",          Descripcion="Eliminar credencial permanentemente (BtnEliminar con confirmación)" },
                            new() { Id="cred_generar_pass",      Descripcion="Generar contraseña segura automática de 16 caracteres (BtnGenerar ⟳)" },
                            new() { Id="cred_editar_vencimiento",Descripcion="Establecer o modificar la fecha de vencimiento de la clave (DpFVencimiento)" },
                            new() { Id="cred_cambiar_categoria", Descripcion="Asignar categoría a la credencial (CbFCategoria)" },
                        }},
                    }
                },
 
                // ── AUDITORÍA ────────────────────────────────────────────
                new() {
                    Id = "auditoria", Nombre = "Log de Auditoría",
                    Grupos = new() {
                        new() { Nombre = "Acceso al módulo", Permisos = new() {
                            new() { Id="aud_menu_ver",      Descripcion="Ver ítem 'Auditoría' en el sidebar" },
                            new() { Id="aud_acceso_modulo", Descripcion="Acceder al UserControl Audit_Log" },
                        }},
                        new() { Nombre = "Audit_Log — Funciones", Permisos = new() {
                            new() { Id="aud_consultar",       Descripcion="Consultar logs por rango de fechas (BtnConsultar)" },
                            new() { Id="aud_ver_diff",        Descripcion="Ver panel de diferencias: valor anterior vs. valor nuevo" },
                            new() { Id="aud_limpiar_filtros", Descripcion="Limpiar filtros y resetear rango de fechas (BtnLimpiar)" },
                            new() { Id="aud_paginar",         Descripcion="Navegar entre páginas de resultados" },
                            new() { Id="aud_exportar_excel",  Descripcion="Exportar log de auditoría a Excel (BtnExportarExcel)" },
                        }},
                    }
                },
 
                // ── CATEGORÍAS DE ACTIVOS ────────────────────────────────
                new() {
                    Id = "categorias", Nombre = "Categorías de Activos",
                    Grupos = new() {
                        new() { Nombre = "Acceso al módulo", Permisos = new() {
                            new() { Id="cat_menu_ver",      Descripcion="Ver ítem 'Categoría Activos' en el submenú de Activos" },
                            new() { Id="cat_acceso_modulo", Descripcion="Acceder al UserControl de gestión de categorías" },
                        }},
                        new() { Nombre = "Gestión de Categorías — CRUD", Permisos = new() {
                            new() { Id="cat_ver_lista",   Descripcion="Ver listado de categorías (ITAM.CategoriasActivo)" },
                            new() { Id="cat_crear",       Descripcion="Crear nueva categoría de activo" },
                            new() { Id="cat_editar",      Descripcion="Editar nombre de categoría existente" },
                            new() { Id="cat_eliminar",    Descripcion="Eliminar categoría (solo si no tiene activos asociados)" },
                            new() { Id="cat_ver_conteo",  Descripcion="Ver conteo de activos por categoría en el panel lateral" },
                        }},
                    }
                },
 
                // ── DASHBOARD ────────────────────────────────────────────
                new() {
                    Id = "dashboard", Nombre = "Dashboard Principal",
                    Grupos = new() {
                        new() { Nombre = "Acceso y navegación", Permisos = new() {
                            new() { Id="dash_acceso",              Descripcion="Acceder al Dashboard tras login exitoso" },
                            new() { Id="dash_sidebar_colapsar",    Descripcion="Colapsar / expandir el sidebar lateral (BtnToggleSidebar)" },
                            new() { Id="dash_selector_workspace",  Descripcion="Usar el selector de workspace (Finanzas / Seguridad / Soporte)" },
                        }},
                        new() { Nombre = "Widgets KPI y Gráficos", Permisos = new() {
                            new() { Id="dash_ver_kpis",       Descripcion="Ver tarjetas KPI: valor total inventario, activos, colaboradores, garantías" },
                            new() { Id="dash_ver_grafico",    Descripcion="Ver gráfico de barras de historial de ingreso de activos" },
                            new() { Id="dash_filtrar_tiempo", Descripcion="Cambiar filtro temporal del gráfico: Día / Mes / Año" },
                            new() { Id="dash_ver_categorias", Descripcion="Ver panel de distribución por categorías con progress bars" },
                            new() { Id="dash_ver_mapa",       Descripcion="Ver mapa de calor de sedes Colombia SVG (WebBrowser MapaBrowser)" },
                        }},
                        new() { Nombre = "Panel de Permisos — Acceso Admin", Permisos = new() {
                            new() { Id="perm_abrir_panel",   Descripcion="Abrir este panel de control de permisos" },
                            new() { Id="perm_modificar_ops", Descripcion="Modificar permisos del rol Operador" },
                            new() { Id="perm_modificar_emp", Descripcion="Modificar permisos del rol Empleado" },
                            new() { Id="perm_guardar",       Descripcion="Guardar cambios de permisos en base de datos" },
                        }},
                        new() { Nombre = "Login — Opciones", Permisos = new() {
                            new() { Id="login_tema",       Descripcion="Alternar tema claro / oscuro en la pantalla de login" },
                            new() { Id="login_registrar",  Descripcion="Acceder a la pestaña 'Sign In' para auto-registro de usuarios" },
                        }},
                    }
                },
            };
        }
    }
}