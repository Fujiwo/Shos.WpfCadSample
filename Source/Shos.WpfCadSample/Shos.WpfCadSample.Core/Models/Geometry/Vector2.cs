namespace Shos.WpfCadSample.Models.Geometry;

/// <summary>
/// 2 次元の変位（2 点の差、移動量、接線）。
/// </summary>
public readonly record struct Vector2(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);

    public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);
    public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);
    public static Vector2 operator *(Vector2 vector, double factor) => new(vector.X * factor, vector.Y * factor);
    public static Vector2 operator /(Vector2 vector, double divisor) => new(vector.X / divisor, vector.Y / divisor);
}
