using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Dominio
{
    public class WindowsFontResolver : IFontResolver
    {
        public static readonly WindowsFontResolver Instance = new();
        public string DefaultFontName => "Arial";

        private static readonly Dictionary<string, string> _map =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Arial",    "arial.ttf"   },
                { "Arial#b",  "arialbd.ttf" },
                { "Arial#i",  "ariali.ttf"  },
                { "Arial#bi", "arialbi.ttf" },
            };

        public byte[] GetFont(string faceName)
        {
            if (!_map.TryGetValue(faceName, out string? file)) file = "arial.ttf";
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts), file);
            if (!File.Exists(path)) path = Path.Combine(@"C:\Windows\Fonts", file);
            return File.ReadAllBytes(path);
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string key = familyName;
            if (isBold && isItalic) key += "#bi";
            else if (isBold) key += "#b";
            else if (isItalic) key += "#i";
            return new FontResolverInfo(key);
        }
    }

    public static class ActaEntregaPdfService
    {
        // ── Paleta de marca Maxfin ────────────────────────────────────
        private static readonly XColor Azul = XColor.FromArgb(43, 108, 176);
        private static readonly XColor AzulOscuro = XColor.FromArgb(20, 60, 110);
        private static readonly XColor Naranja = XColor.FromArgb(245, 153, 36);
        private static readonly XColor NaranjaClaro = XColor.FromArgb(255, 200, 100);
        private static readonly XColor Blanco = XColor.FromArgb(255, 255, 255);
        private static readonly XColor GrisFondo = XColor.FromArgb(245, 247, 250);
        private static readonly XColor GrisBorde = XColor.FromArgb(210, 218, 230);
        private static readonly XColor GrisTexto = XColor.FromArgb(80, 95, 115);
        private static readonly XColor GrisClaro = XColor.FromArgb(245, 248, 252);
        private static readonly XColor AzulFilaAlt = XColor.FromArgb(234, 241, 251);

        private static bool _fontReady = false;
        private static void EnsureFonts()
        {
            if (_fontReady) return;
            GlobalFontSettings.FontResolver = WindowsFontResolver.Instance;
            _fontReady = true;
        }

        // ─────────────────────────────────────────────────────────────
        // ENTRY POINT PÚBLICO
        // ─────────────────────────────────────────────────────────────
        public static void Generar(ActaEntregaModel m, string rutaDestino)
        {
            EnsureFonts();
            using var doc = new PdfDocument();
            doc.Info.Title = "Acta de Entrega de Equipos";
            doc.Info.Author = "SGSI – Maxfin Financiera";
            doc.Info.Subject = m.NombreCompleto;

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            double ml = 42, mr = 42;
            double pw = page.Width;
            double ph = page.Height;
            double cw = pw - ml - mr;
            double y = 0;

            // ── 1. Barra superior azul oscuro ─────────────────────────
            gfx.DrawRectangle(new XSolidBrush(AzulOscuro),
                0, 0, pw, 54);

            // Título en la barra
            var fBarraTit = new XFont("Arial", 13, XFontStyleEx.Bold);
            var fBarraSub = new XFont("Arial", 8, XFontStyleEx.Regular);
            gfx.DrawString("ACTA DE ENTREGA DE EQUIPOS TECNOLÓGICOS",
                fBarraTit, new XSolidBrush(Blanco),
                new XRect(ml, 0, cw * 0.65, 54), XStringFormats.CenterLeft);
            gfx.DrawString("Maxfin Financiera SAS  ·  Gestión TI",
                fBarraSub, new XSolidBrush(NaranjaClaro),
                new XRect(ml, 28, cw * 0.65, 22), XStringFormats.CenterLeft);

            // Fecha y lugar alineados a la derecha en la barra
            var fBarraInfo = new XFont("Arial", 8, XFontStyleEx.Regular);
            double bx = ml + cw * 0.66;
            double bw = cw * 0.34;
            gfx.DrawString($"Fecha:  {m.Fecha:dd / MM / yyyy}",
                fBarraInfo, new XSolidBrush(Blanco),
                new XRect(bx, 8, bw, 18), XStringFormats.TopLeft);
            gfx.DrawString($"Lugar:  {(string.IsNullOrWhiteSpace(m.Lugar) ? "—" : m.Lugar)}",
                fBarraInfo, new XSolidBrush(Blanco),
                new XRect(bx, 26, bw, 18), XStringFormats.TopLeft);

            // Acento naranja inferior de la barra
            gfx.DrawRectangle(new XSolidBrush(Naranja), 0, 52, pw, 4);

            y = 70;

            // ── 2. Tarjeta de datos del colaborador ───────────────────
            DrawCard(gfx, ml, y, cw, 74, AzulOscuro, 8);

            // Icono circular azul
            double cx = ml + 22, cy = y + 37;
            gfx.DrawEllipse(new XSolidBrush(Azul), cx - 18, cy - 18, 36, 36);
            var fIcono = new XFont("Arial", 14, XFontStyleEx.Bold);
            string inicial = m.NombreCompleto.Length > 0
                ? m.NombreCompleto[0].ToString().ToUpper() : "C";
            gfx.DrawString(inicial, fIcono, new XSolidBrush(Blanco),
                new XRect(cx - 18, cy - 18, 36, 36), XStringFormats.Center);

            double tx = ml + 52;
            var fLabel = new XFont("Arial", 7, XFontStyleEx.Regular);
            var fValor = new XFont("Arial", 10, XFontStyleEx.Bold);
            var fSub = new XFont("Arial", 8, XFontStyleEx.Regular);

            gfx.DrawString(m.NombreCompleto,
                fValor, new XSolidBrush(Blanco), new XPoint(tx, y + 24));
            gfx.DrawString($"CC  {m.DocumentoIdentidad}   ·   {m.Cargo}",
                fSub, new XSolidBrush(NaranjaClaro), new XPoint(tx, y + 40));

            // Línea separadora interna de la tarjeta
            gfx.DrawLine(new XPen(XColor.FromArgb(60, 130, 200), 0.5),
                tx, y + 52, ml + cw - 12, y + 52);
            gfx.DrawString("Los datos anteriores corresponden al colaborador receptor de los equipos.",
                fLabel, new XSolidBrush(NaranjaClaro), new XPoint(tx, y + 59));

            y += 90;

            // ── 3. Párrafo legal ──────────────────────────────────────
            var fTexto = new XFont("Arial", 8.5, XFontStyleEx.Regular);
            string parrafo =
                $"Yo {m.NombreCompleto}, con cédula de ciudadanía N.° {m.DocumentoIdentidad}, " +
                $"desempeñando el cargo de {m.Cargo}, declaro haber recibido en perfecto estado los " +
                "siguientes implementos informáticos para realizar mis funciones laborales en el entorno " +
                "empresarial y acorde a mis necesidades en el puesto al cual pertenezco. Dichos equipos " +
                "estarán a mi cargo y me comprometo con la compañía, de acuerdo con las obligaciones del " +
                "trabajador, a conservar y restituir en buen estado, salvo el deterioro natural, los " +
                "instrumentos y herramientas que utilizo diariamente para el desempeño de mis funciones.\n" +
                "Así mismo, declaro conocer y aceptar la política disciplinaria de la compañía, la cual " +
                "se aplicaría en caso de presentarse pérdida total o parcial de dicho inventario.";

            double altoP = DrawWrapped(gfx, parrafo, fTexto,
                new XSolidBrush(GrisTexto), ml, y, cw, 13);
            y += altoP + 18;

            // ── 4. Tabla de equipos ───────────────────────────────────
            var fSecTit = new XFont("Arial", 9, XFontStyleEx.Bold);
            gfx.DrawString("IMPLEMENTOS TECNOLÓGICOS ENTREGADOS",
                fSecTit, new XSolidBrush(AzulOscuro), new XPoint(ml, y));
            // Subrayado acento naranja
            gfx.DrawLine(new XPen(Naranja, 2), ml, y + 12, ml + 230, y + 12);
            y += 20;

            // Encabezado de tabla
            double rowH = 22;
            double[] cols = { ml, ml + 120, ml + 300, ml + 420, ml + cw };
            string[] heads = { "Categoría", "Marca / Modelo", "N° Serie", "Estado" };

            DrawRoundRect(gfx, ml, y, cw, rowH, 5, new XSolidBrush(AzulOscuro));
            var fHead = new XFont("Arial", 8, XFontStyleEx.Bold);
            for (int i = 0; i < heads.Length; i++)
            {
                double cx2 = (cols[i] + cols[i + 1]) / 2;
                gfx.DrawString(heads[i], fHead, new XSolidBrush(Blanco),
                    new XRect(cols[i] + 4, y + 2, cols[i + 1] - cols[i] - 8, rowH - 4),
                    XStringFormats.Center);
            }
            y += rowH;

            var fCell = new XFont("Arial", 8, XFontStyleEx.Regular);
            var fCellB = new XFont("Arial", 8, XFontStyleEx.Bold);

            var incluidos = m.Activos.Where(a => a.Incluido).ToList();
            if (incluidos.Count == 0)
            {
                gfx.DrawRectangle(new XSolidBrush(GrisFondo), ml, y, cw, rowH);
                gfx.DrawString("(Sin equipos seleccionados para esta acta)",
                    fCell, new XSolidBrush(GrisTexto),
                    new XRect(ml + 6, y + 2, cw - 12, rowH - 4), XStringFormats.CenterLeft);
                y += rowH;
            }

            for (int idx = 0; idx < incluidos.Count; idx++)
            {
                if (y + rowH > ph - 170)
                {
                    // Acento naranja al pie de la página
                    gfx.DrawRectangle(new XSolidBrush(Naranja), 0, ph - 6, pw, 6);
                    page = doc.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 30;
                }

                var item = incluidos[idx];
                bool alt = idx % 2 == 0;
                var bgRow = new XSolidBrush(alt ? GrisClaro : AzulFilaAlt);
                gfx.DrawRectangle(bgRow, ml, y, cw, rowH);

                // Celda categoría — badge de color
                DrawBadge(gfx, item.Categoria, fCellB, cols[0] + 4, y + 4, cols[1] - cols[0] - 8);

                gfx.DrawString(Trunc(item.MarcaModelo, fCell, gfx, cols[2] - cols[1] - 8),
                    fCell, new XSolidBrush(GrisTexto),
                    new XRect(cols[1] + 4, y + 2, cols[2] - cols[1] - 8, rowH - 4),
                    XStringFormats.CenterLeft);

                gfx.DrawString(item.NumeroSerie,
                    fCell, new XSolidBrush(GrisTexto),
                    new XRect(cols[2] + 4, y + 2, cols[3] - cols[2] - 8, rowH - 4),
                    XStringFormats.CenterLeft);

                // Estado como badge numérico coloreado
                DrawEstadoBadge(gfx, item.Estado, fCellB, cols[3], y, cols[4] - cols[3]);

                // Línea divisoria entre filas
                gfx.DrawLine(new XPen(GrisBorde, 0.4), ml, y + rowH, ml + cw, y + rowH);
                y += rowH;
            }

            // Borde exterior de la tabla
            gfx.DrawRectangle(new XPen(GrisBorde, 0.8), ml, y - rowH * (incluidos.Count == 0 ? 1 : incluidos.Count) - rowH, cw,
                rowH * (incluidos.Count == 0 ? 1 : incluidos.Count) + rowH);

            y += 20;

            // ── 5. Observaciones ──────────────────────────────────────
            gfx.DrawString("OBSERVACIONES O ADICIONES",
                fSecTit, new XSolidBrush(AzulOscuro), new XPoint(ml, y));
            gfx.DrawLine(new XPen(Naranja, 2), ml, y + 12, ml + 180, y + 12);
            y += 22;

            double obsH = 58;
            DrawRoundRect(gfx, ml, y, cw, obsH, 6, new XSolidBrush(GrisFondo));
            gfx.DrawRectangle(new XPen(GrisBorde, 0.8), ml, y, cw, obsH);
            if (!string.IsNullOrWhiteSpace(m.Observaciones))
                DrawWrapped(gfx, m.Observaciones, fCell,
                    new XSolidBrush(GrisTexto), ml + 8, y + 8, cw - 16, 12);
            y += obsH + 22;

            // ── 6. Firmas ─────────────────────────────────────────────
            gfx.DrawString("FIRMAS Y FECHAS",
                fSecTit, new XSolidBrush(AzulOscuro), new XPoint(ml, y));
            gfx.DrawLine(new XPen(Naranja, 2), ml, y + 12, ml + 120, y + 12);
            y += 24;

            double fw = (cw - 20) / 3;
            double fy = y + 36;

            DrawFirmaBox(gfx, ml, y, fw, "Firma Colaborador", m.NombreCompleto, fCell, fCellB);
            DrawFirmaBox(gfx, ml + fw + 10, y, fw, "Firma Coordinador", m.NombreCoordinador, fCell, fCellB);
            DrawFirmaBox(gfx, ml + fw * 2 + 20, y, fw, "Líder TI / Quien entrega", m.NombreLiderTI, fCell, fCellB);

            // ── 7. Pie de página ──────────────────────────────────────
            gfx.DrawRectangle(new XSolidBrush(AzulOscuro), 0, ph - 26, pw, 26);
            var fFoot = new XFont("Arial", 7, XFontStyleEx.Regular);
            gfx.DrawString(
                $"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm}  ·  SGSI Maxfin Financiera SAS  ·  Confidencial",
                fFoot, new XSolidBrush(NaranjaClaro),
                new XRect(0, ph - 26, pw, 26), XStringFormats.Center);

            // Acento naranja encima del pie
            gfx.DrawRectangle(new XSolidBrush(Naranja), 0, ph - 28, pw, 3);

            doc.Save(rutaDestino);
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS DE DIBUJO
        // ─────────────────────────────────────────────────────────────

        private static double DrawWrapped(XGraphics gfx, string text, XFont font,
            XBrush brush, double x, double y, double maxW, double lineH)
        {
            double startY = y;
            foreach (string para in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(para)) { y += lineH * 0.5; continue; }
                string line = "";
                foreach (string word in para.Split(' '))
                {
                    string cand = line.Length == 0 ? word : line + " " + word;
                    if (gfx.MeasureString(cand, font).Width > maxW && line.Length > 0)
                    {
                        gfx.DrawString(line, font, brush, new XPoint(x, y));
                        y += lineH; line = word;
                    }
                    else line = cand;
                }
                if (line.Length > 0) { gfx.DrawString(line, font, brush, new XPoint(x, y)); y += lineH; }
            }
            return y - startY;
        }

        private static void DrawCard(XGraphics gfx, double x, double y,
            double w, double h, XColor color, double radius)
        {
            var brush = new XSolidBrush(color);
            DrawRoundRect(gfx, x, y, w, h, radius, brush);
        }

        private static void DrawRoundRect(XGraphics gfx, double x, double y,
            double w, double h, double r, XBrush brush)
        {
            // PdfSharp no tiene DrawRoundedRectangle nativo; simulamos con rect + círculos
            gfx.DrawRectangle(brush, x + r, y, w - 2 * r, h);
            gfx.DrawRectangle(brush, x, y + r, w, h - 2 * r);
            gfx.DrawEllipse(brush, x, y, 2 * r, 2 * r);
            gfx.DrawEllipse(brush, x + w - 2 * r, y, 2 * r, 2 * r);
            gfx.DrawEllipse(brush, x, y + h - 2 * r, 2 * r, 2 * r);
            gfx.DrawEllipse(brush, x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r);
        }

        private static void DrawBadge(XGraphics gfx, string text, XFont font,
            double x, double y, double maxW)
        {
            // Elige color según categoría
            XColor bg = text.ToLower() switch
            {
                var t when t.Contains("laptop") || t.Contains("desktop") => XColor.FromArgb(220, 234, 255),
                var t when t.Contains("móvil") || t.Contains("celular") => XColor.FromArgb(220, 255, 234),
                var t when t.Contains("monitor") || t.Contains("pantalla") => XColor.FromArgb(255, 240, 220),
                var t when t.Contains("teclado") || t.Contains("mouse") => XColor.FromArgb(240, 220, 255),
                _ => XColor.FromArgb(230, 230, 240)
            };
            XColor fg = AzulOscuro;

            string label = Trunc(text, font, gfx, maxW - 10);
            double tw = gfx.MeasureString(label, font).Width;
            double bw = Math.Min(tw + 10, maxW);
            double bh = 14;

            DrawRoundRect(gfx, x, y, bw, bh, 4, new XSolidBrush(bg));
            gfx.DrawString(label, font, new XSolidBrush(fg),
                new XRect(x + 5, y, bw - 10, bh), XStringFormats.CenterLeft);
        }

        private static void DrawEstadoBadge(XGraphics gfx, int estado, XFont font,
            double colX, double rowY, double colW)
        {
            XColor bg = estado >= 8 ? XColor.FromArgb(209, 243, 221)
                      : estado >= 5 ? XColor.FromArgb(255, 243, 205)
                                    : XColor.FromArgb(254, 215, 215);
            XColor fg = estado >= 8 ? XColor.FromArgb(21, 128, 61)
                      : estado >= 5 ? XColor.FromArgb(146, 64, 14)
                                    : XColor.FromArgb(153, 27, 27);

            double bw = 28, bh = 14;
            double bx = colX + (colW - bw) / 2;
            double by = rowY + (22 - bh) / 2;
            DrawRoundRect(gfx, bx, by, bw, bh, 4, new XSolidBrush(bg));
            gfx.DrawString(estado.ToString(), font, new XSolidBrush(fg),
                new XRect(bx, by, bw, bh), XStringFormats.Center);
        }

        private static void DrawFirmaBox(XGraphics gfx, double x, double y, double w,
            string titulo, string nombre, XFont fSmall, XFont fBold)
        {
            double boxH = 64;
            gfx.DrawRectangle(new XPen(GrisBorde, 0.8), x, y, w, boxH);
            // Línea de firma
            gfx.DrawLine(new XPen(AzulOscuro, 0.8), x + 10, y + 36, x + w - 10, y + 36);
            // Nombre
            gfx.DrawString(nombre ?? "", fSmall, new XSolidBrush(GrisTexto),
                new XRect(x + 6, y + 40, w - 12, 14), XStringFormats.TopLeft);
            // Etiqueta inferior
            gfx.DrawRectangle(new XSolidBrush(AzulOscuro), x, y + boxH - 16, w, 16);
            gfx.DrawString(titulo, fBold, new XSolidBrush(Blanco),
                new XRect(x, y + boxH - 16, w, 16), XStringFormats.Center);
        }

        private static string Trunc(string t, XFont f, XGraphics g, double max)
        {
            if (string.IsNullOrEmpty(t) || g.MeasureString(t, f).Width <= max) return t;
            while (t.Length > 1 && g.MeasureString(t + "…", f).Width > max) t = t[..^1];
            return t + "…";
        }
    }
}