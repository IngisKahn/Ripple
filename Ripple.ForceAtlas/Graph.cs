namespace Ripple.ForceAtlas;

public record Graph
{
    public Graph() => this.ForceAtlas2 = new(this);
    private Graph(IEnumerable<Node> nodes, IEnumerable<Edge> edges)
    {
        this.nodes = nodes.ToDictionary(n => n.Id);
        this.Edges = edges.ToList();
        this.ForceAtlas2 = new(this);
    }

    private readonly Dictionary<uint, Node> nodes = [];
    public Dictionary<uint, Node>.ValueCollection Nodes => this.nodes.Values;
    public IList<Edge> Edges { get; init; } = [];
    public ForceAtlas2 ForceAtlas2 { get; }

    public Node AddNode(double x, double y, string? label = null)
    {
        var node = new Node(x, y, label);
        this.nodes[node.Id] = node;
        return node;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(this.nodes.Count);
        foreach (var node in this.nodes.Values) 
            node.Write(writer);
        writer.Write(this.Edges.Count);
        foreach (var edge in this.Edges) 
            edge.Write(writer);
    }

    public static Graph Read(BinaryReader reader)
    {
        var nodeCount = reader.ReadInt32();
        var nodes = new List<Node>(nodeCount);
        for (var i = 0; i < nodeCount; i++)
        {
            var node = Node.Read(reader);
            nodes.Add(node);
        }
        var edgeCount = reader.ReadInt32();
        var edges = new List<Edge>(edgeCount);
        for (var i = 0; i < edgeCount; i++)
        {
            var edge = Edge.Read(reader, nodes.ToDictionary(n => n.Id));
            edges.Add(edge);
        }
        return new(nodes, edges);
    }
}