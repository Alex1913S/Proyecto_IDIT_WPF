using Dominio;
using Presentación.Controls;
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
        private static readonly PermisosDominio _dominio = new();

        // Caché en memoria por sesión de aplicación (evita ir a BD en cada .Tiene())
        private static readonly Dictionary<string, Dictionary<string, bool>> _cache = new();

        public static bool Tiene(string rol, string permisoId)
        {
            AsegurarCargado(rol);
            return _cache[rol].TryGetValue(permisoId, out bool v) && v;
        }

        public static void Establecer(string rol, string permisoId, bool valor)
        {
            AsegurarCargado(rol);
            _cache[rol][permisoId] = valor;
            // Nota: esto solo cambia la copia en memoria; persistir se hace con CargarDesdeDict (Guardar)
        }

        public static Dictionary<string, bool> ObtenerTodos(string rol)
        {
            AsegurarCargado(rol);
            return new Dictionary<string, bool>(_cache[rol]);
        }

        /// <summary>
        /// Persiste la copia de trabajo en BD y actualiza la caché.
        /// </summary>
        public static void CargarDesdeDict(string rol, Dictionary<string, bool> mapa)
        {
            try
            {
                _dominio.Guardar(rol, mapa);
                _cache[rol] = new Dictionary<string, bool>(mapa);
            }
            catch
            {
                // Re-lanzar para que la UI (BtnGuardar_Click) muestre el error al usuario
                throw;
            }
        }

        private static void AsegurarCargado(string rol)
        {
            if (_cache.ContainsKey(rol)) return;

            try
            {
                if (_dominio.ExistenPermisos(rol))
                {
                    _cache[rol] = _dominio.Obtener(rol);
                }
                else
                {
                    // Primera vez que se usa este rol: sembrar defaults y guardarlos
                    var defaults = rol == "Operador" ? DefaultsOperador() : DefaultsEmpleado();
                    _dominio.Guardar(rol, defaults);
                    _cache[rol] = defaults;
                }
            }
            catch
            {
                // Si falla la BD (ej. sin conexión), usar defaults solo en memoria para no romper la app
                _cache[rol] = rol == "Operador" ? DefaultsOperador() : DefaultsEmpleado();
            }
        }

        private static Dictionary<string, bool> DefaultsOperador()
        {
            var ids = new[]
            {
                "act_menu_ver", "act_menu_submenu",
                "act_ver_lista", "act_filtrar_tabs", "act_buscar", "act_paginar",
                "act_descargar_factura", "act_exportar_excel",
                "act_crear_acceso", "act_crear_paso1", "act_crear_paso2",
                "act_subir_pdf", "act_guardar", "act_cancelar",
                "act_editar_acceso", "act_editar_guardar",
                "act_baja_logica",
                "cat_menu_ver", "cat_ver_lista", "cat_crear", "cat_editar",
                "col_menu_ver",
                "col_ver_lista", "col_filtrar_tabs", "col_buscar", "col_paginar",
                "col_ver_detalle_panel",
                "col_crear", "col_editar", "col_subir_foto", "col_cambiar_password",
                "col_cambiar_perfil", "col_exportar_excel",
                "asi_menu_ver",
                "asi_ver_lista", "asi_buscar", "asi_paginar", "asi_ver_detalle",
                "asi_crear", "asi_editar", "asi_selec_activo", "asi_selec_colaborador",
                "asi_guardar",
                "cred_menu_ver",
                "cred_ver_lista", "cred_filtrar_tabs", "cred_buscar", "cred_paginar",
                "cred_ver_detalle", "cred_revelar_password",
                "cred_crear", "cred_editar", "cred_eliminar",
                "cred_generar_pass", "cred_ver_notas", "cred_editar_vencimiento",
                "aud_menu_ver", "aud_acceso_modulo",
                "aud_consultar", "aud_ver_diff", "aud_exportar_excel",
                "aud_limpiar_filtros", "aud_paginar",
                "dash_acceso", "dash_sidebar_colapsar", "dash_selector_workspace",
                "dash_ver_kpis", "dash_ver_grafico", "dash_filtrar_tiempo",
                "dash_ver_categorias", "dash_ver_mapa",
                "login_tema",
            };
            var dict = new Dictionary<string, bool>();
            foreach (var id in ids) dict[id] = true;
            return dict;
        }

        private static Dictionary<string, bool> DefaultsEmpleado()
        {
            var ids = new[]
            {
                "act_menu_ver",
                "act_ver_lista", "act_filtrar_tabs", "act_buscar", "act_paginar",
                "act_descargar_factura",
                "cat_menu_ver", "cat_ver_lista",
                "col_menu_ver",
                "col_ver_lista", "col_filtrar_tabs", "col_buscar", "col_paginar",
                "col_ver_detalle_panel",
                "cred_menu_ver",
                "cred_ver_lista", "cred_filtrar_tabs", "cred_buscar", "cred_paginar",
                "cred_ver_detalle", "cred_revelar_password",
                "cred_crear", "cred_editar", "cred_eliminar",
                "cred_generar_pass", "cred_ver_notas", "cred_editar_vencimiento",
                "dash_acceso", "dash_sidebar_colapsar",
                "dash_ver_kpis", "dash_ver_grafico", "dash_filtrar_tiempo",
                "dash_ver_categorias", "dash_ver_mapa",
                "login_tema",
            };
            var dict = new Dictionary<string, bool>();
            foreach (var id in ids) dict[id] = true;
            return dict;
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

        // ── Catálogo de módulos ───────────────────────────────────────
        private readonly List<ModuloPermisos> _modulos = ConstruirModulos();

        // ─────────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────
        private bool _inicializado = false;

        public PermisosPanel()
        {
            InitializeComponent();
        }

        private void PermisosPanel_Loaded(object sender, RoutedEventArgs e)
        {
            _inicializado = true;
            Abrir();
        }

        // ─────────────────────────────────────────────────────────────
        // APERTURA DEL PANEL
        // ─────────────────────────────────────────────────────────────
        public void Abrir()
        {
            if (!_inicializado) return;

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
            if (!_inicializado || RbOperador == null) return;

            _rolActual = RbOperador.IsChecked == true ? "Operador" : "Empleado";
            CargarCopiaTrabajo();
            RenderizarGrupos();
            ActualizarContadores();
        }

        private void TabModulo_Checked(object sender, RoutedEventArgs e)
        {
            if (!_inicializado) return;

            if (sender is RadioButton rb)
            {
                _moduloIndex = rb.Name switch
                {
                    "TabActivos" => 0,
                    "TabColaboradores" => 1,
                    "TabAsignaciones" => 2,
                    "TabContrasenas" => 3,
                    "TabAuditoria" => 5, // Asegúrate de que el índice coincida con ConstruirModulos
                    "TabCategorias" => 4,
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
            if (!_inicializado || PnlGrupos == null) return;

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

            if (_grupoActual == null && _moduloActual.Grupos.Count > 0)
            {
                _grupoActual = _moduloActual.Grupos[0];
                RenderizarGrupos();
                RenderizarPermisos();
            }
        }

        // RENDERIZAR PERMISOS (panel derecho)
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

                fila.MouseLeftButtonDown += (s, e) =>
                {
                    EstablecerPermiso(p.Id, !ObtenerPermiso(p.Id));
                    RenderizarPermisos();
                    RenderizarGrupos();
                };

                PnlPermisos.Children.Add(fila);
            }
        }

        // ACCIONES MASIVAS
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

        // GUARDAR / CERRAR
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PermisosService.CargarDesdeDict(_rolActual, _copiaTrabajo);
                _copiaTrabajoCopiaOriginal = new Dictionary<string, bool>(_copiaTrabajo);

                NotificacionService.Exito(
                    $"Configuración de permisos guardada para el rol '{_rolActual}'.\n\n" +
                    "Los cambios se aplicarán en el próximo inicio de sesión de los usuarios afectados.",
                    "Permisos actualizados");

                this.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"No se pudieron guardar los permisos en la base de datos:\n{ex.Message}");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            _copiaTrabajo = new Dictionary<string, bool>(_copiaTrabajoCopiaOriginal);
            this.Visibility = Visibility.Collapsed;
        }

        private void Overlay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.Source is System.Windows.Shapes.Rectangle)
                BtnCerrar_Click(sender, e);
        }

        // CONTADORES
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
        // CATÁLOGO DE MÓDULOS Y PERMISOS (COMPLETO)
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
                            new() { Id="act_menu_submenu", Descripcion="Expandir submenú de activos" },
                        }},
                        new() { Nombre = "Ver Inventario", Permisos = new() {
                            new() { Id="act_ver_lista",         Descripcion="Ver listado de activos en el DataGrid" },
                            new() { Id="act_filtrar_tabs",      Descripcion="Filtrar por estado: Todos / Asignados / En Bodega / Mantenimiento" },
                            new() { Id="act_buscar",            Descripcion="Buscar activos en tiempo real" },
                            new() { Id="act_paginar",           Descripcion="Navegar entre páginas del inventario" },
                            new() { Id="act_descargar_factura", Descripcion="Descargar factura de compra en PDF" },
                            new() { Id="act_exportar_excel",    Descripcion="Exportar inventario filtrado a Excel" },
                        }},
                        new() { Nombre = "Crear Activo", Permisos = new() {
                            new() { Id="act_crear_acceso",  Descripcion="Acceder al formulario 'Nuevo Activo'" },
                            new() { Id="act_crear_paso1",   Descripcion="Completar Paso 1 (Datos Generales)" },
                            new() { Id="act_crear_paso2",   Descripcion="Completar Paso 2 (Especificaciones)" },
                            new() { Id="act_subir_pdf",     Descripcion="Adjuntar factura en PDF" },
                            new() { Id="act_guardar",       Descripcion="Guardar nuevo activo" },
                            new() { Id="act_cancelar",      Descripcion="Cancelar registro en curso" },
                        }},
                        new() { Nombre = "Edición y Baja", Permisos = new() {
                            new() { Id="act_editar_acceso",  Descripcion="Acceder al formulario de edición" },
                            new() { Id="act_editar_guardar", Descripcion="Guardar cambios al activo" },
                            new() { Id="act_baja_logica",    Descripcion="Ejecutar baja lógica del activo" },
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
                        new() { Nombre = "Visualización", Permisos = new() {
                            new() { Id="col_ver_lista",         Descripcion="Ver listado de colaboradores" },
                            new() { Id="col_filtrar_tabs",      Descripcion="Filtrar por perfil" },
                            new() { Id="col_buscar",            Descripcion="Buscar colaboradores en tiempo real" },
                            new() { Id="col_paginar",           Descripcion="Navegar entre páginas" },
                            new() { Id="col_exportar_excel",    Descripcion="Exportar colaboradores a Excel" },
                            new() { Id="col_ver_detalle_panel", Descripcion="Ver panel lateral de detalles" },
                        }},
                        new() { Nombre = "Gestión (CRUD)", Permisos = new() {
                            new() { Id="col_crear",           Descripcion="Crear nuevo colaborador" },
                            new() { Id="col_editar",          Descripcion="Editar colaborador seleccionado" },
                            new() { Id="col_subir_foto",      Descripcion="Seleccionar y cargar foto de perfil" },
                            new() { Id="col_cambiar_password", Descripcion="Cambiar contraseña de acceso" },
                            new() { Id="col_cambiar_perfil",  Descripcion="Cambiar perfil / rol del colaborador" },
                        }},
                    }
                },

                // ── ASIGNACIONES ─────────────────────────────────────────
                new() {
                    Id = "asignaciones", Nombre = "Asignación de Equipos",
                    Grupos = new() {
                        new() { Nombre = "Menú lateral", Permisos = new() {
                            new() { Id="asi_menu_ver", Descripcion="Ver ítem 'Asignaciones' en el sidebar" },
                        }},
                        new() { Nombre = "Visualización", Permisos = new() {
                            new() { Id="asi_ver_lista",   Descripcion="Ver listado general de asignaciones" },
                            new() { Id="asi_buscar",      Descripcion="Buscar asignaciones" },
                            new() { Id="asi_paginar",     Descripcion="Navegar entre páginas de asignaciones" },
                            new() { Id="asi_ver_detalle", Descripcion="Ver panel de detalles de asignación y acta" },
                        }},
                        new() { Nombre = "Gestión de Asignación", Permisos = new() {
                            new() { Id="asi_crear",             Descripcion="Acceder a nueva asignación" },
                            new() { Id="asi_editar",            Descripcion="Editar asignación existente" },
                            new() { Id="asi_selec_activo",      Descripcion="Seleccionar activo a asignar" },
                            new() { Id="asi_selec_colaborador", Descripcion="Seleccionar colaborador destino" },
                            new() { Id="asi_guardar",           Descripcion="Ejecutar y guardar asignación" },
                        }},
                    }
                },

                // ── CREDENCIALES Y CONTRASEÑAS ───────────────────────────
                new() {
                    Id = "credenciales", Nombre = "Bóveda de Credenciales",
                    Grupos = new() {
                        new() { Nombre = "Menú lateral", Permisos = new() {
                            new() { Id="cred_menu_ver", Descripcion="Ver ítem 'Credenciales' en el sidebar" },
                        }},
                        new() { Nombre = "Visualización", Permisos = new() {
                            new() { Id="cred_ver_lista",          Descripcion="Ver bóveda de credenciales" },
                            new() { Id="cred_filtrar_tabs",       Descripcion="Filtrar por tipo (Servidor, Correo, etc.)" },
                            new() { Id="cred_buscar",             Descripcion="Buscar credenciales" },
                            new() { Id="cred_paginar",            Descripcion="Paginar listado de credenciales" },
                            new() { Id="cred_ver_detalle",        Descripcion="Ver detalles de la cuenta" },
                            new() { Id="cred_revelar_password",   Descripcion="Revelar contraseña oculta" },
                            new() { Id="cred_ver_notas",          Descripcion="Ver notas seguras adjuntas" },
                        }},
                        new() { Nombre = "Gestión", Permisos = new() {
                            new() { Id="cred_crear",              Descripcion="Añadir nueva credencial" },
                            new() { Id="cred_editar",             Descripcion="Editar credencial existente" },
                            new() { Id="cred_eliminar",           Descripcion="Eliminar credencial de la bóveda" },
                            new() { Id="cred_generar_pass",       Descripcion="Usar generador de contraseñas seguras" },
                            new() { Id="cred_editar_vencimiento", Descripcion="Configurar fecha de expiración" },
                        }},
                    }
                },

                // ── CATEGORÍAS ───────────────────────────────────────────
                new() {
                    Id = "categorias", Nombre = "Gestión de Categorías",
                    Grupos = new() {
                        new() { Nombre = "Acceso y Gestión", Permisos = new() {
                            new() { Id="cat_menu_ver",  Descripcion="Ver ítem 'Categorías' en el sidebar" },
                            new() { Id="cat_ver_lista", Descripcion="Ver listado de categorías" },
                            new() { Id="cat_crear",     Descripcion="Crear nuevas categorías" },
                            new() { Id="cat_editar",    Descripcion="Editar categorías existentes" },
                        }},
                    }
                },

                // ── AUDITORÍA Y LOGS ─────────────────────────────────────
                new() {
                    Id = "auditoria", Nombre = "Auditoría del Sistema",
                    Grupos = new() {
                        new() { Nombre = "Menú y Acceso", Permisos = new() {
                            new() { Id="aud_menu_ver",      Descripcion="Ver ítem 'Auditoría' en el sidebar" },
                            new() { Id="aud_acceso_modulo", Descripcion="Ingresar al visor de logs del sistema" },
                        }},
                        new() { Nombre = "Consultas", Permisos = new() {
                            new() { Id="aud_consultar",       Descripcion="Realizar consultas y búsquedas en logs" },
                            new() { Id="aud_ver_diff",        Descripcion="Ver diferencias (Diff) de cambios en JSON" },
                            new() { Id="aud_exportar_excel",  Descripcion="Exportar registro de auditoría a Excel" },
                            new() { Id="aud_limpiar_filtros", Descripcion="Restablecer filtros de búsqueda" },
                            new() { Id="aud_paginar",         Descripcion="Navegar entre páginas del log" },
                        }},
                    }
                },

                // ── DASHBOARD Y GENERAL ──────────────────────────────────
                new() {
                    Id = "dashboard", Nombre = "Dashboard y Configuración",
                    Grupos = new() {
                        new() { Nombre = "Dashboard", Permisos = new() {
                            new() { Id="dash_acceso",             Descripcion="Ver pantalla principal de Dashboard" },
                            new() { Id="dash_ver_kpis",           Descripcion="Ver tarjetas de KPIs e indicadores" },
                            new() { Id="dash_ver_grafico",        Descripcion="Ver gráficos estadísticos" },
                            new() { Id="dash_filtrar_tiempo",     Descripcion="Cambiar filtros de tiempo (Mes/Año)" },
                            new() { Id="dash_ver_categorias",     Descripcion="Ver desglose por categorías" },
                            new() { Id="dash_ver_mapa",           Descripcion="Ver mapa o distribución de activos" },
                        }},
                        new() { Nombre = "Interfaz y Sistema", Permisos = new() {
                            new() { Id="dash_sidebar_colapsar",   Descripcion="Colapsar o expandir el menú lateral" },
                            new() { Id="dash_selector_workspace", Descripcion="Cambiar espacio de trabajo / sucursal" },
                            new() { Id="login_tema",              Descripcion="Cambiar tema visual (Claro/Oscuro)" },
                        }},
                    }
                }
            };
        }
    }
}