using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calc2KeyCE
{
    public static class ImageExtensions
    {
        public static byte[] ToByteArray(this Image img, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, format);
                return ms.ToArray();
            }
        }
    }
}
