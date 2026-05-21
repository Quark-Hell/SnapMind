using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SnapMind.Screenshoter
{
    public static class ScreenCapture
    {
        // ──────────────────────── public API ────────────────────────

        public static string CaptureToBase64(Rectangle region, ImageFormat? format = null)
        {
            using var bmp = CaptureRegion(region);
            return BitmapToBase64(bmp, format ?? ImageFormat.Png);
        }

        public static string CaptureScreenToBase64(ImageFormat? format = null)
        {
            var bounds = GetPrimaryScreenBounds();
            return CaptureToBase64(bounds, format);
        }

        public static string CaptureToFileAndBase64(
            Rectangle region,
            string filePath,
            ImageFormat? format = null)
        {
            using var bmp = CaptureRegion(region);
            format ??= ImageFormat.Png;
            bmp.Save(filePath, format);
            return BitmapToBase64(bmp, format);
        }

        // ──────────────────────── internals ─────────────────────────

        private static Bitmap CaptureRegion(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
                throw new ArgumentException(
                    $"Region must have positive dimensions, got {region.Width}x{region.Height}.");

            var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);

            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);

            return bmp;
        }

        private static string BitmapToBase64(Bitmap bmp, ImageFormat format)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, format);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static Rectangle GetPrimaryScreenBounds()
        {
            int w = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int h = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            return new Rectangle(0, 0, w, h);
        }

        // ──────────────────────── P/Invoke ──────────────────────────

        private static class NativeMethods
        {
            public const int SM_CXSCREEN = 0;
            public const int SM_CYSCREEN = 1;

            [DllImport("user32.dll")]
            public static extern int GetSystemMetrics(int nIndex);
        }
    }
}
