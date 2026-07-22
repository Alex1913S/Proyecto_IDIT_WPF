using Dominio;
using Microsoft.Win32;
using Presentación.Controls;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Presentación.UserControls
{
    public partial class ActaEntrega : UserControl
    {
        private readonly ActaEntregaDominio _dominio = new();
        private ActaEntregaModel? _modeloActual;

        public ActaEntrega() => InitializeComponent();

        // ═════════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ═════════════════════════════════════════════════════════════════
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DpFecha.SelectedDate = DateTime.Today;
            CargarColaboradores();
        }

        private void CargarColaboradores()
        {
            try
            {
                var dt = _dominio.ListarColaboradores();
                CbColaborador.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"Error al cargar colaboradores:\n{ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // SELECCIÓN DE COLABORADOR
        // ═════════════════════════════════════════════════════════════════
        private void CbColaborador_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbColaborador.SelectedValue is not int id) { LimpiarPrevia(); return; }

            try
            {
                _modeloActual = _dominio.PrepararActa(id);
                DgActivos.ItemsSource = _modeloActual.Activos;
                TxtNombrePreview.Text = _modeloActual.NombreCompleto;
                TxtCedulaPreview.Text = _modeloActual.DocumentoIdentidad;
                TxtCargoPreview.Text = _modeloActual.Cargo;

                if (_modeloActual.Activos.Count == 0)
                    NotificacionService.Info("Este colaborador no tiene equipos asignados actualmente.", "Sin equipos");
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"Error al cargar los datos:\n{ex.Message}");
            }
        }

        private void LimpiarPrevia()
        {
            _modeloActual = null;
            DgActivos.ItemsSource = null;
            TxtNombrePreview.Text = "—";
            TxtCedulaPreview.Text = "—";
            TxtCargoPreview.Text = "—";
        }

        // ═════════════════════════════════════════════════════════════════
        // GENERAR ACTA INDIVIDUAL
        // ═════════════════════════════════════════════════════════════════
        private void BtnGenerarActa_Click(object sender, RoutedEventArgs e)
        {
            if (_modeloActual == null)
            {
                NotificacionService.Advertencia("Selecciona primero un colaborador.");
                return;
            }

            if (!_modeloActual.Activos.Any(a => a.Incluido))
            {
                NotificacionService.Advertencia("Marca al menos un equipo en la columna \"✓ Incluir\".");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtLugar.Text))
            {
                NotificacionService.Advertencia("El campo Lugar es obligatorio.");
                TxtLugar.Focus();
                return;
            }

            AplicarCamposAlModelo(_modeloActual);

            var dlg = new SaveFileDialog
            {
                Title = "Guardar Acta de Entrega",
                FileName = NombreArchivo(_modeloActual),
                DefaultExt = ".pdf",
                Filter = "Archivos PDF (*.pdf)|*.pdf"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                _dominio.GenerarPdf(_modeloActual, dlg.FileName);
                NotificacionService.Exito($"Acta generada correctamente:\n{dlg.FileName}", "Éxito");
            }
            catch (Exception ex)
            {
                NotificacionService.Error($"Error al generar el PDF:\n{ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // EXPORTAR TODOS — selección de carpeta con truco SaveFileDialog
        // ═════════════════════════════════════════════════════════════════
        private async void BtnExportarTodos_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Navega hasta la carpeta donde guardar las actas y pulsa Guardar",
                FileName = "Selecciona esta carpeta",
                Filter = "Carpeta|*.ningunoEsValido",
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false,
                OverwritePrompt = false,
            };

            if (dlg.ShowDialog() != true) return;

            // Extraemos solo el directorio
            string raiz = Path.GetDirectoryName(dlg.FileName)!;

            // ── Confirmación ──────────────────────────────────────────────
            var dt = _dominio.ListarColaboradores();
            int total = dt.Rows.Count;

            if (total == 0)
            {
                NotificacionService.Info("No hay colaboradores activos para exportar.", "Sin datos");
                return;
            }

            var confirm = MessageBox.Show(
                $"Se generarán actas para {total} colaboradores.\n" +
                $"Carpeta raíz seleccionada:\n  {raiz}\n\n" +
                "Cada colaborador tendrá su propia subcarpeta.\n¿Continuar?",
                "Exportar todas las actas",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            // ── Bloquear UI y mostrar progreso ────────────────────────────
            ProgressExport.Visibility = Visibility.Visible;
            TxtProgreso.Visibility = Visibility.Visible;
            ProgressExport.Maximum = total;
            ProgressExport.Value = 0;
            BtnExportarTodos.IsEnabled = false;
            BtnGenerarActa.IsEnabled = false;

            // Capturar valores del formulario antes de entrar al hilo
            string lugar = TxtLugar.Text.Trim();
            string coordinador = TxtCoordinador.Text.Trim();
            string liderTI = TxtLiderTI.Text.Trim();
            DateTime fecha = DpFecha.SelectedDate ?? DateTime.Today;

            int exitosos = 0, sinEquipos = 0, fallidos = 0;
            string errores = "";

            // ── Procesamiento asíncrono ───────────────────────────────────
            await Task.Run(() =>
            {
                foreach (DataRow fila in dt.Rows)
                {
                    int colaboradorId = Convert.ToInt32(fila["ColaboradorID"]);
                    string nombre = fila["NombreCompleto"]?.ToString()
                                          ?? $"Colaborador_{colaboradorId}";
                    try
                    {
                        var modelo = _dominio.PrepararActa(colaboradorId);

                        if (modelo.Activos.Count == 0)
                        {
                            sinEquipos++;
                            Dispatcher.Invoke(() =>
                            {
                                ProgressExport.Value++;
                                TxtProgreso.Text = $"Sin equipos: {nombre}";
                            });
                            continue;
                        }

                        modelo.Lugar = lugar;
                        modelo.Fecha = fecha;
                        modelo.NombreCoordinador = coordinador;
                        modelo.NombreLiderTI = liderTI;
                        modelo.Observaciones = "";

                        // Subcarpeta: raiz\Nombre_del_Colaborador\
                        string subcarpeta = Path.Combine(raiz, SanitizarNombre(nombre));
                        Directory.CreateDirectory(subcarpeta);

                        string rutaPdf = Path.Combine(subcarpeta,
                            $"Acta_Entrega_{modelo.DocumentoIdentidad}_{fecha:yyyyMMdd}.pdf");

                        _dominio.GenerarPdf(modelo, rutaPdf);
                        exitosos++;
                    }
                    catch (Exception ex)
                    {
                        fallidos++;
                        errores += $"\n• {nombre}: {ex.Message}";
                    }

                    Dispatcher.Invoke(() =>
                    {
                        ProgressExport.Value++;
                        TxtProgreso.Text =
                            $"Procesando {(int)ProgressExport.Value} de {total} — {nombre}";
                    });
                }
            });

            // ── Restaurar UI ──────────────────────────────────────────────
            ProgressExport.Visibility = Visibility.Collapsed;
            TxtProgreso.Visibility = Visibility.Collapsed;
            BtnExportarTodos.IsEnabled = true;
            BtnGenerarActa.IsEnabled = true;

            // ── Resumen ───────────────────────────────────────────────────
            string resumen =
                $"Exportación completada.\n\n" +
                $"✅  Actas generadas:          {exitosos}\n" +
                $"⏭  Sin equipos asignados:    {sinEquipos}\n" +
                (fallidos > 0 ? $"❌  Con errores:              {fallidos}\n{errores}\n\n" : "") +
                $"\nCarpeta raíz:\n{raiz}";

            if (fallidos > 0)
                NotificacionService.Advertencia(resumen);
            else
                NotificacionService.Info(resumen, "Exportación finalizada");

            // Abrir el explorador en la carpeta raíz
            if (exitosos > 0)
                System.Diagnostics.Process.Start("explorer.exe", raiz);
        }

        // ═════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════
        private void AplicarCamposAlModelo(ActaEntregaModel m)
        {
            m.Lugar = TxtLugar.Text.Trim();
            m.Fecha = DpFecha.SelectedDate ?? DateTime.Today;
            m.NombreCoordinador = TxtCoordinador.Text.Trim();
            m.NombreLiderTI = TxtLiderTI.Text.Trim();
            m.Observaciones = TxtObservaciones.Text.Trim();
            foreach (var item in m.Activos)
                item.Estado = Math.Clamp(item.Estado, 1, 10);
        }

        private static string NombreArchivo(ActaEntregaModel m)
            => $"Acta_Entrega_{m.DocumentoIdentidad}_{DateTime.Now:yyyyMMdd}.pdf";

        private static string SanitizarNombre(string nombre)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');
            return nombre.Trim();
        }
    }
}