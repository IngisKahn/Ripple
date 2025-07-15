namespace Ripple.ForceAtlas;

public record Graph
{
    public Graph() => this.ForceAtlas2 = new(this);

    public IList<Node> Nodes { get; set; } = [];
    public IList<Edge> Edges { get; set; } = [];
    public ForceAtlas2 ForceAtlas2 { get; set; }
}