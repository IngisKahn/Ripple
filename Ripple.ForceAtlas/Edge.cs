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

    public void Write(BinaryWriter writer)
    {
        writer.Write(this.Source.Id);
        writer.Write(this.Target.Id);
        writer.Write(this.Weight);
    }

    public static Edge Read(BinaryReader reader, IReadOnlyDictionary<uint, Node> nodes)
    {
        var sourceId = reader.ReadUInt32();
        var targetId = reader.ReadUInt32();
        var weight = reader.ReadDouble();
        var source = nodes[sourceId];
        var target = nodes[targetId];
        return new(source, target) { Weight = weight };
    }
}