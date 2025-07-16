namespace Ripple.ForceAtlas;

public class Node : IParticle
{
    private static uint nextId;
    internal uint Id { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public double OldX { get; set; }
    public double OldY { get; set; }
    public double Mass { get; set; } = 1;
    public double Dx { get; set; }
    public double Dy { get; set; }
    public double OldDx { get; set; }
    public double OldDy { get; set; }

    public double Size { get; set; }

    public bool IsFixed { get; set; }
    public int Degree { get; set; }

    public string? Label { get; set; }

    public RestrictionRegions Restrictions { get; set; } = new();

    public Node(double x, double y, string? label = null)
    {
        this.OldX = this.X = x;
        this.OldY = this.Y = y;
        this.Label = label;
        this.Id = nextId++;
    }

    private Node(uint id, double x, double y, string? label = null)
    {
        this.Id = id;
        this.OldX = this.X = x;
        this.OldY = this.Y = y;
        this.Label = label;
    }

    //[InlineArray(8)]
    public class RestrictionRegions
    {
        //private double element;
        private readonly double[] element = new double[8];

        public double this[int i]
        { 
            //readonly get => this[i];
            //set => this[i] = value;
            get => element[i];
            set => element[i] = value;
        }

        public override string ToString()
        {
            var regions = this;
            return $"[{string.Join(',', Enumerable.Range(0, 8).Select(i => regions[i].ToString("N3")))}]";
        }
    }

    public override string ToString() => $"({X:N3}, {Y:N3})";

    public void Write(BinaryWriter writer)
    {
        writer.Write(this.Id);
        writer.Write(this.X);
        writer.Write(this.Y);
        writer.Write(this.Mass);
        writer.Write(this.Size);
        writer.Write(this.IsFixed);
        // write label as UTF-8 string
        if (this.Label is not null)
        {
            var labelBytes = System.Text.Encoding.UTF8.GetBytes(this.Label);
            writer.Write((byte)labelBytes.Length);
            writer.Write(labelBytes);
        }
        else
            writer.Write((byte) 0); // write length 0 for null label
    }

    public static Node Read(BinaryReader reader)
    {
        var id = reader.ReadUInt32();
        var x = reader.ReadDouble();
        var y = reader.ReadDouble();
        var mass = reader.ReadDouble();
        var size = reader.ReadDouble();
        var isFixed = reader.ReadBoolean();
        var labelLength = reader.ReadByte();
        string? label = null;
        if (labelLength > 0)
        {
            var labelBytes = reader.ReadBytes(labelLength);
            label = System.Text.Encoding.UTF8.GetString(labelBytes);
        }
        Node node = new(id, x, y, label)
        {
            Mass = mass,
            Size = size,
            IsFixed = isFixed
        };
        return node;
    }
}