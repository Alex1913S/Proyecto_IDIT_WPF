using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;

namespace Presentación.Controls
{
    public enum TipoNotificacion { Exito, Error, Advertencia, Info }

    public partial class NotificacionModal : UserControl
    {
        public event Action Cerrado;
        private bool _cerrandoPorClickFuera;

        public NotificacionModal()
        {
            InitializeComponent();
        }

        public void Configurar(TipoNotificacion tipo, string titulo, string mensaje, string textoBoton = null)
        {
            TxtTitulo.Text = titulo;
            TxtMensaje.Text = mensaje;

            switch (tipo)
            {
                case TipoNotificacion.Exito:
                    HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF6D"));
                    IconPath.Data = Geometry.Parse("M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z");
                    BtnAccion.Content = textoBoton ?? "Entendido";
                    BtnAccion.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    break;

                case TipoNotificacion.Error:
                    HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6483F"));
                    IconPath.Data = Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z");
                    BtnAccion.Content = textoBoton ?? "Reintentar";
                    break;

                case TipoNotificacion.Advertencia:
                    HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59924"));
                    IconPath.Data = Geometry.Parse("M13,14H11V9H13M13,18H11V16H13M1,21H23L12,2L1,21Z");
                    BtnAccion.Content = textoBoton ?? "Entendido";
                    break;

                case TipoNotificacion.Info:
                    HeaderBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                    IconPath.Data = Geometry.Parse("M11,9H13V7H11M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,17H13V11H11V17Z");
                    BtnAccion.Content = textoBoton ?? "Entendido";
                    break;
            }
        }

        public void MostrarConAnimacion()
        {
            CardBorder.RenderTransform = new ScaleTransform(0.85, 0.85);
            CardBorder.RenderTransformOrigin = new Point(0.5, 0.5);
            CardBorder.Opacity = 0;
            RootOverlay.Opacity = 0;

            var fadeOverlay = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            RootOverlay.BeginAnimation(OpacityProperty, fadeOverlay);

            var fadeCard = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            var scaleCard = new DoubleAnimation(0.85, 1, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
            };
            CardBorder.BeginAnimation(OpacityProperty, fadeCard);
            ((ScaleTransform)CardBorder.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleCard);
            ((ScaleTransform)CardBorder.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleCard);
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _cerrandoPorClickFuera = true;
            CerrarConAnimacion();
        }

        private void Card_MouseDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => CerrarConAnimacion();

        private void CerrarConAnimacion()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) => Cerrado?.Invoke();
            RootOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}