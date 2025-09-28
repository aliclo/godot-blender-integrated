#if TOOLS
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class OutputNode : PipelineNode
{

    private partial class OutputNodeStore : GodotObject
    {
        [Export]
        public string Destination { get; set; }
        [Export]
        public string NodeName { get; set; }
    }

    private NodeCopier _nodeCopier = new NodeCopier();
    private OutputNodeStore _outputNodeStore;
    private PipeContext _context;

    private Button _outputNodePicker;
    private LineEdit _nodeNameEditor;

    private NodePath _destination;
    private string _nodeName;
    private Node _node;
    private Node _previousNode;
    private Array<NodePath> _nodeDependencies;

    public override Array<PipelineNode> NextPipes => [];


    public NodePath AbsoluteDestination
    {
        get
        {
            var destinationNode = _context.RootNode.GetNodeOrNull(_destination);
            if (destinationNode == null)
            {
                return null;
            }

            var absoluteDestination = _context.RootNode.GetPathTo(destinationNode);
            return absoluteDestination;
        }
    }

    public NodePath AbsoluteDestinationIncludingNode
    {
        get
        {
            var absoluteDestination = AbsoluteDestination;
            if (absoluteDestination == null)
            {
                return null;
            }

            return absoluteDestination + "/" + _nodeName;
        }
    }

    public override Array<Array<PipelineNode>> NodeConnections => EMPTY_NODE_CONNECTIONS;

    public override Array<NodePath> NodeDependencies => _nodeDependencies;

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new OutputNodeStore()
        {
            Destination = _destination,
            NodeName = _nodeName
        });
    }

    public override void Load(Variant data)
    {
        _outputNodeStore = GodotJsonParser.FromJsonType<OutputNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;

        _nodeDependencies = new Array<NodePath>();

        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0, (int)PipelineNodeTypes.Any);
        SetSlotColorLeft(0, TypeConnectorColors.ANY);
        var nodeLabel = new Label();
        nodeLabel.Text = "Node";
        AddChild(nodeLabel);

        _outputNodePicker = new Button();
        _outputNodePicker.Pressed += OutputNodePickerPressed;
        AddChild(_outputNodePicker);

        _nodeNameEditor = new LineEdit();
        _nodeNameEditor.TextChanged += NodeNameChanged;
        AddChild(_nodeNameEditor);

        if (_outputNodeStore != null)
        {
            _outputNodePicker.Text = _outputNodeStore.Destination;
            _destination = _outputNodeStore.Destination;
            _nodeNameEditor.Text = _outputNodeStore.NodeName;
            _nodeName = _outputNodeStore.NodeName;
        }
        else
        {
            _outputNodePicker.Text = "Select node";
        }
    }

    public override void Register()
    {
        _nodeDependencies = new Array<NodePath>();

        if (AbsoluteDestination != null && !AbsoluteDestination.IsEmpty)
        {
            _nodeDependencies.Add(AbsoluteDestination);
        }

        var node = _context.RootNode.GetNodeOrNull(_destination + "/" + _nodeName);

        if (node != null)
        {
            _node = node;
            _node.Renamed += OutputNodeRenamed;
            _previousNode = _node.Duplicate();
        }
    }

    public override ICloneablePipeValue PipeValue(ICloneablePipeValue pipeValue)
    {
        var obj = pipeValue.ClonePipeValue().Value;
        if (obj is not Node node)
        {
            return null;
        }

        if (_previousNode != null)
        {
            node = _nodeCopier.CopyValues(_previousNode, pipeValue);

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

        node.Name = _nodeName;
        var owner = _context.RootNode?.Owner ?? _context.RootNode;
        parent.AddChild(node);
        node.Owner = owner;

        _previousNode = node.Duplicate();

        _node = node;
        _node.Renamed += OutputNodeRenamed;

        return null;
    }

    public override void Clean()
    {
        if (_node != null)
        {
            _node.Renamed -= OutputNodeRenamed;
            var nodeParent = _node.GetParent();
            if (nodeParent != null)
            {
                nodeParent.RemoveChild(_node);
            }
            _node.QueueFree();
            _node = null;
        }
    }

    public override void PipeDisconnect()
    {
        Clean();
    }

    private void OutputNodePickerPressed()
    {
        EditorInterface.Singleton.PopupNodeSelector(Callable.From<NodePath>(OutputNodePathChanged));
    }

    private void OutputNodeRenamed()
    {
        NodeNameChanged(_node.Name);
    }

    private void NodeNameChanged(string nodeName)
    {
        if (_nodeNameEditor.Text != nodeName)
        {
            _nodeNameEditor.Text = nodeName;
        }

        if (string.IsNullOrWhiteSpace(nodeName))
        {
            _nodeName = "Unnamed-Node";
        }
        else
        {
            _nodeName = nodeName;
        }

        if (_node != null)
        {
            if (_node.Name != _nodeName)
            {
                _node.Name = _nodeName;
            }
        }
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

    public override void AddConnection(int index, Array<PipelineNode> receivePipes)
    {
        // No output connections
    }

    public override void Connect(int index, Array<PipelineNode> receivePipes)
    {
        // No output connections
    }

    public override void Disconnect(int index, Array<PipelineNode> receivePipes)
    {
        // No output connections
    }

    public override void DisposePipe()
    {
        // Nothing to dispose
        GD.Print("Disposed OutputNode!");
    }

    public override void _ExitTree()
    {
        GD.Print("Exiting tree for OutputNode!");
        _outputNodePicker.Pressed -= OutputNodePickerPressed;
        _nodeNameEditor.TextChanged -= NodeNameChanged;
        if (_node != null)
        {
            _node.Renamed -= OutputNodeRenamed;
        }
    }

}
#endif