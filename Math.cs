namespace Teknomli
{
    internal sealed class Math
    {
        public const double PI = 3.14159265359;

        public static bool is_positive_epsilon(double x)
        {
            return x is >= 0.0 and <= 0.001;
        }

        public static bool is_negative_epsilon(double x)
        {
            return x is < 0.0 and >= -0.001;
        }

        /// <summary>
        /// 指定の数値を指定した値で累乗する
        /// </summary>
        /// <param name="x">累乗したい値</param>
        /// <param name="y">累乗を指定する値</param>
        /// <returns>xをyで累乗した値</returns>
        public static double Pow(double x, double y)
        {
            double m = 1;
            for (int i = 1; i < y; i++)
            {
                m *= x;
            }
            return m;
        }

        /// <summary>
        /// 弧度法をラジアンに変換する
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double ToRad(double deg)
        {
            return (Math.PI / 180) * deg;
        }

        /// <summary>
        /// ラジアンを弧度法に変換する
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double ToDeg(double rad)
        {
            return (180 / Math.PI) * rad;
        }

        /// <summary>
        /// 指定された数値の階乗を返す
        /// </summary>
        /// <param name="i"></param>
        /// <returns>iの階乗</returns>
        public static double Fact(int i)
        {
            double fact = 1;
            for (; i >= 1; i--)
            {
                fact *= i;
            }
            return fact;
        }

        /// <summary>
        /// 指定された数値の絶対値を返す
        /// </summary>
        /// <param name="x">絶対値を求めたい数</param>
        /// <returns>整数</returns>
        public static double Abs(double x)
        {
            if (x < 0)
                x *= -1;
            return x;
        }

        /// <summary>
        /// 指定された角度のCosを返す
        /// </summary>
        /// <param name="d">角度(ラジアン)</param>
        /// <returns>dのCos</returns>
        public static double Cos(double d)
        {
            double t = 1.0;
            double y = 1.0;
            int n = 1;
            while (true)
            {
                t = -(t * d * d) / ((2 * n) * (2 * n - 1));
                if (Math.Abs(t) <= 0.001) break;
                y += t;
                n++;
            }
            return y;
        }

        /// <summary>
        /// 指定された角度のSinを返す
        /// </summary>
        /// <param name="d">角度(ラジアン)</param>
        /// <returns>dのSin</returns>
        public static double Sin(double d)
        {
            double t = d;
            double y = d;
            int n = 1;
            while (true)
            {
                t = -(t * d * d) / ((2 * n + 1) * (2 * n));
                if (Math.Abs(t) <= 0.001) break;
                y += t;
                n++;
            }
            return y;
        }

        public static double Tan(double d)
        {
            int k = (int)(d / (PI / 2) + (d >= 0 ? 0.5 : -0.5));
            double d2 = (d - (3217.0 / 2048) * k) + 4.4544551033807686 * k;
            double t = 0;
            for (int i = 19; i >= 3; i -= 2)
            {
                t = d2 * d2 / (i - t);
            }
            t = d2 / (1 - t);
            if ((k % 2) == 0)
                return t;
            if (t != 0)
                return -1 / t;
            return Double.MaxValue;
        }

        public static double Atan(double x)
        {
            int sign;
            double t = 0;
            switch (x)
            {
                case > 1.0:
                    sign = 1;
                    x = 1.0 / x;
                    break;
                case < -1.0:
                    sign = -1;
                    x = 1.0 / x;
                    break;
                default:
                    sign = 0;
                    break;
            }
            for (int i = 24; i >= 1; i--)
            {
                t = (i * i * x * x) / (2 * i + 1 + t);
            }
            return sign switch
            {
                > 0 => Math.PI / 2.0 - x / (1.0 + t),
                0 => x / (1.0 + t),
                _ => -Math.PI / 2.0 - x / (1.0 + t)
            };
        }

        public static double Atan2(double y, double x)
        {
            if (is_positive_epsilon(y) && x < 0.0)
                return Math.PI;
            if (is_negative_epsilon(y) && x < 0.0)
                return -Math.PI;
            if (is_positive_epsilon(y) && x > 0.0)
                return 0.0;
            if (is_negative_epsilon(y) && x > 0.0)
                return 0.0;
            switch (y)
            {
                case < 0.0 when is_positive_epsilon(x) || is_negative_epsilon(x):
                    return -Math.PI / 2.0;
                case > 0.0 when is_positive_epsilon(x) || is_negative_epsilon(x):
                    return Math.PI / 2.0;
            }
            if (double.IsNaN(x) || double.IsNaN(y))
                return double.NaN;
            if (is_positive_epsilon(y) && is_negative_epsilon(x))
                return Math.PI;
            if (is_negative_epsilon(y) && is_negative_epsilon(x))
                return -Math.PI;
            if (is_positive_epsilon(y) && is_positive_epsilon(x))
                return 0.0;
            if (is_negative_epsilon(y) && is_positive_epsilon(x))
                return -0.0;
            switch (y)
            {
                case > 0.0 when (x < 0.0 && double.IsInfinity(x)):
                    return Math.PI;
                case < 0.0 when (x < 0.0 && double.IsInfinity(x)):
                    return -Math.PI;
                case > 0.0 when (x > 0.0 && double.IsInfinity(x)):
                case < 0.0 when (x > 0.0 && double.IsInfinity(x)):
                    return 0.0;
                case > 0.0 when double.IsInfinity(y) && !Double.IsInfinity(x) && !Double.IsNaN(x):
                    return Math.PI / 2.0;
                case < 0.0 when double.IsInfinity(y) && !Double.IsInfinity(x) && !Double.IsNaN(x):
                    return -Math.PI / 2.0;
                case > 0.0 when double.IsInfinity(y) && (x < 0.0 && double.IsInfinity(x)):
                    return 3.0 * Math.PI / 4.0;
                case < 0.0 when double.IsInfinity(y) && (x < 0.0 && double.IsInfinity(x)):
                    return -3.0 * Math.PI / 4.0;
                case > 0.0 when double.IsInfinity(y) && (x > 0.0 && double.IsInfinity(x)):
                    return Math.PI / 4.0;
                case < 0.0 when double.IsInfinity(y) && (x > 0.0 && double.IsInfinity(x)):
                    return -Math.PI / 4.0;
                default:
                    return y switch
                    {
                        > 0.0 when x < 0.0 => Math.Atan(y / x) + Math.PI,
                        < 0.0 when x < 0.0 => Math.Atan(y / x) - Math.PI,
                        _ => Math.Atan(y / x)
                    };
            }
        }


        /// <summary>
        /// 指定された数値の平方根を返す
        /// </summary>
        /// <param name="d">平方根を求める値</param>
        /// <returns></returns>
        public static double Sqrt(double d)
        {
            if (d == 0)
                return 0;
            double y = 1;
            for (int i = 0; i < 1000; i++)
            {
                double z = d / y;
                y = (y + z) / 2;
            }
            return y;
        }
    }
}
