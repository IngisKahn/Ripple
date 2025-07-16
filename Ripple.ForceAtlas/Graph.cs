namespace Ripple.ForceAtlas;

public record Graph
{
    public Graph() => this.ForceAtlas2 = new(this);
    private Graph(IEnumerable<Node> nodes, IEnumerable<Edge> edges)
    {
        this.nodes = nodes.ToDictionary(n => n.Id);
        this.edges = edges.ToList();
        foreach (var edge in this.Edges)
        {
            this.nodes[edge.Source.Id].Degree++;
            this.nodes[edge.Target.Id].Degree++;
        }
        this.ForceAtlas2 = new(this);
    }

    private readonly Dictionary<uint, Node> nodes = [];
    private readonly List<Edge> edges = [];
    public Dictionary<uint, Node>.ValueCollection Nodes => this.nodes.Values;
    public IReadOnlyList<Edge> Edges => this.edges;
    public ForceAtlas2 ForceAtlas2 { get; }

    public Node AddNode(double x, double y, string? label = null)
    {
        var node = new Node(x, y, label);
        this.nodes[node.Id] = node;
        return node;
    }

    public Edge AddEdge(Node source, Node target, double weight = .00001)
    {
        if (source == target)
            throw new ArgumentException("Source and target nodes cannot be the same.");
        var edge = new Edge(source, target) { Weight = weight };
        this.edges.Add(edge);
        source.Degree++;
        target.Degree++;
        return edge;
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