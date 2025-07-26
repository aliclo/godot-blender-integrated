#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class OutputNode : PipelineNode, IReceivePipe
{

    private partial class OutputNodeStore : GodotObject
    {
        [Export]
        public string Destination { get; set; }
    }

    private class NodeProp
    {
        public string Name { get; set; }
        public Variant Value { get; set; }
    }

    private NodeCopier _nodeCopier = new NodeCopier();
    private OutputNodeStore _outputNodeStore;
    private PipeContext _context;

    private Button _outputNodePicker;

    private NodePath _destination;
    private string _nodeName;
    private Node _node;
    private Node _previousNode;
    private List<NodePath> _nodeDependencies = new List<NodePath>();

    public List<IReceivePipe> NextPipes => null;


    public NodePath AbsoluteDestination { 
        get {
            var destinationNode = _context.RootNode.GetNodeOrNull(_destination);
            if(destinationNode == null) {
                return null;
            }

            var absoluteDestination = _context.RootNode.GetPathTo(destinationNode);
            return absoluteDestination;
        }
    }

    public NodePath AbsoluteDestinationIncludingNode {
        get {
            var absoluteDestination = AbsoluteDestination;
            if(absoluteDestination == null) {
                return null;
            }

            return absoluteDestination + "/" + _nodeName;
        }
    }

    public override List<List<IReceivePipe>> NodeConnections => EMPTY_NODE_CONNECTIONS;

    public override List<NodePath> NodeDependencies => _nodeDependencies;

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new OutputNodeStore()
        {
            Destination = _destination
        });
    }

    public override void Load(Variant data)
    {
        _outputNodeStore = GodotJsonParser.FromJsonType<OutputNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;

        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0, (int)PipelineNodeTypes.Any);
        SetSlotColorLeft(0, TypeConnectorColors.ANY);
        var nodeLabel = new Label();
        nodeLabel.Text = "Node";
        AddChild(nodeLabel);

        _outputNodePicker = new Button();
        _outputNodePicker.Pressed += OutputNodePickerPressed;
        AddChild(_outputNodePicker);

        if (_outputNodeStore != null)
        {
            _outputNodePicker.Text = _outputNodeStore.Destination;
            _destination = _outputNodeStore.Destination;
        }
        else
        {
            _outputNodePicker.Text = "Select node";
        }
    }

    public void Register()
    {
        _nodeDependencies = new List<NodePath>();

        if (AbsoluteDestination != null && !AbsoluteDestination.IsEmpty)
        {
            _nodeDependencies.Add(AbsoluteDestination);
        }

        _node = _context.RootNode.GetNodeOrNull(_destination + "/" + _nodeName);
        
        if (_node != null)
        {
            _previousNode = _node.Duplicate();
        }
    }

    public void PreRegister(string nodeName)
    {
        _nodeName = nodeName;
    }

    public PipeValue Pipe(PipeValue pipeValue)
    {
        var obj = pipeValue.Value;
        if (obj is not Node node)
        {
            return null;
        }

        if (_previousNode != null)
        {
            node = _nodeCopier.CopyValues(_previousNode, node, pipeValue.UntouchedProperties, pipeValue.TouchedProperties);

            var outputNodes = _context.OutputNodes;
            var outputNodePaths = outputNodes.Select(on => on.AbsoluteDestinationIncludingNode);
            var nodeChildren = _previousNode.GetChildren().Where(c => !outputNodePaths.Contains(_context.GetPathTo(c))).ToList();

            foreach (var child in nodeChildren)
            {
                child.GetParent().RemoveChild(child);
                child.Owner = null;
                node.AddChild(child);
            }
        }

        if (_destination == null || _destination.IsEmpty)
        {
            return null;
        }

        var parent = _context.RootNode.GetNodeOrNull(_destination);

        if (parent == null)
        {
            return null;
        }

        var owner = _context.RootNode?.Owner ?? _context.RootNode;
        parent.AddChild(node);
        node.Owner = owner;

        _previousNode = node.Duplicate();

        _node = node;

        return null;
    }

    public void Clean()
    {
        if (_node != null)
        {
            var nodeParent = _node.GetParent();
            if (nodeParent != null)
            {
                nodeParent.RemoveChild(_node);
            }
            _node.QueueFree();
            _node = null;
        }
    }

    public void PipeDisconnect()
    {
        Clean();
    }

    private void OutputNodePickerPressed()
    {
        EditorInterface.Singleton.PopupNodeSelector(Callable.From<NodePath>(OutputNodePathChanged));
    }

    private void OutputNodePathChanged(NodePath destinationPath)
    {
        if (!destinationPath.IsEmpty)
        {
            var newlyChosenParentNode = _context.RootNode.Owner.GetNodeOrNull(destinationPath);

            if (newlyChosenParentNode != null)
            {
                _destination = _context.RootNode.GetPathTo(newlyChosenParentNode);
                _outputNodePicker.Text = _destination;

                _context.Reprocess();
            }
        }
        
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    public override void AddConnection(int index, List<IReceivePipe> receivePipes)
    {
        // No output connections
    }

    public override void Connect(int index, List<IReceivePipe> receivePipes)
    {
        // No output connections
    }

    public override void Disconnect(int index, List<IReceivePipe> receivePipes)
    {
        // No output connections
    }

    public override void DisposePipe()
    {
        // Nothing to dispose
    }
}
#endif