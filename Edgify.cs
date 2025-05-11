using Godot;
using Godot.Collections;
using System;
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

            var destinationPipesPathsToRemove = previousDestinationPaths.Except(value);
            var destinationPipesToRemove = destinationPipesPathsToRemove.Select(p => GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);

            foreach(var destinationPipeToRemove in destinationPipesToRemove) {
                destinationPipeToRemove.PipeDisconnect();
            }

            if(!IsNodeReady()) {
                return;
            }

            NextPipes = _destinations
                .Select(d => GetNodeOrNull<IReceivePipe>(d))
                .Where(p => p != null).ToList();

            if(_context != null) {
                var destinationPipesPathsToAdd = value.Except(previousDestinationPaths);
                var destinationPipesToAdd = destinationPipesPathsToAdd.Select(p => GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);

                foreach(var destinationPipeToAdd in destinationPipesToAdd) {
                    destinationPipeToAdd.Register(_context, _nodeName);
                    if(_meshInstance3D != null) {
                        destinationPipeToAdd.Init();
                        var mesh = _meshInstance3D.Duplicate();
                        destinationPipeToAdd.Pipe(mesh);
                    }
                }
            }
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