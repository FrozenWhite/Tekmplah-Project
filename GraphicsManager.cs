using System.Diagnostics;
using System.Drawing.Imaging;

namespace Teknomli
{
    internal static class GraphicsManager
    {
        public static Bitmap RotateImage(Bitmap bmp, float angle)
        {
            var alpha = angle;
            while (alpha < 0) alpha += 360;

            var gamma = 90;
            var beta = 180 - angle - gamma;

            float c1 = bmp.Height;
            var a1 = (float)(c1 * Math.Sin(alpha * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));
            var b1 = (float)(c1 * Math.Sin(beta * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));

            float c2 = bmp.Width;
            var a2 = (float)(c2 * Math.Sin(alpha * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));
            var b2 = (float)(c2 * Math.Sin(beta * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));

            var width = Convert.ToInt32(b2 + a1);
            var height = Convert.ToInt32(b1 + a2);

            Bitmap rotatedImage = new Bitmap(width, height);
            using Graphics g = Graphics.FromImage(rotatedImage);
            g.TranslateTransform(rotatedImage.Width / 2, rotatedImage.Height / 2);
            g.RotateTransform(angle);
            g.TranslateTransform(-rotatedImage.Width / 2, -rotatedImage.Height / 2);
            g.DrawImage(bmp, new Point((width - bmp.Width) / 2, (height - bmp.Height) / 2));
            return rotatedImage;
        }

        public static void DrawLine(BitmapData bmp, Color col, int x, int y, int x1, int y1)
        {
            int dx = x1 - x;
            int dy = y1 - x;
            double len = Math.Sqrt(dx * dx + dy * dy);
            double rad = Math.Atan2(dy, dx);
            for (int i = 0; i < len; i++)
            {
                int ex = (int)(x + i * Math.Cos(rad));
                int ey = (int)(y + i * Math.Sin(rad));
                Debug.WriteLine($"x:{ex},y:{ey}");
                BitmapDataEx.SetPixel(bmp, ex, ey, col);
            }
        }

        public static void FillEllipse(BitmapData bmp, Color col, int x, int y, int width, int height)
        {
            for (int xl = 0; xl < bmp.Width; xl++)
            {
                for (int yl = 0; yl < bmp.Height; yl++)
                {
                    int dx = xl - width - x + 1;
                    int dy = yl - height - y + 1;
                    if ((double)(dx * dx) / (width * width) + (double)(dy * dy) / (height * height) <= 1) BitmapDataEx.SetPixel(bmp, xl, yl, col);
                }
            }
        }

        public static void FillPie(BitmapData bmp, Color col, int x, int y, int width, int height, uint startAngle, uint sweepAngle)
        {
            double startRad = Math.ToRad(startAngle);
            double sweepRad = Math.ToRad(sweepAngle);
            if (startRad > sweepRad)
                sweepRad += 2 * Math.PI;
            Parallel.For(0, bmp.Width, xl =>
            {
                for (int yl = 0; yl < bmp.Height; yl++)
                {
                    int dx = xl - width - x + 1;
                    int dy = yl - height - y + 1;
                    double prad = -Math.Atan2(dx, dy);
                    if (prad < 0)
                        prad += 2 * Math.PI;
                    if (prad < startRad || prad > sweepRad)
                    {
                        if (sweepRad >= 2 * Math.PI)
                        {
                            if (prad + 2 * Math.PI < startRad || prad + 2 * Math.PI > sweepRad) continue;
                        }
                        else continue;
                    }
                    if ((double)(dx * dx) / (width * width) + (double)(dy * dy) / (height * height) <= 1) BitmapDataEx.SetPixel(bmp, xl, yl, col);
                }
            });
        }

        public static void FillRectangle(BitmapData bmp, Color col, int x, int y, int width, int height)
        {
            for (int xl = x; xl < x + 50; xl++)
            {
                for (int yl = y; yl < y + 50; yl++)
                {
                    BitmapDataEx.SetPixel(bmp, xl, yl, col);
                }
            }
        }
    }
}
