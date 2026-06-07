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
using System.Windows.Threading; // Necesario para el DispatcherTimer de WPF
using AccesoDatos;              // Carpetas importadas de tu otro proyecto
using Dominio;                  // Carpetas importadas de tu otro proyecto
using Microsoft.Data.SqlClient;

namespace Presentación
{
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        // Lógica del dominio traída de tu proyecto anterior
        private readonly UsuarioDominio _dominio = new UsuarioDominio();
        private DispatcherTimer timer1; // Reemplazo de WPF para el Timer de WinForms

        private bool isPasswordVisible = false;
        private bool _isDarkMode = true;

        public Login()
        {
            InitializeComponent();
            this.Loaded += Login_Loaded; // Evento nativo de WPF al cargar la ventana
        }

        private void Login_Loaded(object sender, RoutedEventArgs e)
        {
            // Asignación de focos y eventos de teclas adaptados a WPF
            Username.Focus();
            Username.KeyDown += Username_KeyDown;
            TxtPassword.KeyDown += Password_KeyDown;
            TxtPasswordRevealed.KeyDown += Password_KeyDown;
            LoginServices.Click += LoginServices_Click;

            // Configuración del Fade In inicial (Transparencia)
            this.Opacity = 0.0;

            timer1 = new DispatcherTimer();
            timer1.Interval = TimeSpan.FromMilliseconds(30);
            timer1.Tick += Timer1_Tick;

            // NOTA: Si en tu XAML tienes un ProgressBar llamado "progressBar1", 
            // puedes descomentar las líneas de abajo. Si no, déjalas comentadas para evitar errores.
            /*
            progressBar1.Value = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            */

            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (this.Opacity < 1)
                this.Opacity += 0.2;

            /* Lógica de barra de progreso (Descomentar si usas un ProgressBar en el XAML)
            if (progressBar1.Value < 100)
                progressBar1.Value++;

            if (progressBar1.Value >= 100)
            {
                timer1.Stop();
                progressBar1.Visibility = Visibility.Collapsed;
            }
            */

            if (this.Opacity >= 1)
            {
                timer1.Stop();
            }
        }

        // ================= LÓGICA DE CONEXIÓN Y AUTENTICACIÓN (FUSIONADA) =================
        private void LoginServices_Click(object sender, RoutedEventArgs e)
        {
            string correo = Username.Text.Trim().ToLower();

            // Detecta la contraseña tanto si está oculta como si está revelada por el Ojo
            string passwordHash = isPasswordVisible ? TxtPasswordRevealed.Text.Trim() : TxtPassword.Password.Trim();

            var resultado = _dominio.Login(correo, passwordHash);

            if (resultado.Exitoso)
            {
                ConexionSql.VariablesGlobales.xEstIni = 1;

                // Instancia tu ventana de Dashboard enviando los parámetros de la BD
                Dashboard FrmMenu = new Dashboard(
                    resultado.Nombres.Split(' ')[0],
                    resultado.Apellidos.Split(' ')[0],
                    resultado.Rol,
                    resultado.Cargo,
                    resultado.Foto
                );

                this.Hide();

                // En WPF, ShowDialog detiene el hilo actual hasta que se cierra la ventana secundaria
                FrmMenu.ShowDialog();

                // Al regresar del Dashboard, limpia los controles y se vuelve a mostrar
                this.Show();
                Username.Clear();
                TxtPassword.Clear();
                TxtPasswordRevealed.Clear();
                Username.Focus();
            }
            else
            {
                // Alerta adaptada a los cuadros de diálogo nativos de WPF
                MessageBox.Show(resultado.Mensaje, "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (resultado.Bloqueado)
                {
                    Username.IsEnabled = false;
                    TxtPassword.IsEnabled = false;
                    TxtPasswordRevealed.IsEnabled = false;
                }
                else
                {
                    TxtPassword.Clear();
                    TxtPasswordRevealed.Clear();
                    if (isPasswordVisible) TxtPasswordRevealed.Focus(); else TxtPassword.Focus();
                }
            }
        }

        private void Username_KeyDown(object sender, KeyEventArgs e)
        {
            // Evento de tecla Enter adaptado a las enumeraciones de WPF (Key.Enter)
            if (e.Key == Key.Enter)
            {
                if (isPasswordVisible) TxtPasswordRevealed.Focus(); else TxtPassword.Focus();
            }
        }

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginServices_Click(sender, e);
            }
        }


        // ================= TU CÓDIGO ACTUAL DE WPF (PRESERVADO SIN ALTERACIONES) =================

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

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
            if (_isDarkMode)
            {
                // ☀️ CONFIGURAR PALETA MODO CLARO
                this.Resources["CardBgColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2FFFFFF"));
                this.Resources["InputBgColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F2F5"));
                this.Resources["InputFocusBgColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E6E9"));
                this.Resources["InputBorderColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCD1D9"));
                this.Resources["PrimaryTextColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                this.Resources["SecondaryTextColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#65676B"));
                this.Resources["TabTextSelectedColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#050505"));
                this.Resources["TabTextUnselectedColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#65676B"));
            }
            else
            {
                // 🌙 REVERTIR A PALETA MODO OSCURO
                this.Resources["CardBgColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F20A0E29"));
                this.Resources["InputBgColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12163B"));
                this.Resources["InputFocusBgColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161B40"));
                this.Resources["InputBorderColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2456"));
                this.Resources["PrimaryTextColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5A9CC"));
                this.Resources["SecondaryTextColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757B96"));
                this.Resources["TabTextSelectedColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                this.Resources["TabTextUnselectedColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757B96"));
            }

            _isDarkMode = !_isDarkMode;
        }
    }
}