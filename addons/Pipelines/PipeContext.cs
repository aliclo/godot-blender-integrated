using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

#if TOOLS
[Tool]
public partial class PipeContext : Node
{

    private class NodePipes
    {
        public IReceivePipe CurrentNodePipe => Pipes[CurrentProgress];
        public PipeValue CurrentValue { get; set; }
        public List<IReceivePipe> Pipes { get; set; }
        public int CurrentProgress { get; set; } = 0;
    }

    private class NodeDependency
    {
        public PipelineNode Node { get; set; }
        public IReceivePipe Dependency { get; set; }
    }

    public Node RootNode => this;
    public List<OutputNode> OutputNodes { get; set; }
    public System.Collections.Generic.Dictionary<string, PipelineNode> PipelineNodeDict => _pipelineNodeDict;

    private readonly PipelineAccess _pipelineAccess = new PipelineAccess();
    private readonly PipeMapper _pipeMapper = new PipeMapper();
    private System.Collections.Generic.Dictionary<string, PipelineNode> _pipelineNodeDict = new System.Collections.Generic.Dictionary<string, PipelineNode>();
    private List<NodePipes> _nodePipesList;
    private List<NodeDependency> _nodeDependencies;
    private bool _completedFirstImport = false;

    public override void _Ready()
    {
        if (OS.HasFeature("editor_runtime"))
        {
            return;
        }

        var sceneFilePath = GetTree().EditedSceneRoot.SceneFilePath;
        var pipelineContextStores = _pipelineAccess.Read(sceneFilePath);
        var pipelineContextStore = pipelineContextStores?.SingleOrDefault(pcs => pcs.Name == Name);
        if (pipelineContextStore != null)
        {
            AddNodesAndConnections(pipelineContextStore);
        }

        if (Owner != null)
        {
            if (Owner.IsNodeReady())
            {
                Process();
                _completedFirstImport = true;
            }
            else
            {
                Owner.Ready += () =>
                {
                    Process();
                    _completedFirstImport = true;
                };
            }
        }
    }

    public override void _EnterTree()
    {
        if (Owner == null)
        {
            _completedFirstImport = true;
        }
    }


    public void AddNodesAndConnections(PipelineContextStore pipelineContextStore)
    {
        foreach (var pipelineNodeStore in pipelineContextStore.Nodes)
        {
            var nodeResourcePath = $"res://addons/Pipelines/Nodes/{pipelineNodeStore.Type}.tscn";
            var pipelineNode = GD.Load<PackedScene>(nodeResourcePath).Instantiate<PipelineNode>();

            pipelineNode.Name = pipelineNodeStore.Name;
            pipelineNode.Load(pipelineNodeStore.Data);
            pipelineNode.Init(this);
            pipelineNode.PositionOffset = new Vector2(pipelineNodeStore.X, pipelineNodeStore.Y);
            _pipelineNodeDict[pipelineNode.Name] = pipelineNode;
        }

        foreach (var pipelineConnection in pipelineContextStore.Connections)
        {
            var inputNode = _pipelineNodeDict[pipelineConnection.FromNodeName];
            var toNode = _pipelineNodeDict[pipelineConnection.ToNodeName];

            if (toNode is not IReceivePipe receiveNode)
            {
                GD.PrintErr("Node ", toNode.Name, " is not a receive node and cannot be connected to ", inputNode.Name);
                continue;
            }

            inputNode.AddConnection(pipelineConnection.FromPort, new List<IReceivePipe>() { receiveNode });
        }
    }

    public Variant GetData()
    {
        var nodes = _pipelineNodeDict.Values.Select(_pipeMapper.Map);

        // Eventually we can support receiver nodes with multiple input ports
        var nodeConnections = _pipelineNodeDict.Values.Select(n => n.NodeConnections.Select((pc, pi) => pc.Select(c => new PipelineConnectionStore()
        {
            FromNodeName = n.Name,
            FromPort = pi,
            ToNodeName = c.Name,
            ToPort = 0
        })))
        .SelectMany(nc => nc)
        .SelectMany(pc => pc);

        var pipelineContextStore = new PipelineContextStore()
        {
            Name = Name,
            Nodes = new Array<PipelineNodeStore>(nodes),
            Connections = new Array<PipelineConnectionStore>(nodeConnections)
        };

        return pipelineContextStore;
    }

    public void Reprocess()
    {
        if (_completedFirstImport)
        {
            Process();
        }
    }

    public void RegisterPipe(ValuePipe valuePipe)
    {
        _nodePipesList.AddRange(GetNodePipes(valuePipe));
    }

    public void ReprocessPipe(List<ValuePipe> valuePipes)
    {
        foreach (var valuePipe in valuePipes)
        {
            valuePipe.Pipe.Register();
        }

        var nodePipes = new List<NodePipes>();

        foreach (var valuePipe in valuePipes)
        {
            nodePipes.AddRange(GetNodePipes(valuePipe));
        }

        var nodePipesOrdering = OrderEvaluation(nodePipes);
        EvaluateNodePipes(nodePipesOrdering);
    }

    private IEnumerable<NodePipes> GetNodePipes(ValuePipe valuePipe)
    {
        var pipe = valuePipe.Pipe;
        var cloneablePipeValue = valuePipe.CloneablePipeValue;

        var processedPipes = new List<List<IReceivePipe>>();
        var nodePipes = new List<List<IReceivePipe>>() { new List<IReceivePipe>() { pipe } };
        List<List<IReceivePipe>> nodePipesWithNext;

        do
        {
            nodePipesWithNext = nodePipes.Where(np => np.Last().NextPipes != null && np.Last().NextPipes.Any()).ToList();
            var nodePipesWithoutNext = nodePipes.Except(nodePipesWithNext);
            processedPipes.AddRange(nodePipesWithoutNext);

            var newNodePipes = new List<List<IReceivePipe>>();
            foreach (var nodePipe in nodePipesWithNext)
            {
                var lastPipe = nodePipe.Last();
                newNodePipes.AddRange(lastPipe.NextPipes.Select(np =>
                {
                    var clonedNodePipe = new List<IReceivePipe>(nodePipe);
                    clonedNodePipe.Add(np);
                    return clonedNodePipe;
                }));
            }
            nodePipes = newNodePipes;
        } while (nodePipesWithNext.Any());

        return processedPipes.Select(p => new NodePipes()
        {
            CurrentValue = cloneablePipeValue.ClonePipeValue(),
            Pipes = p,
            CurrentProgress = 0
        });
    }

    private List<NodePipes> OrderEvaluation(List<NodePipes> nodePipesList)
    {
        var nodePipesOrdering = new List<NodePipes>();
        var evaluateNodePipes = new List<NodePipes>(nodePipesList);
        var processedPipes = new List<IReceivePipe>();

        while (evaluateNodePipes.Any())
        {
            var pipesToProcess = evaluateNodePipes
                .Where(p => !_nodeDependencies.Any(nd => p.CurrentNodePipe == nd.Node && !processedPipes.Contains(nd.Dependency)));

            foreach (var p in pipesToProcess)
            {
                nodePipesOrdering.Add(p);
                processedPipes.Add(p.CurrentNodePipe);
                p.CurrentProgress++;
            }

            evaluateNodePipes.RemoveAll(p => p.CurrentProgress == p.Pipes.Count);
        }

        return nodePipesOrdering;
    }

    private void EvaluateNodePipes(List<NodePipes> nodePipesOrdering)
    {
        foreach (var p in nodePipesOrdering.Reverse<NodePipes>())
        {
            p.CurrentProgress--;
            p.CurrentNodePipe.Clean();
        }

        var nodePipesToProcess = nodePipesOrdering.Where(npo => npo.CurrentValue != null);
        foreach (var p in nodePipesToProcess)
        {
            p.CurrentValue = p.CurrentNodePipe.Pipe(p.CurrentValue);
            p.CurrentProgress++;
        }
    }

    private void Process()
    {
        OutputNodes = _pipelineNodeDict.Values
            .Where(n => n is OutputNode)
            .Select(n => (OutputNode)n)
            .ToList();

        var inputPipes = _pipelineNodeDict.Values
            .Where(n => n is IInputPipe)
            .Select(n => (IInputPipe)n);

        _nodePipesList = new List<NodePipes>();

        foreach (var inputPipe in inputPipes)
        {
            inputPipe.Register();
        }

        foreach (var pipeList in _nodePipesList)
        {
            foreach (var pipe in pipeList.Pipes)
            {
                pipe.Register();
            }
        }
        
        _nodeDependencies = _pipelineNodeDict.Values
            .SelectMany(pn => pn.NodeDependencies.Select(nd => new { Node = pn, NodeDependencyPath = nd }))
            .Select(pn => new NodeDependency { Node = pn.Node, Dependency = OutputNodes.SingleOrDefault(on => on.AbsoluteDestinationIncludingNode == pn.NodeDependencyPath) })
            .Where(pn => pn.Dependency != null)
            .ToList();

        var nodePipesOrdering = OrderEvaluation(_nodePipesList);

        EvaluateNodePipes(nodePipesOrdering);
    }

    protected override void Dispose(bool disposing)
    {
        var allPipes = _pipelineNodeDict.Values;

        foreach (var pipe in allPipes)
        {
            pipe.DisposePipe();
        }

        base.Dispose(disposing);
    }

}
#endif