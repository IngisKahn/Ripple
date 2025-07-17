namespace Ripple.UI;

using System.IO.IsolatedStorage;
using ForceAtlas;
using System.Runtime.InteropServices;

public partial class Form1 : Form
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Handle;
        public uint Message;
        public IntPtr WParameter;
        public IntPtr LParameter;
        public uint Time;
        public Point Location;
    }

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageA")]
    private static partial int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);

    private Graph graph = new();

    private Node? selected;
    private SizeF mid;
    private PointF pan;
    private float zoom = 1;

    private PointF Transform(PointF point) => new((point.X * zoom + pan.X) + mid.Width, (point.Y * zoom + pan.Y) + mid.Height);
    private PointF Transform(float x, float y) => new((x * zoom + pan.X) + mid.Width, (y * zoom + pan.Y) + mid.Height);

    private AppSettings appSettings;

    public Form1()
    {
        InitializeComponent();
        this.pictureBox1.MouseDown += this.Form1_MouseDown;
        this.pictureBox1.MouseMove += this.Form1_MouseMove;
        this.pictureBox1.MouseUp += this.Form1_MouseUp;
        this.Load += this.Form1_Load;
        this.KeyPress += this.Form1_KeyPress;
        this.MouseWheel += OnMouseWheel;

        using var settingsFile = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("settings.data", FileMode.OpenOrCreate, FileAccess.Read);
        if (settingsFile.Length > 0)
        {
            using var reader = new BinaryReader(settingsFile);
            this.appSettings = new(reader.ReadString());
        }
        else
            this.appSettings = new(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

    }

    private void Form1_KeyPress(object? sender, KeyPressEventArgs e)
    {
        switch (e.KeyChar)
        {
            case ' ':
                this.graph.ForceAtlas2.Step();
                break;
            case 'g':
                this.drawGrid = !this.drawGrid;
                break;
            case 'r':
                this.drawRegions = !this.drawRegions;
                break;
            case 'n':
                this.drawNodes = !this.drawNodes;
                break;
            case 'e':
                this.drawEdges = !this.drawEdges;
                break;
            case 'x':
                this.drawRestrictions = !this.drawRestrictions;
                break;
        }
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        this.DoubleBuffered = true;

        var edges = this.graph.Edges;

        const int planckLength = 10;
        const int n = 5;
        const double scale = 1;
        const double expansion = 1.41;
        var θ = Math.Pow(planckLength / 2d / scale, 1 / expansion);
        Node? aNew = null, bNew = null;
        for (var i = 0; i < n; i++)
        {
            var radius = scale * Math.Pow(θ, expansion);
            var x = radius * Math.Cos(θ);
            var y = radius * Math.Sin(θ);
            θ += planckLength / radius;
            var aOld = aNew;
            var bOld = bNew;
            aNew = this.graph.AddNode(x, y, $"A{i}");
            bNew = this.graph.AddNode(-x, -y, $"B{i}");
            if (i > 0)
            {
                this.graph.AddEdge(aOld!, aNew, 1);
                this.graph.AddEdge(bOld!, bNew, 1);
            }
            else
            {
                // connect first two nodes
                this.graph.AddEdge(aNew, bNew, 1);
            }

        }
        graph.ForceAtlas2.IsStrongGravityMode = false;
        graph.ForceAtlas2.Gravity = 1.1;
        graph.ForceAtlas2.IsLogMode = false;
        graph.ForceAtlas2.Initialize();
        graph.ForceAtlas2.RegionStep();
        graph.ForceAtlas2.IsBarnesHutOptimize = true;
        graph.ForceAtlas2.IsMultiThread = false;

        this.Height = 1000;
        this.Width = 1000;

        Application.Idle += Application_Idle;
    }

    private void OnMouseWheel(object? s, MouseEventArgs e)
    {
        var lastZoom = this.zoom;
        this.zoom += e.Delta / 1200f;
        if (this.zoom < 0.1f) this.zoom = 0.1f;
        if (this.zoom > 10f) this.zoom = 10f;
        this.pan.X += (e.X - this.mid.Width) * (this.zoom - lastZoom);
        this.pan.Y += (e.Y - this.mid.Height) * (this.zoom - lastZoom);
    }

    private void Form1_MouseUp(object? sender, MouseEventArgs e)
    {
        if (this.selected != null && !wasSelectedFixed)
            this.selected.IsFixed = false;
        this.selected = null;
    }

    private PointF lastPos = new(0, 0);

    private void Form1_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            if (!this.isPanning)
                return;
            this.pan.X += e.X - lastPos.X;
            this.pan.Y += e.Y - lastPos.Y;
            lastPos = new(e.X, e.Y);
            return;
        }
        lastPos = new(e.X, e.Y);

        var mousePos = (PointF)e.Location;

        var m = Transform(mousePos - mid);
        if (this.selected == null)
        {
            // dumb, use spatial index from ForceAtlas2
            this.mouseNode = graph.Nodes.FirstOrDefault(n =>
            {
                var p = NodeToPointF(n);
                return Math.Abs(p.X - m.X) < 3 && Math.Abs(p.Y - m.Y) < 3;
            });
            return;
        }

        this.selected.X = m.X;
        this.selected.Y = m.Y;
        //this.graph.ForceAtlas2.RegionStep();
        
    }

    private Node? mouseNode;
    private bool wasSelectedFixed;
    private bool isPanning;

    private void Form1_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            this.isPanning = true;
            return;
        }
        this.selected = mouseNode;
        if (this.selected != null)
        {
            this.wasSelectedFixed = this.selected.IsFixed;
            this.selected.IsFixed = true;
        }
    }

    private bool drawGrid;
    private bool drawRegions;
    private bool drawNodes = true;
    private bool drawEdges = true;
    private bool drawRestrictions;

    private void Application_Idle(object? sender, EventArgs e)
    {
        using var g = this.pictureBox1.CreateGraphics();
        using var b = new Bitmap((int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height, g);
        using var bg = Graphics.FromImage(b);
        mid = g.VisibleClipBounds.Size * .5f;
        Queue<Region> regions = new();
        while (IsApplicationIdle())
        {
            bg.Clear(Color.Azure);

            if (this.drawGrid)
            {
                DrawLine(Pens.Gray, 0, -100, 0, 100);
                DrawLine(Pens.Gray, -100, 0, 100, 0);

                for (var i = 10; i < 100; i += 10)
                {
                    DrawLine(Pens.LightGray, i, -100, i, 100);
                    DrawLine(Pens.LightGray, -100, i, 100, i);
                    DrawLine(Pens.LightGray, -i, -100, -i, 100);
                    DrawLine(Pens.LightGray, -100, -i, 100, -i);
                }
            }

            if (this.drawRegions && graph.ForceAtlas2.RootNodeRegion != null)
            {

                regions.Enqueue(graph.ForceAtlas2.RootNodeRegion);
                while (regions.Count > 0)
                {
                    var region = regions.Dequeue();
                    var thisX = (float)(region.MassCenterX);
                    var thisY = (float)(region.MassCenterY);
                    var center = Transform(thisX, thisY);
                    var size = (float)region.Size * this.zoom;
                    bg.DrawEllipse(Pens.LightGray, center.X - size * .5f, center.Y - size * .5f, size, size);

                    ProcessSubregion(region.TopLeft);
                    ProcessSubregion(region.TopRight);
                    ProcessSubregion(region.BottomLeft);
                    ProcessSubregion(region.BottomRight);

                    continue;

                    void ProcessSubregion(Region? r)
                    {
                        if (r is not { Count: > 0 })
                            return;
                        var rx = (float)(r.MassCenterX);
                        var ry = (float)(r.MassCenterY);
                        DrawLine(Pens.LightPink, thisX, thisY, rx, ry);
                        regions.Enqueue(r);
                    }
                }
            }

            if (this.drawRestrictions)
                foreach (var graphNode in graph.Nodes)
                {
                    var p = NodeToPointOldF(graphNode);
                    for (var i = 0; i < 8; i++)
                    {
                        var r = (float)graphNode.Restrictions[i];
                        if (double.IsInfinity(r) || Math.Abs(r) < 0.0001)
                            continue;
                        bg.DrawPie(Pens.BurlyWood, p.X - r, p.Y - r, 2 * r, 2 * r, -i * 45, -45);
                    }
                }

            if (this.drawEdges)
                foreach (var graphEdge in graph.Edges)
                    bg.DrawLine(Pens.Black, NodeToPointF(graphEdge.Source), NodeToPointF(graphEdge.Target));

            if (this.drawNodes)
                foreach (var graphNode in graph.Nodes)
                {
                    var p = NodeToPointF(graphNode);
                    //g.FillRectangle(Brushes.Red, p.X, p.Y, 1, 1);
                    // draw a 3x3 cross
                    bg.DrawLine(Pens.Red, p.X - 3, p.Y - 3, p.X + 3, p.Y + 3);
                    bg.DrawLine(Pens.Red, p.X - 3, p.Y + 3, p.X + 3, p.Y - 3);
                }

            if (mouseNode != null)
            {
                var p = NodeToPointF(mouseNode);
                bg.FillRectangle(mouseNode == selected ? Brushes.Blue : Brushes.Red, p.X - 3, p.Y - 3, 6, 6);
            }

            bg.DrawString($"Pan: {pan.X:N2}, {pan.Y:N2} | Zoom: {zoom:N2} | LastPos: {lastPos.X:N2}, {lastPos.Y:N2} | Mid: {mid.Width:N2}, {mid.Height:N2}",
                this.Font, Brushes.Black, 10, 10);

            g.DrawImageUnscaled(b, 0, 0);
            graph.ForceAtlas2.Step();
        }
        return;
        void DrawLine(Pen pen, float x1, float y1, float x2, float y2) => bg.DrawLine(pen, Transform(x1, y1), Transform(x2, y2));
    }

    private PointF NodeToPointF(Node node) => Transform(new((float)node.X, (float)node.Y));
    private PointF NodeToPointOldF(Node node) => Transform(new((float)node.OldX, (float)node.OldY));
    private bool IsApplicationIdle() => PeekMessage(out _, IntPtr.Zero, 0, 0, 0) == 0;

    private void saveButton_Click(object sender, EventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Ripple Graph|*.ripple",
            Title = "Save Ripple Graph",
            FileName = Path.Combine(this.appSettings.LastAccessedPath, "graph.ripple")
        };

        if (saveFileDialog.ShowDialog() != DialogResult.OK)
            return;
        using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fileStream);
        this.graph.Write(writer);
        this.SaveLastAccessedPath(Path.GetDirectoryName(saveFileDialog.FileName) ?? string.Empty);
    }

    private void loadButton_Click(object sender, EventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Ripple Graph|*.ripple",
            Title = "Load Ripple Graph",
            FileName = Path.Combine(this.appSettings.LastAccessedPath, "graph.ripple")
        };
        if (openFileDialog.ShowDialog() != DialogResult.OK)
            return;
        using var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);
        this.graph = Graph.Read(reader);
        this.SaveLastAccessedPath(Path.GetDirectoryName(openFileDialog.FileName) ?? string.Empty);
    }

    private void SaveLastAccessedPath(string path)
    {
        this.appSettings.LastAccessedPath = path;
        using var settingsFile = IsolatedStorageFile.GetUserStoreForAssembly().OpenFile("settings.data", FileMode.Create, FileAccess.Write);
        using var settingsWriter = new BinaryWriter(settingsFile);
        settingsWriter.Write(this.appSettings.LastAccessedPath);
    }
}
