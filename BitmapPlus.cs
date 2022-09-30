using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;

namespace Teknomli
{
    internal static class BitmapDataEx
    {
        /// <summary>
        /// BitmapのGetPixel同等
        /// </summary>
        /// <param name="x">Ｘ座標</param>
        /// <param name="y">Ｙ座標</param>
        /// <returns>Colorオブジェクト</returns>
        public static Color GetPixel(Bitmap _bmp, BitmapData _img, int x, int y)
        {
            if (!(0 <= x && x <= _bmp.Width && 0 <= y && y <= _bmp.Height))
            {
                return Color.FromArgb(0);
            }
            unsafe
            {
                // Bitmap処理の高速化を開始している場合はBitmapメモリへの直接アクセス
                byte* adr = (byte*)_img.Scan0;
                int pos = x * 3 + _img.Stride * y;
                byte b = adr[pos + 0];
                byte g = adr[pos + 1];
                byte r = adr[pos + 2];
                return Color.FromArgb(r, g, b);
            }
        }

        /// <summary>
        /// BitmapのSetPixel同等
        /// </summary>
        /// <param name="x">Ｘ座標</param>
        /// <param name="y">Ｙ座標</param>
        /// <param name="col">Colorオブジェクト</param>
        public static void SetPixel(Bitmap _bmp, BitmapData _img, int x, int y, Color col)
        {
            if (0 <= x && 0 <= y && x <= _bmp.Width && y <= _bmp.Height)
            {
                unsafe
                {
                    // Bitmap処理の高速化を開始している場合はBitmapメモリへの直接アクセス
                    byte* adr = (byte*)_img.Scan0;
                    int pos = x * 3 + _img.Stride * y;
                    adr[pos + 0] = col.B;
                    adr[pos + 1] = col.G;
                    adr[pos + 2] = col.R;
                }
            }
        }
    }
}