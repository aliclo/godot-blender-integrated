using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

#if TOOLS
[Tool]
public partial class PipeContext : Node
{

    public Node RootNode => this;
    public Array<OutputNode> OutputNodes { get; set; }
    public Godot.Collections.Dictionary<string, PipelineNode> PipelineNodeDict => _pipelineNodeDict;

    private readonly PipelineAccess _pipelineAccess = new PipelineAccess();
    private readonly PipeMapper _pipeMapper = new PipeMapper();
    private Godot.Collections.Dictionary<string, PipelineNode> _pipelineNodeDict;
    private Array<NodePipes> _nodePipesList;
    private Array<NodeDependency> _nodeDependencies;
    private bool _completedFirstImport = false;

    public override void _Ready()
    {
        if (OS.HasFeature("editor_runtime"))
        {
            return;
        }

        _pipelineNodeDict = new Godot.Collections.Dictionary<string, PipelineNode>();

        Pipelines.Instance.RegisterContext(this);

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
                Owner.Ready += OnOwnerReady;
            }
        }
    }

    private void OnOwnerReady()
    {
        Owner.Ready -= OnOwnerReady;
        Process();
        _completedFirstImport = true;
    }

    public override void _EnterTree()
    {
        if (OS.HasFeature("editor_runtime"))
        {
            return;
        }

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

            inputNode.AddConnection(pipelineConnection.FromPort, new Array<PipelineNode>() { toNode });
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

    public void ReprocessPipe(Array<ValuePipe> valuePipes)
    {
        foreach (var valuePipe in valuePipes)
        {
            valuePipe.Pipe.Register();
        }

        var nodePipes = new Array<NodePipes>();

        foreach (var valuePipe in valuePipes)
        {
            nodePipes.AddRange(GetNodePipes(valuePipe));
        }

        var nodePipesOrdering = OrderEvaluation(nodePipes);
        EvaluateNodePipes(nodePipesOrdering);

        // TODO: This gets annoying, we only need this when an import has changed
        EditorInterface.Singleton.SaveScene();
    }

    private IEnumerable<NodePipes> GetNodePipes(ValuePipe valuePipe)
    {
        var pipe = valuePipe.Pipe;
        var cloneablePipeValue = valuePipe.CloneablePipeValue;

        var processedPipes = new Array<Array<PipelineNode>>();
        var nodePipes = new Array<Array<PipelineNode>>() { new Array<PipelineNode>() { pipe } };
        Array<Array<PipelineNode>> nodePipesWithNext;

        do
        {
            nodePipesWithNext = new Array<Array<PipelineNode>>(nodePipes.Where(np => np.Last().NextPipes != null && np.Last().NextPipes.Any()));
            var nodePipesWithoutNext = nodePipes.Except(nodePipesWithNext);
            processedPipes.AddRange(nodePipesWithoutNext);

            var newNodePipes = new Array<Array<PipelineNode>>();
            foreach (var nodePipe in nodePipesWithNext)
            {
                var lastPipe = nodePipe.Last();
                newNodePipes.AddRange(lastPipe.NextPipes.Select(np =>
                {
                    var clonedNodePipe = new Array<PipelineNode>(nodePipe);
                    clonedNodePipe.Add(np);
                    return clonedNodePipe;
                }));
            }
            nodePipes = newNodePipes;
        } while (nodePipesWithNext.Any());

        return processedPipes.Select(p => new NodePipes()
        {
            CurrentValue = cloneablePipeValue,
            Pipes = p,
            CurrentProgress = 0
        });
    }

    private Array<NodePipes> OrderEvaluation(Array<NodePipes> nodePipesList)
    {
        var nodePipesOrdering = new Array<NodePipes>();
        var evaluateNodePipes = new List<NodePipes>(nodePipesList);
        var nodesToBeProcessed = nodePipesList
            .SelectMany(np => np.Pipes)
            .Distinct();

        var processedPipes = new Array<PipelineNode>();

        while (evaluateNodePipes.Any())
        {
            var pipesToProcess = evaluateNodePipes
                .Where(p => !_nodeDependencies.Any(nd => p.CurrentNodePipe == nd.Node && nodesToBeProcessed.Contains(nd.Dependency) && !processedPipes.Contains(nd.Dependency)));

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

    private void EvaluateNodePipes(Array<NodePipes> nodePipesOrdering)
    {
        foreach (var p in nodePipesOrdering.Reverse<NodePipes>())
        {
            p.CurrentProgress--;
            p.CurrentNodePipe.Clean();
        }

        var nodePipesToProcess = nodePipesOrdering.Where(npo => npo.CurrentValue != null);
        foreach (var p in nodePipesToProcess)
        {
            p.CurrentValue = p.CurrentNodePipe.PipeValue(p.CurrentValue);
            p.CurrentProgress++;
        }
    }

    private void Process()
    {
        GD.Print("PipeContext process!!");
        OutputNodes = new Array<OutputNode>(_pipelineNodeDict.Values
            .Where(n => n is OutputNode)
            .Select(n => (OutputNode)n));

        var receiverPipes = _pipelineNodeDict.Values
            .SelectMany(p => p.NextPipes)
            .Distinct();

        var inputPipes = _pipelineNodeDict.Values.Except(receiverPipes);

        _nodePipesList = new Array<NodePipes>();

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

        _nodeDependencies = new Array<NodeDependency>(_pipelineNodeDict.Values
            .SelectMany(pn => pn.NodeDependencies.Select(nd => new { Node = pn, NodeDependencyPath = nd }))
            .Select(pn => new NodeDependency { Node = pn.Node, Dependency = OutputNodes.SingleOrDefault(on => on.AbsoluteDestinationIncludingNode == pn.NodeDependencyPath) })
            .Where(pn => pn.Dependency != null));

        var nodePipesOrdering = OrderEvaluation(_nodePipesList);

        EvaluateNodePipes(nodePipesOrdering);

        // TODO: This gets annoying, we only need this when an import has changed
        EditorInterface.Singleton.SaveScene();
    }

    public override void _ExitTree()
    {
        GD.Print("PipeContext Exit tree!");
        if (OS.HasFeature("editor_runtime"))
        {
            return;
        }

        Pipelines.Instance.UnregisterContext(this);
    }

}
#else
public partial class PipeContext : Node
{

}
#endif