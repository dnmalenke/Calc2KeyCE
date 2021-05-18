using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp.PixelFormats;
using Vanara.PInvoke;

namespace Calc2KeyCE.ScreenMirroring
{
    public static class CaptureMonitor
    {
        public static byte[] Capture(Screen monitor)
        {
            Rectangle monitorRect = monitor.Bounds;
            Bitmap resultBmp = null;
            HWND desktopWindow = User32.GetDesktopWindow();
            HDC windowDc = User32.GetWindowDC(desktopWindow);
            HDC memDc = Gdi32.CreateCompatibleDC(windowDc);
            var bitmap = Gdi32.CreateCompatibleBitmap(windowDc, monitorRect.Width, monitorRect.Height);
            var oldBitmap = Gdi32.SelectObject(memDc, bitmap);

            bool result = Gdi32.BitBlt(memDc, 0, 0, monitorRect.Width, monitorRect.Height, windowDc, 0, 0, Gdi32.RasterOperationMode.SRCCOPY);

            if (result)
            {
                resultBmp = bitmap.ToBitmap();
            }

            Gdi32.SelectObject(memDc, oldBitmap);
            Gdi32.DeleteObject(bitmap);
            Gdi32.DeleteDC(memDc);
            User32.ReleaseDC(desktopWindow, windowDc);

            var shrunkImage = resultBmp.GetThumbnailImage(320, 240, null, IntPtr.Zero);
            shrunkImage.RotateFlip(RotateFlipType.Rotate180FlipX);

            Bitmap clone = new Bitmap(shrunkImage.Width, shrunkImage.Height, PixelFormat.Format16bppRgb565);

            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(shrunkImage, new Rectangle(0, 0, clone.Width, clone.Height));
            }

            byte[] bmpArray;

            using (MemoryStream ms = new())
            {
                clone.Save(ms, ImageFormat.Bmp);
                bmpArray = ms.ToArray();
            }

            clone.Dispose();
            resultBmp.Dispose();
            shrunkImage.Dispose();

            return bmpArray;
        }
    }
}
