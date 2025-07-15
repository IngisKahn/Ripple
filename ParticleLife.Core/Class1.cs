namespace ParticleLife.Core;
[System.Runtime.CompilerServices.InlineArray(2)]
public struct Vector2 : IVector
{
    private double element0;
    public void Wrap()
    {
        this[0] = double.Abs(this[0] + 1) % 2 - 1;
        this[1] = double.Abs(this[1] + 1) % 2 - 1;
    }

    public void Clamp()
    {
        this[0] = double.Min(1, double.Max(this[0], -1));
        this[1] = double.Min(1, double.Max(this[1], -1));
    }
}

public interface IVector
{
    void Wrap();
    void Clamp();
}

public interface IAccelerator<T> where T : struct, IVector
{
    void Accelerate(double a, ref T position);
}

public class Particle<T> where T : struct, IVector
{
    public T Position;
    public T Velocity;
    public int Type;
}

public interface IMatrix
{
    int Size { get; }
    double this[int i, int j] { get; set; }
}

public readonly struct Matrix(int size) : IMatrix
{
    private static readonly Random random = new();
    public int Size { get; } = size;
    private readonly double[,] data = new double[size, size];

    public double this[int i, int j] { get => this.data[i, j]; set => this.data[i, j] = value; }

    public void Randomize()
    {
        for (var i = 0; i < this.Size; i++)
            for (var j = 0; j < this.Size; j++)
                this[i, j] = 2 * random.NextDouble() - 1;
    }
}

public record PhysicsSettings
{
    public bool Wrap { get; set; }
    public double MaxRadius { get; set; } = .04;
    public double VelocityHalfLife { get; set; } = .043;
    public double Force { get; set; } = 1;
    public double TimeStep { get; set; } = .02;
}

public interface ITypeSetter<T> where T : struct, IVector
{
    int GetType(ref T position, ref T velocity, int type, int typeCount);
}

public interface IPositionSetter<T> where T : struct, IVector
{
    void Set(ref T position, int type, int typeCount);
}

public interface IMatrixGenerator
{
    IMatrix Generate(int size);
}

public class TypeSetter<T> : ITypeSetter<T> where T : struct, IVector
{
    private static readonly Random random = new();
    public int GetType(ref T position, ref T velocity, int type, int typeCount) => random.Next(typeCount);
}

public class PositionSetter2 : IPositionSetter<Vector2>
{
    private static readonly Random random = new();

    public void Set(ref Vector2 position, int type, int typeCount)
    {
        position[0] = random.NextDouble() * 2 - 1;
        position[1] = random.NextDouble() * 2 - 1;
    }
}

public class MatrixGenerator : IMatrixGenerator
{
    public IMatrix Generate(int size)
    {
        Matrix matrix = new(size);
        matrix.Randomize();
        return matrix;
    }
}