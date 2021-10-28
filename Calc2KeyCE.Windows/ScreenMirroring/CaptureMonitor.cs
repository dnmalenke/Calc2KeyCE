using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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

            var cloned = ImageExtensions.ConvertPixelFormat(shrunkImage, PixelFormat.Format8bppIndexed, OptimizedPaletteQuantizer.Octree(),ErrorDiffusionDitherer.FloydSteinberg);

            ////var cloned = ((Bitmap)shrunkImage).Clone(new Rectangle(0, 0, shrunkImage.Width, shrunkImage.Height), PixelFormat.Format8bppIndexed);
            //Bitmap cloned = new(shrunkImage.Width, shrunkImage.Height, PixelFormat.Format8bppIndexed);

            //using (MemoryStream ms = new MemoryStream())
            //{
            //    shrunkImage.Save(ms, ImageFormat.Gif);
            //    cloned = (Bitmap)Image.FromStream(ms);
            //}


            
            
            short[] colors = new short[256];

            for (int i = 0; i < cloned.Palette.Entries.Length; i++)
            {
                colors[i] = ConvertColor(cloned.Palette.Entries[i].R, cloned.Palette.Entries[i].G, cloned.Palette.Entries[i].B);
            }

            byte[] colorBytes = new byte[colors.Length * sizeof(short)];
            Buffer.BlockCopy(colors, 0, colorBytes, 0, colorBytes.Length);

            resultBmp.Dispose();
            shrunkImage.Dispose();

            List<byte> data = new List<byte>(); // indexes
            List<Color> pixels = cloned.Palette.Entries.ToList(); // palette

            for (int i = 0; i < cloned.Height; i++)
                for (int j = 0; j < cloned.Width; j++)
                    data.Add((byte)pixels.IndexOf(cloned.GetPixel(j, i)));

            //return data.ToArray();

            cloned.Dispose();

            return colorBytes.Concat(data).ToArray();
        }


        public static short ConvertColor(byte red, byte green, byte blue)
        {
            short color = (short)((int)Math.Round(red * 31 / 255.0) << 10);
            color += (short)((int)Math.Round(green * 31 / 255.0) << 5);
            color += (short)((int)Math.Round(blue * 31 / 255.0) << 0);

            return color;
        }
    }
}