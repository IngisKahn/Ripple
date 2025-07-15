namespace Ripple.ForceAtlas;

using static ForceMill;

public class Region
{
    private readonly List<IParticle> bottomLeftNodes = [];
    private readonly List<IParticle> bottomRightNodes = [];

    private readonly List<IParticle> leftNodes = [];
    private readonly HashSet<IParticle> nodes;
    private readonly List<IParticle> rightNodes = [];
    private readonly List<IParticle> topLeftNodes = [];
    private readonly List<IParticle> topRightNodes = [];
    public Region? BottomLeft;
    public Region? BottomRight;
    public Region? TopLeft;
    public Region? TopRight;

    public void SubRegions(Action<Region> action)
    {
        if (this.TopLeft != null)
            action(this.TopLeft);
        if (this.BottomLeft != null)
            action(this.BottomLeft);
        if (this.BottomRight != null)
            action(this.BottomRight);
        if (this.TopRight != null)
            action(this.TopRight);
    }

    //private readonly object lockObject = new();

    public Region(IEnumerable<IParticle> nodes)
    {
        this.nodes = [.. nodes];
        UpdateMassAndGeometry();
    }

    public double Mass { get; set; }
    public double MassCenterX { get; set; }
    public double MassCenterY { get; set; }
    public double Size { get; private set; }

    private void UpdateMassAndGeometry()
    {
        //if (nodes.Count <= 1)
        //    return;
        // Compute Mass
        this.Mass = 0;
        var massSumX = .0;
        var massSumY = .0;
        foreach (var n in nodes)
        {
            this.Mass += n.Mass;
            massSumX += n.X * n.Mass;
            massSumY += n.Y * n.Mass;
        }

        this.MassCenterX = massSumX / this.Mass;
        this.MassCenterY = massSumY / this.Mass;

        // Compute size
        this.Size = nodes
            .Select(n => Math.Sqrt(
                (n.X - this.MassCenterX) * (n.X - this.MassCenterX) +
                (n.Y - this.MassCenterY) * (n.Y - this.MassCenterY)))
            .Max(d => d * 2);

        RefreshSubRegions();
    }

    public Region Refresh(IEnumerable<IParticle> otherNodes)
    {
        if (!nodes.SetEquals(otherNodes))
            return new(otherNodes);
        UpdateMassAndGeometry();
        return this;
    }

    private void Clear()
    {
        nodes.Clear();
        topLeftNodes.Clear();
        bottomLeftNodes.Clear();
        bottomRightNodes.Clear();
        topRightNodes.Clear();
        //this.SubRegions(r => r.Clear());
        this.TopLeft?.Clear();
        this.BottomLeft?.Clear();
        this.BottomRight?.Clear();
        this.TopRight?.Clear();
    }

    public int Count => nodes.Count;

    private void RefreshSubRegions()
    {
        //lock (this.lockObject)
        {
            if (nodes.Count <= 1)
                return;
            foreach (var n in nodes)
                (n.X < this.MassCenterX ? leftNodes : rightNodes).Add(n);

            foreach (var n in leftNodes)
                (n.Y < this.MassCenterY ? topLeftNodes : bottomLeftNodes).Add(n);

            leftNodes.Clear();

            foreach (var n in rightNodes)
                (n.Y < this.MassCenterY ? topRightNodes : bottomRightNodes).Add(n);

            rightNodes.Clear();

            if (topLeftNodes.Count > 0)
                this.TopLeft = this.TopLeft != null ? this.TopLeft.Refresh(topLeftNodes) : new(topLeftNodes);
            else
                this.TopLeft?.Clear();
            topLeftNodes.Clear();

            if (bottomLeftNodes.Count > 0)
                this.BottomLeft = this.BottomLeft != null ? this.BottomLeft.Refresh(bottomLeftNodes) : new(bottomLeftNodes);
            else
                this.BottomLeft?.Clear();
            bottomLeftNodes.Clear();

            if (bottomRightNodes.Count > 0)
                this.BottomRight = this.BottomRight != null ? this.BottomRight.Refresh(bottomRightNodes) : new(bottomRightNodes);
            else
                this.BottomRight?.Clear();
            bottomRightNodes.Clear();

            if (topRightNodes.Count > 0)
                this.TopRight = this.TopRight != null ? this.TopRight.Refresh(topRightNodes) : new(topRightNodes);
            else
                this.TopRight?.Clear();
            topRightNodes.Clear();
        }
    }

    public void ApplyForce(IParticle p, RepulsionForce force, double theta)
    {
        if (nodes.Count < 2)
        {
            if (nodes.Count == 1)
                force.Apply(p, nodes.First());
        }
        else
        {
            var distance = Math.Sqrt(
                (p.X - this.MassCenterX) * (p.X - this.MassCenterX) +
                (p.Y - this.MassCenterY) * (p.Y - this.MassCenterY));
            if (distance * theta > this.Size)
            {
                force.Apply(p, this);
            }
            else
            {
                //this.SubRegions(r => r.ApplyForce(n, force, theta));
                this.TopLeft?.ApplyForce(p, force, theta);
                this.BottomLeft?.ApplyForce(p, force, theta);
                this.BottomRight?.ApplyForce(p, force, theta);
                this.TopRight?.ApplyForce(p, force, theta);
            }
        }
    }

    public void ApplyEdgeForce(IParticle p, RepulsionForce force, double theta, double edgeGamma)
    {
        if (nodes.Count < 2)
        {
            if (nodes.Count == 1)
                force.Apply(p, (Edge) nodes.First());
        }
        else
        {
            var distance = Math.Sqrt(
                (p.X - this.MassCenterX) * (p.X - this.MassCenterX) +
                (p.Y - this.MassCenterY) * (p.Y - this.MassCenterY));
            if (!(distance * theta < this.Size + edgeGamma))
                return;
            //this.SubRegions(r => r.ApplyForce(n, force, theta));
            this.TopLeft?.ApplyEdgeForce(p, force, theta, edgeGamma);
            this.BottomLeft?.ApplyEdgeForce(p, force, theta, edgeGamma);
            this.BottomRight?.ApplyEdgeForce(p, force, theta, edgeGamma);
            this.TopRight?.ApplyEdgeForce(p, force, theta, edgeGamma);
        }
    }
}