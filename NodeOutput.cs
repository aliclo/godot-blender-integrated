using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class NodeOutput : Node, IReceivePipe
{

    private class NodeProp {
        public string Name { get; set; }
        public Variant Value { get; set; }
    }

    private static readonly List<string> PropNamesToIgnore = new List<string>() {
        "Node",
        "_import_path",
        "name",
        "unique_name_in_owner",
        "scene_file_path",
        "owner",
        "multiplayer",
        "Process",
        "Node3D",
        "Thread Group",
        "Transform",
        "global_transform",
        "global_position",
        "global_basis",
        "global_rotation",
        "global_rotation_degrees",
        "Visibility",
        "visibility_parent",
        "VisualInstance3D",
        "Sorting",
        "GeometryInstance3D",
        "Geometry",
        "Global Illumination",
        "Visibility Range",
        "MeshInstance3D",
        "Skeleton",
        "MyScript"
    };

    [Export]
    public NodePath Destination {
        get {
            return _destination;
        }
        set {
            if(_destination != null && !_destination.IsEmpty) {
                // var existingMeshPath = _context.RootNode.GetPathTo(_node);
                if(_node != null) {
                    var nodeParent = _node.GetParent();
                    if(nodeParent != null) {
                        nodeParent.RemoveChild(_node);
                    }
                }
                //_mesh = null;
            }

            _destination = value;

            if(!IsNodeReady()) {
                return;
            }

            var parentNodePath = value;
            if(!parentNodePath.IsEmpty && _node != null) {
                var parentNode = GetNode(parentNodePath);
                parentNode.AddChild(_node);
                var owner = GetParent()?.Owner ?? GetParent();
                _node.Owner = owner;
                // _mesh = mesh;
                SetOrder();
            }
        }
    }

    public NodePath AbsoluteDestination { 
        get {
            var destinationNode = GetNodeOrNull(_destination);
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

    public List<IReceivePipe> NextPipes => null;

    private PipeContext _context;
    private NodePath _destination;

    private string _nodeName;
    private Node _node;
    private List<Node> _previousNodeChildren;
    private List<NodeProp> _previousNodeProps;

    public void Register(PipeContext context, string nodeName)
    {
        _context = context;
        _nodeName = nodeName;

        _node = GetNodeOrNull(_destination + "/" + nodeName);
        if(_node != null) {
            GetPreviousNodeValues(_node);
        }

        HashSet<NodeOutput> nodeOutputs;
        bool exists = _context.ContextData.TryGetValue(nameof(NodeOutput), out object nodeOutputsObj);

        if(exists) {
            nodeOutputs = (HashSet<NodeOutput>) nodeOutputsObj;
        } else {
            nodeOutputs = new HashSet<NodeOutput>();
            _context.ContextData[nameof(NodeOutput)] = nodeOutputs;
        }

        nodeOutputs.Add(this);
    }

    public void Init()
    {
        SetOrder();
    }

    public PipeValue Pipe(PipeValue pipeValue)
    {
        var obj = pipeValue.Value;
        if(obj is not Node) {
            return null;
        }

        var node = (Node) obj;

        if(_previousNodeProps != null) {
            foreach(var prop in _previousNodeProps.Where(p => !pipeValue.TouchedProperties.Contains(p.Name))) {
                node.Set(prop.Name, prop.Value);
            }
        }

        if(_previousNodeChildren != null) {
            foreach(var child in _previousNodeChildren) {
                child.GetParent().RemoveChild(child);
                child.Owner = null;
                node.AddChild(child);
            }
        }

        _node = node;

        if (_destination == null || _destination.IsEmpty) {
            return null;
        }

        var parent = GetNodeOrNull(_destination);

        if(parent == null) {
            return null;
        }

        var owner = GetParent()?.Owner ?? GetParent();
        parent.AddChild(node);
        node.Owner = owner;

        GetPreviousNodeValues(node);

        return null;
    }

    private void GetPreviousNodeValues(Node previousNode) {
        var properties = previousNode.GetPropertyList();
        var names = properties.Select(p => (string) p["name"]).Where(n => !PropNamesToIgnore.Contains(n));
        _previousNodeProps = names
            .Select(n => new NodeProp() { Name = n, Value = previousNode.Get(n) })
            .ToList();

        _previousNodeChildren = previousNode.GetChildren().ToList();
    }

    public void Clean()
    {
        if(_node != null) {
            var nodeParent = _node.GetParent();
            if(nodeParent != null) {
                nodeParent.RemoveChild(_node);
            }
            _node.QueueFree();
            _node = null;
        }
    }

    private void SetOrder() {
        _context.OrderOfCreation.Remove(this);

        if(AbsoluteDestination == null || AbsoluteDestination.IsEmpty) {
            return;
        }

        var nodeOutputs = (HashSet<NodeOutput>) _context.ContextData[nameof(NodeOutput)];

        if(nodeOutputs.Any(no => no.AbsoluteDestinationIncludingNode == AbsoluteDestination || no.AbsoluteDestination == AbsoluteDestinationIncludingNode)) {
            int indexOfParentNode = _context.OrderOfCreation.FindIndex(no => no.AbsoluteDestinationIncludingNode == AbsoluteDestination);
            if(indexOfParentNode != -1) {
                _context.OrderOfCreation.Insert(indexOfParentNode+1, this);
            } else {
                int indexOfChildNode = _context.OrderOfCreation.FindIndex(no => no.AbsoluteDestination == AbsoluteDestinationIncludingNode);
                if(indexOfChildNode != -1) {
                    _context.OrderOfCreation.Insert(indexOfChildNode, this);
                } else {
                    _context.OrderOfCreation.Add(this);
                }
            }
        }
    }

    public void PipeDisconnect()
    {
        Clean();
        _context.OrderOfCreation.Remove(this);

        var nodeOutputs = (HashSet<NodeOutput>) _context.ContextData[nameof(NodeOutput)];
        nodeOutputs.Remove(this);
    }

}