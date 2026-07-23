using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // ← agregar esta

namespace Presentación
{
    public static class ThemeColors
    {
        public static SolidColorBrush B(string hex) =>
            (SolidColorBrush)new BrushConverter().ConvertFromString(hex);

        // ── OSCURO ──
        public static SolidColorBrush DarkBg => B("Transparent");
        public static SolidColorBrush DarkPanel => B("#09274c");
        public static SolidColorBrush DarkPanelBorder => B("#0d3a5c");
        public static SolidColorBrush DarkInput => B("#0d3a5c");
        public static SolidColorBrush DarkInputBorder => B("#1a4a7a");
        public static SolidColorBrush DarkTextPrimary => Brushes.White;
        public static SolidColorBrush DarkTextSecond => B("#A0C4E0");
        public static SolidColorBrush DarkRowHover => B("#0D0D2D");
        public static SolidColorBrush DarkRowSelected => B("#1a4a7a");
        public static SolidColorBrush DarkAccent => B("#2F80ED");

        // ── CLARO ──
        public static SolidColorBrush LightBg => B("#F4F6F9");
        public static SolidColorBrush LightPanel => B("#EDF2FF");
        public static SolidColorBrush LightPanelBorder => B("#C3D3F0");
        public static SolidColorBrush LightInput => B("#E8F0FF");
        public static SolidColorBrush LightInputBorder => B("#BFCFE8");
        public static SolidColorBrush LightTextPrimary => B("#1E3A5F");
        public static SolidColorBrush LightTextSecond => B("#4A6080");
        public static SolidColorBrush LightRowHover => B("#D6E4FF");
        public static SolidColorBrush LightRowSelected => B("#BFCFE8");
        public static SolidColorBrush LightAccent => B("#2F80ED");

        // Helpers directos (evitan repetir el ternario en cada control)
        public static SolidColorBrush Panel(bool claro) => claro ? LightPanel : DarkPanel;
        public static SolidColorBrush PanelBorder(bool claro) => claro ? LightPanelBorder : DarkPanelBorder;
        public static SolidColorBrush TextPrimary(bool claro) => claro ? LightTextPrimary : DarkTextPrimary;
        public static SolidColorBrush TextSecond(bool claro) => claro ? LightTextSecond : DarkTextSecond;
        public static SolidColorBrush Input(bool claro) => claro ? LightInput : DarkInput;
        public static SolidColorBrush InputBorder(bool claro) => claro ? LightInputBorder : DarkInputBorder;
        public static SolidColorBrush RowHover(bool claro) => claro ? LightRowHover : DarkRowHover;
        public static SolidColorBrush RowSelected(bool claro) => claro ? LightRowSelected : DarkRowSelected;

        public static Style GridRowStyle(bool claro)
        {
            var s = new Style(typeof(System.Windows.Controls.DataGridRow));
            s.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.BackgroundProperty,
                claro ? Panel(true) : Brushes.Transparent));
            s.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.ForegroundProperty, TextPrimary(claro)));
            s.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.BorderBrushProperty, PanelBorder(claro)));
            s.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));

            var hover = new Trigger { Property = System.Windows.Controls.DataGridRow.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.BackgroundProperty, RowHover(claro)));
            s.Triggers.Add(hover);

            var sel = new Trigger { Property = System.Windows.Controls.DataGridRow.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.BackgroundProperty, RowSelected(claro)));
            sel.Setters.Add(new Setter(System.Windows.Controls.DataGridRow.ForegroundProperty, TextPrimary(claro)));
            s.Triggers.Add(sel);
            return s;
        }

        public static Style GridHeaderStyle(bool claro)
        {
            var h = new Style(typeof(DataGridColumnHeader));
            h.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty,
                claro ? Panel(true) : Brushes.Transparent));
            h.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, TextSecond(claro)));
            h.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
            h.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, PanelBorder(claro)));
            h.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            h.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            return h;
        }
    }
}