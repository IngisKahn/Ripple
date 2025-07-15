namespace Ripple.ForceAtlas;

public class ForceAtlas2(Graph graph)
{
    public double BarnesHutTheta { get; set; } = 1.2;
    public double EdgeWeightInfluence { get; set; } = 1;
    public double JitterTolerance { get; set; } = 1;
    public bool IsLogMode { get; set; }
    public bool IsNormalizeEdgeWeights { get; set; } = false;
    public double ScalingRatio { get; set; }
    public bool IsStrongGravityMode { get; set; }
    public bool IsInvertedEdgeWeightsMode { get; set; } = false;
    public double Gravity { get; set; } = 1;
    public bool IsOutboundAttractionDistribution { get; set; } = false;
    public bool IsAdjustSizes { get; set; } = false;
    public bool IsBarnesHutOptimize { get; set; }
    public double EdgeGamma { get; set; } = 20;
    public bool IsMultiThread { get; set; }
    private double speed = 1;
    private double speedEfficiency = 1;
    public Region? RootNodeRegion { get; private set; }
    public Region? RootEdgeRegion { get; private set; }
    private double outboundAttractionCompensation;

    private static readonly Node.RestrictionRegions rr = new();

    static ForceAtlas2()
    {
        for (var i = 0; i < 8; i++)
            rr[i] = double.PositiveInfinity;
    }

    public void Initialize()
    {
        this.ScalingRatio = graph.Nodes.Count >= 100 ? 2 : 10;
        this.IsBarnesHutOptimize = graph.Nodes.Count >= 1000;

        foreach (var n in graph.Nodes)
        {
            n.Mass = 1 + n.Degree;
            n.OldDx = 0;
            n.OldDy = 0;
            n.Dx = 0;
            n.Dy = 0;
            //n.Restrictions =  rr;
            for (var i = 0; i < 8; i++)
                n.Restrictions[i] = double.PositiveInfinity;
        }
        foreach (var e in graph.Edges)
            e.UpdatePhysics();
    }

    private double GetEdgeWeight(Edge e) => this.IsInvertedEdgeWeightsMode ? e.Weight == 0 ? 0 : 1 / e.Weight : e.Weight;

    public void RegionStep()
    {
        this.RootNodeRegion = this.RootNodeRegion != null ? this.RootNodeRegion.Refresh(graph.Nodes) : new(graph.Nodes);
        this.RootEdgeRegion = this.RootEdgeRegion != null ? this.RootEdgeRegion.Refresh(graph.Edges) : new(graph.Edges);
    }

    private static readonly ParallelOptions multi = new();
    private static readonly ParallelOptions single = new() { MaxDegreeOfParallelism = 1 };

    public void Step()
    {
        foreach (var n in graph.Nodes)
        {
            n.Mass = 1 + n.Degree;
            n.OldDx = n.Dx;
            n.OldDy = n.Dy;
            n.Dx = 0;
            n.Dy = 0;
            //n.Restrictions =  rr;
            for (var i = 0; i < 8; i++)
                n.Restrictions[i] = double.PositiveInfinity;
        }
        foreach (var e in graph.Edges)
            e.UpdatePhysics();

        if (this.IsBarnesHutOptimize)
            this.RegionStep();

        if (this.IsOutboundAttractionDistribution)
            this.outboundAttractionCompensation = graph.Nodes.Sum(n => n.Mass);

        var repulsion = ForceMill.BuildRepulsion(this.IsAdjustSizes, 1, this.EdgeGamma);
        var gravityForce = this.IsStrongGravityMode ? ForceMill.GetStrongGravity(this.ScalingRatio) : repulsion;

        Parallel.ForEach(graph.Nodes, this.IsMultiThread ? multi : single, n =>
        {
            if (this.IsBarnesHutOptimize)
            {
                this.RootNodeRegion!.ApplyForce(n, repulsion, this.BarnesHutTheta);
                this.RootEdgeRegion!.ApplyEdgeForce(n, repulsion, this.BarnesHutTheta, this.EdgeGamma);
            }
            else
            {
                foreach (var n2 in graph.Nodes)
                {
                    if (n == n2)
                        break;
                    repulsion.Apply(n, n2);
                }

                foreach (var graphEdge in graph.Edges)
                    repulsion.Apply(n, graphEdge);
            }

            gravityForce.Apply(n, this.Gravity / this.ScalingRatio);
        });
        var springK = 10; //this.speedEfficiency;

       // Attraction
       var attraction = ForceMill.BuildAttraction(this.IsLogMode, this.IsOutboundAttractionDistribution, this.IsAdjustSizes, this.IsOutboundAttractionDistribution ? this.outboundAttractionCompensation : springK);
        if (Math.Abs(this.EdgeWeightInfluence) < 1e-4)
            foreach (var graphEdge in graph.Edges)
                attraction.Apply(graphEdge.Source, graphEdge.Target, 1);
        else if (this.IsNormalizeEdgeWeights)
        {
            var edgeWeightMin = double.MaxValue;
            var edgeWeightMax = double.MinValue;
            foreach (var graphEdge in graph.Edges)
            {
                var edgeWeight = this.GetEdgeWeight(graphEdge);
                edgeWeightMin = Math.Min(edgeWeightMin, edgeWeight);
                edgeWeightMax = Math.Max(edgeWeightMax, edgeWeight);
            }
            var edgeWeightRange = edgeWeightMax - edgeWeightMin;
            if (edgeWeightRange > 1e-4)
            {
                if (Math.Abs(this.EdgeWeightInfluence - 1) < 1e-4)
                    foreach (var graphEdge in graph.Edges)
                        attraction.Apply(graphEdge.Source, graphEdge.Target,
                            (this.GetEdgeWeight(graphEdge) - edgeWeightMin) / edgeWeightRange);
                else
                    foreach (var graphEdge in graph.Edges)
                        attraction.Apply(graphEdge.Source, graphEdge.Target,
                            Math.Pow((this.GetEdgeWeight(graphEdge) - edgeWeightMin) / edgeWeightRange, this.EdgeWeightInfluence));
            }
            else
                foreach (var graphEdge in graph.Edges)
                    attraction.Apply(graphEdge.Source, graphEdge.Target, 1);
        }
        else if (Math.Abs(this.EdgeWeightInfluence - 1) < 1e-4)
            foreach (var graphEdge in graph.Edges)
                attraction.Apply(graphEdge.Source, graphEdge.Target, this.GetEdgeWeight(graphEdge));
        else
            foreach (var graphEdge in graph.Edges)
                attraction.Apply(graphEdge.Source, graphEdge.Target, Math.Pow(this.GetEdgeWeight(graphEdge), this.EdgeWeightInfluence));

        // Auto adjust speed
        var totalSwinging = .0;  // How much irregular movement
        var totalEffectiveTraction = .0;  // How much useful movement
        foreach (var n in graph.Nodes.Where(n => !n.IsFixed))
        {
            totalSwinging += n.Mass * Math.Sqrt((n.OldDx - n.Dx) * (n.OldDx - n.Dx) + (n.OldDy - n.Dy) * (n.OldDy - n.Dy));

            totalEffectiveTraction += n.Mass * .5 * Math.Sqrt((n.OldDx + n.Dx) * (n.OldDx + n.Dx) + (n.OldDy + n.Dy) * (n.OldDy + n.Dy));
        }
        // We want that swingingMovement < tolerance * convergenceMovement

        // Optimize jitter tolerance
        // The 'right' jitter tolerance for this network. Bigger networks need more tolerance. Denser networks need less tolerance. Totally empiric.
        var estimatedOptimalJitterTolerance = .05 * Math.Sqrt(graph.Nodes.Count);
        var minJt = Math.Sqrt(estimatedOptimalJitterTolerance);
        const double maxJt = 10d;
        var jt = this.JitterTolerance * Math.Max(minJt, Math.Min(maxJt, estimatedOptimalJitterTolerance * totalEffectiveTraction / (graph.Nodes.Count * graph.Nodes.Count)));

        const double minSpeedEfficiency = .05;

        // Protection against erratic behavior
        if (totalSwinging / totalEffectiveTraction > 2)
        {
            if (this.speedEfficiency > minSpeedEfficiency)
                this.speedEfficiency *= .5;
            jt = Math.Max(jt, this.JitterTolerance);
        }

        var targetSpeed = jt * this.speedEfficiency * totalEffectiveTraction / totalSwinging;

        // Speed efficiency is how the speed really corresponds to the swinging vs. convergence tradeoff.
        // We adjust it slowly and carefully.
        if (totalSwinging > jt * totalEffectiveTraction)
        {
            if (this.speedEfficiency > minSpeedEfficiency)
                this.speedEfficiency *= .7;
        }
        else if (this.speed < 1000)
            this.speedEfficiency *= 1.3;

        // But the speed shouldn't rise too much too quickly, since it would make the convergence drop dramatically.
        const double maxRise = .5;
        this.speed += Math.Min(targetSpeed - this.speed, maxRise * this.speed);

        // Apply forces
        if (this.IsAdjustSizes)
            foreach (var n in graph.Nodes.Where(n => !n.IsFixed))
            {
                // Adaptive auto-speed: the speed of each node is lowered when the node swings.
                var swinging = n.Mass * Math.Sqrt((n.OldDx - n.Dx) * (n.OldDx - n.Dx) + (n.OldDy - n.Dy) * (n.OldDy - n.Dy));
                var factor = .1 * this.speed / (1 + Math.Sqrt(this.speed * swinging));

                var df = Math.Sqrt(n.Dx * n.Dx + n.Dy * n.Dy);
                factor = Math.Min(factor * df, 10) / df;

                n.X += n.Dx * factor;
                n.Y += n.Dy * factor;
            }
        else
            foreach (var n in graph.Nodes.Where(n => !n.IsFixed))
            {
                // Adaptive auto-speed: the speed of each node is lowered when the node swings.
                var swinging = n.Mass * Math.Sqrt((n.OldDx - n.Dx) * (n.OldDx - n.Dx) + (n.OldDy - n.Dy) * (n.OldDy - n.Dy));
                var factor = this.speed / (1 + Math.Sqrt(this.speed * swinging)) / n.Mass;
                var dx = n.Dx * factor;
                var dy = n.Dy * factor;

                var dDistance = Math.Sqrt(dx * dx + dy * dy);

                // restrict movement
                var arc = Mod(-(int)Math.Floor(Math.Atan2(-dy, dx) * 4 / Math.PI), 8);
                if (dDistance > n.Restrictions[arc])
                {
                    var scale = n.Restrictions[arc] / dDistance;
                    dx *= scale;
                    dy *= scale;
                }

                n.OldX = n.X;
                n.OldY = n.Y;
                n.X += dx;
                n.Y += dy;
                continue;

                static int Mod(int x, int m) => (x % m + m) % m;
            }
    }
}