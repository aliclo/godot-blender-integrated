using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Edgify : Node, IReceivePipe
{

    private const string EDGEIFY_NAME = "Edges";

    [Export]
    public Array<NodePath> Destination {
        get {
            return _destinations;
        }
        set {
            var previousDestinationPaths = _destinations ?? new Array<NodePath>();
            _destinations = value ?? new Array<NodePath>();

            var destinationHelper = new DestinationHelper();
            destinationHelper.HandleDestinationChange(new DestinationPropertyInfo() {
                PipeContext = _context,
                Node = this,
                DestinationNodeName = _nodeName,
                PreviousDestinationPaths = previousDestinationPaths,
                NewDestinationPaths = _destinations,
                CloneableValue = _meshInstance3D == null ? null : new CloneableNode() { Node = _meshInstance3D }
            });

            if(!IsNodeReady()) {
                return;
            }

            NextPipes = _destinations
                .Select(d => GetNodeOrNull<IReceivePipe>(d))
                .Where(p => p != null).ToList();
        }
    }
    public List<IReceivePipe> NextPipes { get; set; } = new List<IReceivePipe>();
    private PipeContext _context;
    private Array<NodePath> _destinations;
    private MeshInstance3D _meshInstance3D;
    private string _nodeName;

    public void Register(PipeContext context, string nodeName)
    {
        _context = context;
        _nodeName = $"{nodeName}-{EDGEIFY_NAME}";
        NextPipes = _destinations
                .Select(d => GetNodeOrNull<IReceivePipe>(d))
                .Where(p => p != null).ToList();

        foreach(var pipe in NextPipes) {
            pipe.Register(context, _nodeName);
        }
    }

    public void Init()
    {
        foreach(var pipe in NextPipes) {
            pipe.Init();
        }
    }

    public object Pipe(object obj)
    {
        if(obj is not MeshInstance3D) {
            return null;
        }

        var meshInstance = (MeshInstance3D) obj;
        _meshInstance3D = meshInstance;

        _meshInstance3D.Name = _nodeName;

        return meshInstance.Duplicate();
    }

    public void Clean()
    {
        _meshInstance3D = null;
        foreach(var pipe in NextPipes) {
            pipe.Clean();
        }
    }

    public void PipeDisconnect()
    {
        Clean();
    }

}