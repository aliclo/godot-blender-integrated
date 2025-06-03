using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class EdgifyNode : PipelineNode, IReceivePipe
{

    private partial class EdgifyNodeStore : GodotObject
    {

        [Export]
        public double ThicknessX { get; set; }

        [Export]
        public double ThicknessY { get; set; }

        [Export]
        public double SharpnessThreshold { get; set; }

        [Export]
        public double MinYAngle { get; set; }

        [Export]
        public double MaxYAngle { get; set; }

        [Export]
        public double MinY { get; set; }

        [Export]
        public double MaxY { get; set; }

    }

    private class MeshEdge
    {
        public Vector3 StartVertex { get; set; }
        public Vector3 EndVertex { get; set; }
        public Vector3 FirstFaceVertex { get; set; }
        public Vector3? SecondFaceVertex { get; set; }
    }

    private const string EDGEIFY_NAME = "Edges";

    // TODO: Use this from the context instead of having to provide it to the context
    private static readonly List<string> TOUCHED_PROPERTIES = new List<string>() {
        nameof(MeshInstance3D.Mesh).ToLower()
    };

    private EdgifyNodeStore _edgifyNodeStore;
    private PipeContext _context;

    private NumericLineEdit _thicknessXLineEdit;
    private NumericLineEdit _thicknessYLineEdit;
    private NumericLineEdit _sharpnessThresholdLineEdit;
    private NumericLineEdit _minYAngleLineEdit;
    private NumericLineEdit _maxYAngleLineEdit;
    private NumericLineEdit _minYLineEdit;
    private NumericLineEdit _maxYLineEdit;
    private MeshInstance3D _meshInstance3D;
    private string _nodeName;

    private double _thicknessX = 0.5f;
    private double _thicknessY = 0.5f;
    private double _sharpnessThreshold = 30;
    private double _minYAngle = 0;
    private double _maxYAngle = 360;
    private double _minY = -1000;
    private double _maxY = 1000;

    public List<IReceivePipe> NextPipes { get; set; } = new List<IReceivePipe>();
    private List<List<IReceivePipe>> _nodeConnections;
    public override List<List<IReceivePipe>> NodeConnections => _nodeConnections;

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new EdgifyNodeStore()
        {
            ThicknessX = double.Parse(_thicknessXLineEdit.Text),
            ThicknessY = double.Parse(_thicknessYLineEdit.Text),
            SharpnessThreshold = double.Parse(_sharpnessThresholdLineEdit.Text),
            MinYAngle = double.Parse(_minYAngleLineEdit.Text),
            MaxYAngle = double.Parse(_maxYAngleLineEdit.Text),
            MinY = double.Parse(_minYLineEdit.Text),
            MaxY = double.Parse(_maxYLineEdit.Text)
        });
    }

    public override void Load(Variant data)
    {
        _edgifyNodeStore = GodotJsonParser.FromJsonType<EdgifyNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;

        _nodeConnections = Enumerable.Range(0, 1)
            .Select(n => new List<IReceivePipe>())
            .ToList();

        var meshNodeContainer = new HBoxContainer();
        AddChild(meshNodeContainer);

        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0, (int)PipelineNodeTypes.Mesh);
        SetSlotColorLeft(0, TypeConnectorColors.MESH);
        var inputMeshLabel = new Label();
        inputMeshLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        inputMeshLabel.HorizontalAlignment = HorizontalAlignment.Left;
        inputMeshLabel.Text = "Mesh";
        meshNodeContainer.AddChild(inputMeshLabel);

        SetSlotEnabledRight(0, true);
        SetSlotTypeRight(0, (int)PipelineNodeTypes.Mesh);
        SetSlotColorRight(0, TypeConnectorColors.MESH);
        var outputMeshLabel = new Label();
        outputMeshLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        outputMeshLabel.HorizontalAlignment = HorizontalAlignment.Right;
        outputMeshLabel.Text = "Mesh";
        meshNodeContainer.AddChild(outputMeshLabel);

        _thicknessXLineEdit = CreateThicknessXControl();
        _thicknessYLineEdit = CreateThicknessYControl();
        _sharpnessThresholdLineEdit = CreateSharpnessThresholdControl();
        _minYAngleLineEdit = CreateMinYAngleControl();
        _maxYAngleLineEdit = CreateMaxYAngleControl();
        _minYLineEdit = CreateMinYControl();
        _maxYLineEdit = CreateMaxYControl();

        if (_edgifyNodeStore != null)
        {
            _thicknessXLineEdit.Number = _edgifyNodeStore.ThicknessX;
            _thicknessYLineEdit.Number = _edgifyNodeStore.ThicknessY;
            _sharpnessThresholdLineEdit.Number = _edgifyNodeStore.SharpnessThreshold;
            _minYAngleLineEdit.Number = _edgifyNodeStore.MinYAngle;
            _maxYAngleLineEdit.Number = _edgifyNodeStore.MaxYAngle;
            _minYLineEdit.Number = _edgifyNodeStore.MinY;
            _maxYLineEdit.Number = _edgifyNodeStore.MaxY;
        }
        else
        {
            _thicknessXLineEdit.Number = _thicknessX;
            _thicknessYLineEdit.Number = _thicknessY;
            _sharpnessThresholdLineEdit.Number = _sharpnessThreshold;
            _minYAngleLineEdit.Number = _minYAngle;
            _maxYAngleLineEdit.Number = _maxYAngle;
            _minYLineEdit.Number = _minY;
            _maxYLineEdit.Number = _maxY;
        }
    }

    public override void _Ready()
    {

    }

    public void Register()
    {
        foreach (var pipe in NextPipes)
        {
            pipe.Register();
        }
    }

    public void PreRegister(string nodeName)
    {
        _nodeName = $"{nodeName}-{EDGEIFY_NAME}";

        foreach (var pipe in NextPipes)
        {
            pipe.PreRegister(_nodeName);
        }
    }

    public PipeValue Pipe(PipeValue pipeValue)
    {
        var obj = pipeValue.Value;

        if (obj is not MeshInstance3D)
        {
            return null;
        }

        var meshInstance = (MeshInstance3D)obj;

        float sharpnessThresholdRad = (float)(_sharpnessThreshold * Math.PI / 180);
        float minYAngleRad = (float)(_minYAngle * Math.PI / 180 - Math.PI);
        float maxYAngleRad = (float)(_maxYAngle * Math.PI / 180 - Math.PI);

        // Get face vertices and transform according to node position
        var faces = meshInstance.Mesh.GetFaces();

        int numFaces = faces.Length / 3;

        // Another way to get edges is with MeshDataTool, however this seemed to also need to be done by faces as some edges would return 1 connected face even if there were 2
        // With triangulated meshes an edge can only have up to 2 faces connected
        var edges = new System.Collections.Generic.Dictionary<Vector3, MeshEdge>();

        for (int fi = 0; fi < numFaces; fi++)
        {
            var v1 = faces[fi * 3];
            var v2 = faces[fi * 3 + 1];
            var v3 = faces[fi * 3 + 2];

            var e1Pos = (v1 + v2) / 2;

            var exists = edges.TryGetValue(e1Pos, out var e1);

            if (!exists)
            {
                e1 = new MeshEdge()
                {
                    StartVertex = v1,
                    EndVertex = v2,
                    FirstFaceVertex = v3,
                    SecondFaceVertex = null
                };

                edges[e1Pos] = e1;
            }
            else
            {
                e1.SecondFaceVertex = v3;
            }

            var e2Pos = (v1 + v3) / 2;

            exists = edges.TryGetValue(e2Pos, out var e2);

            if (!exists)
            {
                e2 = new MeshEdge()
                {
                    StartVertex = v1,
                    EndVertex = v3,
                    FirstFaceVertex = v2,
                    SecondFaceVertex = null
                };

                edges[e2Pos] = e2;
            }
            else
            {
                e2.SecondFaceVertex = v2;
            }

            var e3Pos = (v2 + v3) / 2;

            exists = edges.TryGetValue(e3Pos, out var e3);

            if (!exists)
            {
                e3 = new MeshEdge()
                {
                    StartVertex = v2,
                    EndVertex = v3,
                    FirstFaceVertex = v1,
                    SecondFaceVertex = null
                };

                edges[e3Pos] = e3;
            }
            else
            {
                e3.SecondFaceVertex = v1;
            }
        }

        var sharpEdges = edges.Values.Where(edge =>
        {
            var center = (edge.StartVertex + edge.EndVertex) / 2;
            if (center.Y < _minY || center.Y > _maxY)
            {
                return false;
            }

            // No face means it's super sharp!
            if (edge.SecondFaceVertex == null)
            {
                return true;
            }

            // Rotate so that the edge is along the Z axis (Y in Blender), this allows us to find the angle with only X and Y (Z in Blender) axis
            var direction = edge.EndVertex - edge.StartVertex;
            var xRot = Math.Atan2(direction.Y, direction.Z);
            direction = direction.Rotated(new Vector3(1, 0, 0), (float)xRot);
            var yRot = Math.Atan2(direction.X, direction.Z);

            if (yRot < minYAngleRad || yRot > maxYAngleRad)
            {
                return false;
            }

            // var basis = new Basis(new Vector3(0, 1, 0), (float) -yRot);
            // basis.Rotated(new Vector3(1, 0, 0), (float) -xRot);

            // Make everything relative to StartVertex and then rotate
            var e1 = edge.FirstFaceVertex - edge.StartVertex;
            var e2 = edge.SecondFaceVertex.Value - edge.StartVertex;

            // Rotate +X and -Y to align edge with Z
            e1 = e1.Rotated(new Vector3(1, 0, 0), (float)xRot).Rotated(new Vector3(0, 1, 0), (float)-yRot);
            e2 = e2.Rotated(new Vector3(1, 0, 0), (float)xRot).Rotated(new Vector3(0, 1, 0), (float)-yRot);

            // Only X and Y components needed after rotation, Z tells nothing about sharpness at this point
            var e1xy = new Vector2(e1.X, e1.Y);
            var e2xy = new Vector2(e2.X, e2.Y);
            var angle = Math.Abs(e1xy.AngleTo(e2xy));

            return angle > sharpnessThresholdRad && angle < (Math.PI - sharpnessThresholdRad);
        }).ToList();

        var edgeMeshsVertices = sharpEdges.Select(sharpEdge =>
        {
            var direction = sharpEdge.StartVertex.DirectionTo(sharpEdge.EndVertex);
            var xRot = Math.Atan2(direction.Y, direction.Z);
            direction = direction.Rotated(new Vector3(1, 0, 0), (float)-xRot);
            var yRot = Math.Atan2(direction.X, direction.Z);

            var topLeft = new Vector3((float)(-1 * _thicknessX), (float)(-1 * _thicknessX), 0).Rotated(new Vector3(0, 1, 0), (float)yRot).Rotated(new Vector3(1, 0, 0), (float)xRot);
            var topRight = new Vector3((float)(1 * _thicknessX), (float)(-1 * _thicknessX), 0).Rotated(new Vector3(0, 1, 0), (float)yRot).Rotated(new Vector3(1, 0, 0), (float)xRot);
            var bottomLeft = new Vector3((float)(-1 * _thicknessX), (float)(1 * _thicknessX), 0).Rotated(new Vector3(0, 1, 0), (float)yRot).Rotated(new Vector3(1, 0, 0), (float)xRot);
            var bottomRight = new Vector3((float)(1 * _thicknessX), (float)(1 * _thicknessX), 0).Rotated(new Vector3(0, 1, 0), (float)yRot).Rotated(new Vector3(1, 0, 0), (float)xRot);

            var vertices = new List<Vector3>
            {
                sharpEdge.StartVertex + topLeft,
                sharpEdge.EndVertex + topLeft,
                sharpEdge.StartVertex + topRight,
                sharpEdge.EndVertex + topRight,
                sharpEdge.StartVertex + bottomRight,
                sharpEdge.EndVertex + bottomRight,
                sharpEdge.StartVertex + bottomLeft,
                sharpEdge.EndVertex + bottomLeft,
                sharpEdge.StartVertex + topLeft,
                sharpEdge.EndVertex + topLeft
            };

            return vertices;
        });


        var triangleStrips = new List<Vector3>();


        var edgeMeshVerticesEnumerator = edgeMeshsVertices.GetEnumerator();

        edgeMeshVerticesEnumerator.MoveNext();
        var edgeMeshVertices = edgeMeshVerticesEnumerator.Current;
        edgeMeshVerticesEnumerator.MoveNext();
        var nextEdgeMeshVertex = edgeMeshVerticesEnumerator.Current;

        while (nextEdgeMeshVertex != null)
        {
            triangleStrips.AddRange(edgeMeshVertices);

            var lastVertex = edgeMeshVertices.Last();
            var nextVertex = nextEdgeMeshVertex.First();

            triangleStrips.Add(lastVertex);
            triangleStrips.Add(nextVertex);

            edgeMeshVertices = nextEdgeMeshVertex;
            edgeMeshVerticesEnumerator.MoveNext();
            nextEdgeMeshVertex = edgeMeshVerticesEnumerator.Current;
        }

        triangleStrips.AddRange(edgeMeshVertices);


        var vertices = triangleStrips.ToArray();

        // Initialize the ArrayMesh.
        var arrMesh = new ArrayMesh();
        Godot.Collections.Array arrays = new Godot.Collections.Array { };
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;

        // Create the Mesh.
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, arrays);
        var edgeMeshInstance = new MeshInstance3D();
        edgeMeshInstance.Mesh = arrMesh;
        edgeMeshInstance.Name = _nodeName;

        _meshInstance3D = edgeMeshInstance;

        return new PipeValue()
        {
            Value = _meshInstance3D,
            TouchedProperties = TOUCHED_PROPERTIES
        };
    }

    public void Clean()
    {
        _meshInstance3D = null;
        foreach (var pipe in NextPipes)
        {
            pipe.Clean();
        }
    }

    public void PipeDisconnect()
    {
        Clean();
    }

    private void ThicknessXChanged(double number)
    {
        _thicknessX = number;
        _context?.Reprocess();
    }

    private void ThicknessYChanged(double number)
    {
        _thicknessY = number;
        _context?.Reprocess();
    }

    private void SharpnessThresholdChanged(double number)
    {
        _sharpnessThreshold = number;
        _context?.Reprocess();
    }

    private void MinYAngleChanged(double number)
    {
        _minYAngle = number;
        _context?.Reprocess();
    }

    private void MaxYAngleChanged(double number)
    {
        _maxYAngle = number;
        _context?.Reprocess();
    }

    private void MinYChanged(double number)
    {
        _minY = number;
        _context?.Reprocess();
    }

    private void MaxYChanged(double number)
    {
        _maxY = number;
        _context?.Reprocess();
    }

    public override void AddConnection(int index, List<IReceivePipe> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);
        NextPipes.AddRange(receivePipes);
    }

    public override void Connect(int index, List<IReceivePipe> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);
        NextPipes.AddRange(receivePipes);

        var destinationHelper = new DestinationHelper();
        var pipeValue = new PipeValue() { Value = _meshInstance3D, TouchedProperties = TOUCHED_PROPERTIES };
        destinationHelper.AddReceivePipes(_context, _nodeName, receivePipes, _meshInstance3D == null ? null : new CloneablePipeValue() { PipeValue = pipeValue });
    }

    public override void Disconnect(int index, List<IReceivePipe> receivePipes)
    {
        _nodeConnections[index].RemoveAll(rp => receivePipes.Contains(rp));
        NextPipes.RemoveAll(p => receivePipes.Contains(p));

        var destinationHelper = new DestinationHelper();
        destinationHelper.RemoveReceivePipes(receivePipes);
    }

    public NumericLineEdit CreateThicknessXControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var label = new Label();
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.Text = "Thickness X";
        container.AddChild(label);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill; lineEdit.Text = "";
        lineEdit.NumberChanged += ThicknessXChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }

    public NumericLineEdit CreateThicknessYControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var label = new Label();
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.Text = "Thickness Y";
        container.AddChild(label);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.NumberChanged += ThicknessYChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }

    public NumericLineEdit CreateSharpnessThresholdControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var label = new Label();
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.Text = "Threshold";
        container.AddChild(label);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.NumberChanged += SharpnessThresholdChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }

    public NumericLineEdit CreateMinYAngleControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var minYAngleLabel = new Label();
        minYAngleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        minYAngleLabel.Text = "Min Y Angle";
        container.AddChild(minYAngleLabel);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.NumberChanged += MinYAngleChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }

    public NumericLineEdit CreateMaxYAngleControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var maxYAngleLabel = new Label();
        maxYAngleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        maxYAngleLabel.Text = "Max Y Angle";
        container.AddChild(maxYAngleLabel);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.NumberChanged += MaxYAngleChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }

    public NumericLineEdit CreateMinYControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var minYLabel = new Label();
        minYLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        minYLabel.Text = "Min Y";
        container.AddChild(minYLabel);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.NumberChanged += MinYChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }

    public NumericLineEdit CreateMaxYControl()
    {
        var container = new HBoxContainer();
        AddChild(container);

        var maxYLabel = new Label();
        maxYLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        maxYLabel.Text = "Max Y";
        container.AddChild(maxYLabel);

        var lineEdit = new NumericLineEdit();
        lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lineEdit.NumberChanged += MaxYChanged;
        container.AddChild(lineEdit);

        return lineEdit;
    }


}
