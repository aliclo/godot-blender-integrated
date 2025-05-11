using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class NodeOutput : Node, IReceivePipe
{

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

    public void Register(PipeContext context, string nodeName)
    {
        _context = context;
        _nodeName = nodeName;

        _node = GetNodeOrNull(_destination + "/" + nodeName);

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

    public object Pipe(object obj)
    {
        if(obj is not Node) {
            return null;
        }

        var node = (Node) obj;
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

        return null;
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