using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Presentación.Controls
{
    public static class NotificacionService
    {
        public static void Mostrar(TipoNotificacion tipo, string titulo, string mensaje, string textoBoton = null)
        {
            Window ventanaActiva = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

            if (ventanaActiva == null) return;

            // Buscar (o crear) un Grid raíz sobre el que superponer el modal
            if (ventanaActiva.Content is not Grid rootGrid)
            {
                var contenidoOriginal = ventanaActiva.Content as UIElement;
                rootGrid = new Grid();
                ventanaActiva.Content = null;
                if (contenidoOriginal != null) rootGrid.Children.Add(contenidoOriginal);
                ventanaActiva.Content = rootGrid;
            }

            var modal = new NotificacionModal();
            modal.Configurar(tipo, titulo, mensaje, textoBoton);
            Panel.SetZIndex(modal, 9999);
            rootGrid.Children.Add(modal);

            modal.Cerrado += () => rootGrid.Children.Remove(modal);
            modal.MostrarConAnimacion();
        }

        public static Task<bool> Confirmar(string mensaje, string titulo = "Confirmación", string textoSi = "Sí", string textoNo = "No")
        {
            var tcs = new TaskCompletionSource<bool>();

            Window ventanaActiva = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

            if (ventanaActiva == null)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            if (ventanaActiva.Content is not Grid rootGrid)
            {
                var contenidoOriginal = ventanaActiva.Content as UIElement;
                rootGrid = new Grid();
                ventanaActiva.Content = null;
                if (contenidoOriginal != null) rootGrid.Children.Add(contenidoOriginal);
                ventanaActiva.Content = rootGrid;
            }

            var modal = new NotificacionModal();
            modal.Configurar(TipoNotificacion.Confirmacion, titulo, mensaje, textoSi, textoNo);
            Panel.SetZIndex(modal, 9999);
            rootGrid.Children.Add(modal);

            modal.Cerrado += () =>
            {
                rootGrid.Children.Remove(modal);
                tcs.SetResult(modal.Resultado);
            };

            modal.MostrarConAnimacion();

            return tcs.Task;
        }

        // Atajos que reemplazan directamente MessageBox.Show
        public static void Exito(string mensaje, string titulo = "¡Éxito!")
            => Mostrar(TipoNotificacion.Exito, titulo, mensaje, "Got It!");

        public static void Error(string mensaje, string titulo = "¡Whoops!")
            => Mostrar(TipoNotificacion.Error, titulo, mensaje, "Try Again");

        public static void Advertencia(string mensaje, string titulo = "Atención")
            => Mostrar(TipoNotificacion.Advertencia, titulo, mensaje);

        public static void Info(string mensaje, string titulo = "Información")
            => Mostrar(TipoNotificacion.Info, titulo, mensaje);
    }
}