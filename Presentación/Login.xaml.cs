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
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        // 1. Este método soluciona el error de la línea 14
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // 2. Este método soluciona el error de la línea 32
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private bool isPasswordVisible = false;
        private bool isDarkMode = true;
        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!isPasswordVisible)
            {
                // Copiar el texto oculto al campo visible
                TxtPasswordRevealed.Text = TxtPassword.Password;

                // Alternar visibilidades visuales
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordRevealed.Visibility = Visibility.Visible;

                // Cambiar el vector al OJO TACHADO / CERRADO
                EyeIcon.Data = Geometry.Parse("M12,17A5,5 0 0,1 7,12C7,11.38 7,10.79 7.31,10.25L3.37,6.31C1.86,7.86 1,9.83 1,12C2.73,16.39 7,19.5 12,19.5C14.07,19.5 16,18.9 17.64,17.9L15.34,15.6C14.41,16.5 13.27,17 12,17M12,4.5C9.93,4.5 8,5.1 6.36,6.1L8.66,8.4C9.59,7.5 10.73,7 12,7A5,5 0 0,1 17,12C17,12.62 16.89,13.21 16.69,13.75L20.63,17.69C22.14,16.14 23,14.17 23,12C21.27,7.61 17,4.5 12,4.5M12,9A3,3 0 0,0 9,12C9,12.22 9,12.44 9.1,12.64L14.36,7.38C13.67,7.14 12.87,7 12,9M12,15C12.22,15 12.44,15 12.64,14.9L7.38,9.64C7.14,10.33 7,11.13 7,12A3,3 0 0,0 12,15Z");
                EyeIcon.Fill = System.Windows.Media.Brushes.DarkGray;

                isPasswordVisible = true;
                TxtPasswordRevealed.Focus();
            }
            else
            {
                // Regresar el texto del campo visible al campo oculto
                TxtPassword.Password = TxtPasswordRevealed.Text;

                // Alternar visibilidades visuales al revés
                TxtPasswordRevealed.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;

                // Cambiar el vector de vuelta al OJO ABIERTO original
                EyeIcon.Data = Geometry.Parse("M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9Z");
                EyeIcon.Fill = System.Windows.Media.Brushes.SlateGray;

                isPasswordVisible = false;
                TxtPassword.Focus();
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            // Localizar el objeto Path que está dentro de la plantilla del botón
            var themeIcon = (Path)BtnThemeToggle.Template.FindName("ThemeIcon", BtnThemeToggle);
            var bc = new System.Windows.Media.BrushConverter();

            if (isDarkMode)
            {
                // ================= CONMUTAR A MODO CLARO =================
                MainWindowBorder.Background = (SolidColorBrush)bc.ConvertFromString("#FFFFFF");
                RightImageBorder.Background = (SolidColorBrush)bc.ConvertFromString("#FFFFFF");

                // Cambiar el color de los textos para que tengan contraste en fondo blanco
                SolidColorBrush lightThemeText = (SolidColorBrush)bc.ConvertFromString("#22223B");
                LblTitle.Foreground = lightThemeText;
                LblUser.Foreground = lightThemeText;
                LblPassword.Foreground = lightThemeText;
                LblTerms.Foreground = lightThemeText;

                if (themeIcon != null)
                {
                    // Cambiar vector gráfico a un SOL
                    themeIcon.Data = Geometry.Parse("M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,3.5A1,1 0 0,1 13,4.5V5.5A1,1 0 0,1 11,5.5V4.5A1,1 0 0,1 12,3.5M12,18.5A1,1 0 0,1 13,19.5V20.5A1,1 0 0,1 11,20.5V19.5A1,1 0 0,1 12,18.5M20.5,12A1,1 0 0,1 19.5,13H18.5A1,1 0 0,1 18.5,11H19.5A1,1 0 0,1 20.5,12M5.5,12A1,1 0 0,1 4.5,13H3.5A1,1 0 0,1 3.5,11H4.5A1,1 0 0,1 5.5,12M6,6A1,1 0 0,1 7.41,6L8.12,6.71A1,1 0 0,1 6.71,8.12L6,7.41A1,1 0 0,1 6,6M18,18A1,1 0 0,1 16.59,18L15.88,17.29A1,1 0 0,1 17.29,15.88L18,16.59A1,1 0 0,1 18,18M18,6A1,1 0 0,1 18,7.41L17.29,8.12A1,1 0 0,1 15.88,6.71L16.59,6A1,1 0 0,1 18,6M6,18A1,1 0 0,1 6,16.59L6.71,15.88A1,1 0 0,1 8.12,17.29L7.41,18A1,1 0 0,1 6,18Z");
                    themeIcon.Fill = (SolidColorBrush)bc.ConvertFromString("#E89A24"); // Sol naranja/dorado
                }

                isDarkMode = false;
            }
            else
            {
                // ================= CONMUTAR A MODO OSCURO =================
                MainWindowBorder.Background = (SolidColorBrush)bc.ConvertFromString("#060621");
                RightImageBorder.Background = (SolidColorBrush)bc.ConvertFromString("#060621");

                // Devolver los textos a Blanco original
                LblTitle.Foreground = Brushes.White;
                LblUser.Foreground = Brushes.White;
                LblPassword.Foreground = Brushes.White;
                LblTerms.Foreground = Brushes.White;

                if (themeIcon != null)
                {
                    // Cambiar vector gráfico a una LUNA
                    themeIcon.Data = Geometry.Parse("M12,2A10,10 0 1,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z");
                    themeIcon.Fill = (SolidColorBrush)bc.ConvertFromString("#A0A0B8"); // Luna grisácea
                }

                isDarkMode = true;
            }
        }
    }
}
