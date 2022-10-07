using System.Drawing.Imaging;

namespace Teknomli
{
    internal static class BitmapDataEx
    {
        /// <summary>
        /// BitmapのGetPixel同等
        /// </summary>
        /// <param name="x">x座標</param>
        /// <param name="y">y標</param>
        /// <returns>Colorオブジェクト</returns>
        public static Color GetPixel(BitmapData _img, int x, int y)
        {
            if (!(0 <= x && x <= _img.Width && 0 <= y && y <= _img.Height))
            {
                return Color.FromArgb(0);
            }
            unsafe
            {
                byte* adr = (byte*)_img.Scan0;
                int pos = x * 3 + _img.Stride * y;
                if (_img.PixelFormat == PixelFormat.Format32bppArgb)
                    pos = x * 4 + _img.Stride * y;
                byte b = adr[pos + 0];
                byte g = adr[pos + 1];
                byte r = adr[pos + 2];
                byte a = 255;
                if (_img.PixelFormat == PixelFormat.Format32bppArgb)
                    a = adr[pos + 3];
                return Color.FromArgb(a, r, g, b);
            }
        }

        /// <summary>
        /// BitmapのSetPixel同等
        /// </summary>
        /// <param name="x">x座標</param>
        /// <param name="y">y座標</param>
        /// <param name="col">Colorオブジェクト</param>
        public static void SetPixel(BitmapData _img, int x, int y, Color col)
        {
            if (0 <= x && 0 <= y && x <= _img.Width && y <= _img.Height)
            {
                unsafe
                {
                    byte* adr = (byte*)_img.Scan0;
                    int pos = x * 3 + _img.Stride * y;
                    if (_img.PixelFormat == PixelFormat.Format32bppArgb)
                        pos = x * 4 + _img.Stride * y;
                    adr[pos + 0] = col.B;
                    adr[pos + 1] = col.G;
                    adr[pos + 2] = col.R;
                    if (_img.PixelFormat == PixelFormat.Format32bppArgb)
                        adr[pos + 3] = col.A;
                }
            }
        }
    }
}