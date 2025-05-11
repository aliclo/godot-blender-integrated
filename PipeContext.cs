using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class PipeContext : Node {

    private class NodePipes {
        public IReceivePipe CurrentNodePipe => Pipes[CurrentProgress];
        public object CurrentValue { get; set; }
        public List<IReceivePipe> Pipes { get; set; }
        public int CurrentProgress { get; set; } = 0;
    }

    public Node RootNode => this;
    public List<NodeOutput> OrderOfCreation { get; set; }
    public System.Collections.Generic.Dictionary<string, object> ContextData { get; set; }

    private List<NodePipes> _nodePipesList;
    private bool _completedFirstImport = false;

    public override void _Ready()
    {
        Owner.Ready += () => {
            GD.Print("Ready!!");

            Import();
            _completedFirstImport = true;
        };
    }

    public void Reimport() {
        if(_completedFirstImport) {
            Import();
        }
    }

    public void RegisterPipe(IReceivePipe pipe, object value) {
        // pipe.Register(this, name);
        var nodePipes = new List<IReceivePipe>();
        var nextPipeNode = pipe;
        while(nextPipeNode != null) {
            nodePipes.Add(nextPipeNode);
            nextPipeNode = nextPipeNode.NextPipe;
        }
        _nodePipesList.Add(new NodePipes() {
            CurrentValue = value,
            Pipes = nodePipes,
            CurrentProgress = 0
        });
    }

    private void Import() {
        var allDescendents = new List<Node>();
        var children = GetChildren().ToList();

        while(children.Any()) {
            allDescendents.AddRange(children);
            children = children.SelectMany(c => c.GetChildren()).ToList();
        }
        
        var inputPipes = allDescendents
            .Where(n => n is IInputPipe)
            .Select(n => (IInputPipe) n);

        OrderOfCreation = new List<NodeOutput>();
        ContextData = new System.Collections.Generic.Dictionary<string, object>();
        _nodePipesList = new List<NodePipes>();

        foreach(var inputPipe in inputPipes) {
            inputPipe.Register();
        }

        foreach(var pipeList in _nodePipesList) {
            foreach(var pipe in pipeList.Pipes) {
                pipe.Init();
            }
        }

        // When determining processing, there are two types:
        // - 1. Ones that can be done directly without any dependency as they are done singly
        // - 2. Ones that are done together and need to be done within an order
        // For (2) we can therefore generate this ordering for cleanup and piping
        
        var nodePipesOrdering = new List<NodePipes>();
        var evaluateNodePipes = new List<NodePipes>(_nodePipesList);
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
    
}