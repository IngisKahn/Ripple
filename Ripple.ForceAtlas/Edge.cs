namespace Ripple.ForceAtlas;

public class Edge(Node source, Node target) : IParticle
{
    public Node Source { get; } = source;
    public Node Target { get; } = target;
    public double Weight { get; init; } = .00001;
    public double X { get; set; }
    public double Y { get; set; }
    public double Mass { get; set; }
    public double Dx { get; set; }
    public double Dy { get; set; }

    public double Nx { get; private set; }
    public double Ny { get; private set; }
    public double Size { get; set; }

    public double Length { get; private set; }

    public void UpdatePhysics()
    {
        this.X = (this.Source.X + this.Target.X) / 2;
        this.Y = (this.Source.Y + this.Target.Y) / 2;
        this.Mass = (this.Source.Mass + this.Target.Mass) / 2;
        var vx = this.Target.X - this.Source.X;
        var vy = this.Target.Y - this.Source.Y;
        var magnitude = Math.Sqrt(vx * vx + vy * vy);
        this.Length = magnitude;
        this.Size = magnitude + (this.Target.Size + this.Source.Size) / 2; 
        this.Nx = vx / magnitude;
        this.Ny = vy / magnitude;
    }

    public override string ToString() => $"{this.Source} -> {this.Target}";
}

public interface IParticle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Mass { get; set; }

    public double Dx { get; set; }
    public double Dy { get; set; }
    public double Size { get; set; }
}