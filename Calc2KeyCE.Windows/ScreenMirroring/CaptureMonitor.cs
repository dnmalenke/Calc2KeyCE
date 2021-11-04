﻿using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
            resultBmp.Dispose();
            
            var cloned = shrunkImage.ConvertPixelFormatAsync(PixelFormat.Format8bppIndexed, OptimizedPaletteQuantizer.Octree(), ErrorDiffusionDitherer.FloydSteinberg, new TaskConfig()).GetAwaiter().GetResult();
            shrunkImage.Dispose();

            short[] colors = new short[256];

            var pal = (Color[])cloned.Palette.Entries.Clone();
            Parallel.For(0, cloned.Palette.Entries.Length, i =>
            {
                colors[i] = ConvertColor(pal[i].R, pal[i].G, pal[i].B);
            });

            byte[] imageBytes = new byte[colors.Length * sizeof(short) + cloned.Width * cloned.Height];
            Buffer.BlockCopy(colors, 0, imageBytes, 0, colors.Length * sizeof(short));

            BitmapData imageData = cloned.LockBits(new Rectangle(0, 0, cloned.Width, cloned.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            Marshal.Copy(imageData.Scan0, imageBytes, colors.Length * sizeof(short), cloned.Width * cloned.Height);
            cloned.UnlockBits(imageData);
            cloned.Dispose();

            return imageBytes;
        }

        public static short ConvertColor(byte red, byte green, byte blue)
        {
            return (short)(((int)Math.Round(red * 31 / 255.0) << 10) + ((int)Math.Round(green * 31 / 255.0) << 5) + ((int)Math.Round(blue * 31 / 255.0) << 0));
        }
    }
}