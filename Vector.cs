namespace Teknomli
{
    [Serializable]
    public struct Vector : IFormattable
    {
        internal double _x;
        internal double _y;
        public static bool operator ==(Vector vector1, Vector vector2) => vector1.X == vector2.X && vector1.Y == vector2.Y;

        public static bool operator !=(Vector vector1, Vector vector2) => !(vector1 == vector2);

        public static Point operator +(Vector vector, Point point) => new((int)(point.X + vector._x), (int)(point.Y + vector._y));

        public static Vector operator +(Vector vector1, Vector vector2) => new(vector1._x + vector2._x, vector1._y + vector2._y);

        public static Vector operator -(Vector vector1, Vector vector2) => new(vector1._x - vector2._x, vector1._y - vector2._y);

        public static double operator *(Vector vector1, Vector vector2) => vector1._x * vector2._x + vector1._y * vector2._y;

        public static Vector operator *(Vector vector, double scalar) => new(vector._x * scalar, vector._y * scalar);

        public static Vector operator /(Vector vector, double scalar) => vector * (1.0 / scalar);

        public double X
        {
            get => this._x;
            set => this._x = value;
        }

        public double Y
        {
            get => this._y;
            set => this._y = value;
        }

        public double Length => Math.Sqrt(this._x * this._x + this._y * this._y);

        public Vector(double x, double y)
        {
            this._x = x;
            this._y = y;
        }

        public override int GetHashCode()
        {
            double num = this.X;
            int hashCode1 = num.GetHashCode();
            num = this.Y;
            int hashCode2 = num.GetHashCode();
            return hashCode1 ^ hashCode2;
        }

        internal string ConvertToString(string? format, IFormatProvider? provider)
        {;
            return string.Format(provider, "{{1:" + format + "}},{{2:" + format + "}}", new object[2]
            {
                 this._x,
                 this._y
            });
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return this.ConvertToString(null, null);
        }

        public override bool Equals(object? o)
        {
            return o is Vector vector2 && Equals(this, vector2);
        }

        public bool Equals(Vector value　) => Equals(this, value);

        public static Vector Add(Vector vector1, Vector vector2) => new(vector1._x + vector2._x, vector1._y + vector2._y);
        
        public static Vector Subtract(Vector vector1, Vector vector2) => new(vector1._x - vector2._x, vector1._y - vector2._y);

        public static Point Add(Vector vector, Point point) => new((int)(point.X + vector._x), (int)(point.Y + vector._y));

        public static Vector Multiply(Vector vector, double scalar) => new(vector._x * scalar, vector._y * scalar);

        public static Vector Multiply(double scalar, Vector vector) => new(vector._x * scalar, vector._y * scalar);

        public static Vector Divide(Vector vector, double scalar) => vector * (1.0 / scalar);

        public static double Multiply(Vector vector1, Vector vector2) => vector1._x * vector2._x + vector1._y * vector2._y;

        public static double Determinant(Vector vector1, Vector vector2) => vector1._x * vector2._y - vector1._y * vector2._x;

        public static explicit operator Size(Vector vector) => new((int)Math.Abs(vector._x), (int)Math.Abs(vector._y));

        public static explicit operator Point(Vector vector) => new((int)vector._x, (int)vector._y);
    }
}
