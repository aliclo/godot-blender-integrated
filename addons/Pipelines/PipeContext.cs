using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class PipeContext : Node {

    private class NodePipes {
        public IReceivePipe CurrentNodePipe => Pipes[CurrentProgress];
        public PipeValue CurrentValue { get; set; }
        public List<IReceivePipe> Pipes { get; set; }
        public int CurrentProgress { get; set; } = 0;
    }

    public Node RootNode => this;
    public List<OutputNode> OrderOfCreation { get; set; }
    public System.Collections.Generic.Dictionary<string, object> ContextData { get; set; }
    public System.Collections.Generic.Dictionary<string, PipelineNode> PipelineNodeDict => _pipelineNodeDict;

    private readonly PipelineAccess _pipelineAccess = new PipelineAccess();
    private readonly PipeMapper _pipeMapper = new PipeMapper();
    private System.Collections.Generic.Dictionary<string, PipelineNode> _pipelineNodeDict = new System.Collections.Generic.Dictionary<string, PipelineNode>();
    private List<NodePipes> _nodePipesList;
    private bool _completedFirstImport = false;

    public override void _Ready()
    {
        GD.Print("Ready!!");

        var sceneFilePath = GetTree().EditedSceneRoot.SceneFilePath;
        var pipelineContextStores = _pipelineAccess.Read(sceneFilePath);
        var pipelineContextStore = pipelineContextStores?.SingleOrDefault(pcs => pcs.Name == Name);
        if (pipelineContextStore != null)
        {
            AddNodesAndConnections(pipelineContextStore);   
        }

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

    public void RegisterPipe(ValuePipe valuePipe) {
        _nodePipesList.AddRange(GetNodePipes(valuePipe));
    }

    public void ReprocessPipe(List<ValuePipe> valuePipes) {
        foreach(var valuePipe in valuePipes) {
            valuePipe.Pipe.PreRegistration();
        }

        var nodePipes = new List<NodePipes>();

        foreach(var valuePipe in valuePipes) {
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
        var nodePipes = new List<List<IReceivePipe>>() {new List<IReceivePipe>() { pipe }};
        List<List<IReceivePipe>> nodePipesWithNext;

        do {
            nodePipesWithNext = nodePipes.Where(np => np.Last().NextPipes != null && np.Last().NextPipes.Any()).ToList();
            var nodePipesWithoutNext = nodePipes.Except(nodePipesWithNext);
            processedPipes.AddRange(nodePipesWithoutNext);

            var newNodePipes = new List<List<IReceivePipe>>();
            foreach(var nodePipe in nodePipesWithNext) {
                var lastPipe = nodePipe.Last();
                newNodePipes.AddRange(lastPipe.NextPipes.Select(np => {
                    var clonedNodePipe = new List<IReceivePipe>(nodePipe);
                    clonedNodePipe.Add(np);
                    return clonedNodePipe;
                }));
            }
            nodePipes = newNodePipes;
        } while (nodePipesWithNext.Any());
        
        return processedPipes.Select(p => new NodePipes(){
            CurrentValue = cloneablePipeValue.ClonePipeValue(),
            Pipes = p,
            CurrentProgress = 0
        });
    }

    private List<NodePipes> OrderEvaluation(List<NodePipes> nodePipesList) {
        // When determining processing, there are two types:
        // - 1. Ones that can be done directly without any dependency as they are done singly
        // - 2. Ones that are done together and need to be done within an order
        // For (2) we can therefore generate this ordering for cleanup and piping
        
        var nodePipesOrdering = new List<NodePipes>();
        var evaluateNodePipes = new List<NodePipes>(nodePipesList);
        while(evaluateNodePipes.Any()) {
            if(evaluateNodePipes.Any(p => OrderOfCreation.Contains(p.CurrentNodePipe))) {
                if(OrderOfCreation.All(ooc => evaluateNodePipes.Select(eoop => eoop.CurrentNodePipe).Contains(ooc))) {
                    var orderOfEvaluation = OrderOfCreation
                        .Select(oocp => evaluateNodePipes.Single(eoop => eoop.CurrentNodePipe == oocp))
                        .ToList();
                    
                    foreach(var p in orderOfEvaluation) {
                        nodePipesOrdering.Add(p);
                        p.CurrentProgress++;
                    }
                } else {
                    var pipesToProcess = evaluateNodePipes
                        .Where(p => !OrderOfCreation.Contains(p.CurrentNodePipe));
                    
                    foreach(var p in pipesToProcess) {
                        nodePipesOrdering.Add(p);
                        p.CurrentProgress++;
                    }
                }
            } else {
                foreach(var p in evaluateNodePipes) {
                    nodePipesOrdering.Add(p);
                    p.CurrentProgress++;
                }
            }

            evaluateNodePipes.RemoveAll(p => p.CurrentProgress == p.Pipes.Count);
        }

        return nodePipesOrdering;
    }

    private void EvaluateNodePipes(List<NodePipes> nodePipesOrdering) {
        foreach(var p in nodePipesOrdering.Reverse<NodePipes>()) {
            p.CurrentProgress--;
            p.CurrentNodePipe.Clean();
        }

        var nodePipesToProcess = nodePipesOrdering.Where(npo => npo.CurrentValue != null);
        foreach(var p in nodePipesToProcess) {
            p.CurrentValue = p.CurrentNodePipe.Pipe(p.CurrentValue);
            p.CurrentProgress++;
        }
    }

    private void Process()
    {
        var inputPipes = _pipelineNodeDict.Values
            .Where(n => n is IInputPipe)
            .Select(n => (IInputPipe)n);

        OrderOfCreation = new List<OutputNode>();
        ContextData = new System.Collections.Generic.Dictionary<string, object>();
        _nodePipesList = new List<NodePipes>();

        foreach (var inputPipe in inputPipes)
        {
            inputPipe.Register();
        }

        foreach (var pipeList in _nodePipesList)
        {
            foreach (var pipe in pipeList.Pipes)
            {
                pipe.PreRegistration();
            }
        }

        var nodePipesOrdering = OrderEvaluation(_nodePipesList);

        EvaluateNodePipes(nodePipesOrdering);
    }
    
}