namespace Ripple.ForceAtlas;

public interface IParticle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Mass { get; set; }

    public double Dx { get; set; }
    public double Dy { get; set; }
    public double Size { get; set; }
}