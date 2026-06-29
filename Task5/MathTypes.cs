using System;

namespace Task5
{

    public readonly struct Vec3
    {
        public static readonly Vec3 Zero = new(0, 0, 0);

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vec3 Normalized()
        {
            double length = Length;
            if (length < 0.000001)
                return new Vec3(0, 0, 0);

            return this / length;
        }

        public static double Dot(Vec3 a, Vec3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 v, double k) => new(v.X * k, v.Y * k, v.Z * k);
        public static Vec3 operator /(Vec3 v, double k) => new(v.X / k, v.Y / k, v.Z / k);
    }

    public readonly struct ColorVec
    {
        public static readonly ColorVec Black = new(0, 0, 0);

        public double R { get; }
        public double G { get; }
        public double B { get; }

        public ColorVec(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        public ColorVec Clamped()
        {
            return new ColorVec(Math.Clamp(R, 0.0, 1.0), Math.Clamp(G, 0.0, 1.0), Math.Clamp(B, 0.0, 1.0));
        }

        public static ColorVec operator +(ColorVec a, ColorVec b) => new(a.R + b.R, a.G + b.G, a.B + b.B);
        public static ColorVec operator *(ColorVec a, double k) => new(a.R * k, a.G * k, a.B * k);
        public static ColorVec operator *(ColorVec a, ColorVec b) => new(a.R * b.R, a.G * b.G, a.B * b.B);
    }

    public readonly struct Matrix4
    {
        private readonly double[,] _m;

        public Matrix4(double[,] values)
        {
            _m = values;
        }

        public static Matrix4 Translation(double tx, double ty, double tz)
        {
            return new Matrix4(new double[,]
            {
                { 1, 0, 0, tx },
                { 0, 1, 0, ty },
                { 0, 0, 1, tz },
                { 0, 0, 0, 1 }
            });
        }

        public static Matrix4 RotationX(double angle)
        {
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);

            return new Matrix4(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, c, -s, 0 },
                { 0, s, c, 0 },
                { 0, 0, 0, 1 }
            });
        }

        public static Matrix4 RotationY(double angle)
        {
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);

            return new Matrix4(new double[,]
            {
                { c, 0, s, 0 },
                { 0, 1, 0, 0 },
                { -s, 0, c, 0 },
                { 0, 0, 0, 1 }
            });
        }

        public Vec3 TransformPoint(Vec3 p)
        {
            double x = _m[0, 0] * p.X + _m[0, 1] * p.Y + _m[0, 2] * p.Z + _m[0, 3];
            double y = _m[1, 0] * p.X + _m[1, 1] * p.Y + _m[1, 2] * p.Z + _m[1, 3];
            double z = _m[2, 0] * p.X + _m[2, 1] * p.Y + _m[2, 2] * p.Z + _m[2, 3];

            return new Vec3(x, y, z);
        }

        public Matrix4 InverseRigid()
        {
            double[,] r = new double[4, 4];

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                    r[row, col] = _m[col, row];
            }

            Vec3 t = new(_m[0, 3], _m[1, 3], _m[2, 3]);
            Vec3 newT = new(-(r[0, 0] * t.X + r[0, 1] * t.Y + r[0, 2] * t.Z), -(r[1, 0] * t.X + r[1, 1] * t.Y + r[1, 2] * t.Z), -(r[2, 0] * t.X + r[2, 1] * t.Y + r[2, 2] * t.Z));

            r[0, 3] = newT.X;
            r[1, 3] = newT.Y;
            r[2, 3] = newT.Z;
            r[3, 0] = 0;
            r[3, 1] = 0;
            r[3, 2] = 0;
            r[3, 3] = 1;

            return new Matrix4(r);
        }

        public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            double[,] result = new double[4, 4];

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    double sum = 0;
                    for (int k = 0; k < 4; k++)
                        sum += a._m[row, k] * b._m[k, col];

                    result[row, col] = sum;
                }
            }

            return new Matrix4(result);
        }
    }

}