namespace Teknomli
{
    [Serializable]
    public struct Vector : IFormattable
    {
        private double _x;
        private double _y;
        public static bool operator ==(Vector vector1, Vector vector2) => vector1.X.Equals(vector2.X) && vector1.Y.Equals(vector2.Y);

        public static bool operator !=(Vector vector1, Vector vector2) => !(vector1 == vector2);

        public static Point operator +(Vector vector, Point point) => new((int)(point.X + vector.X), (int)(point.Y + vector.Y));

        public static Vector operator +(Vector vector1, Vector vector2) => new(vector1.X + vector2.X, vector1.Y + vector2.Y);

        public static Vector operator -(Vector vector1, Vector vector2) => new(vector1.X - vector2.X, vector1.Y - vector2.Y);

        public static double operator *(Vector vector1, Vector vector2) => vector1.X * vector2.X + vector1.Y * vector2.Y;

        public static Vector operator *(Vector vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);

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

        public static bool Equals(Vector vector1, Vector vector2) => vector1.X.Equals(vector2.X) && vector1.Y.Equals(vector2.Y);

        public override bool Equals(object? o) => o is Vector vector2 && Vector.Equals(this, vector2);

        public bool Equals(Vector value) => Vector.Equals(this, value);

        public static Vector Add(Vector vector1, Vector vector2) => new(vector1.X + vector2.X, vector1.Y + vector2.Y);
        
        public static Vector Subtract(Vector vector1, Vector vector2) => new(vector1.X - vector2.X, vector1.Y - vector2.Y);

        public static Point Add(Vector vector, Point point) => new((int)(point.X + vector.X), (int)(point.Y + vector.Y));

        public static Vector Multiply(Vector vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);

        public static Vector Multiply(double scalar, Vector vector) => new(vector.X * scalar, vector.Y * scalar);

        public static Vector Divide(Vector vector, double scalar) => vector * (1.0 / scalar);

        public static double Multiply(Vector vector1, Vector vector2) => vector1.X * vector2.X + vector1.Y * vector2.Y;

        public static double Determinant(Vector vector1, Vector vector2) => vector1.X * vector2.Y - vector1.Y * vector2.X;

        public static explicit operator Size(Vector vector) => new((int)Math.Abs(vector.X), (int)Math.Abs(vector.Y));

        public static explicit operator Point(Vector vector) => new((int)vector.X, (int)vector.X);
    }
}
