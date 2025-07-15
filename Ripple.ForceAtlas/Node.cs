using System.Runtime.CompilerServices;

namespace Ripple.ForceAtlas;

public class Node(double x, double y) : IParticle
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double OldX { get; set; } = x;
    public double OldY { get; set; } = y;
    public double Mass { get; set; } = 1;
    public double Dx { get; set; }
    public double Dy { get; set; }
    public double OldDx { get; set; }
    public double OldDy { get; set; }

    public double Size { get; set; }

    public bool IsFixed { get; set; }
    public int Degree { get; set; }

    public RestrictionRegions Restrictions { get; set; } = new();

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
}