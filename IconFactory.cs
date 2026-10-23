using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ScrollFlip;

/// <summary>
/// Gera os ícones da bandeja em tempo de execução (seta para cima = normal, para baixo = invertido).
/// </summary>
internal static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create(bool inverted)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            Color color = inverted ? Color.FromArgb(255, 120, 60) : Color.FromArgb(90, 170, 255);
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.DrawLine(pen, 16, inverted ? 6 : 26, 16, inverted ? 26 : 6);

            Point[] head = inverted
                ? [new Point(16, 29), new Point(7, 18), new Point(25, 18)]
                : [new Point(16, 3), new Point(7, 14), new Point(25, 14)];
            g.FillPolygon(brush, head);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
