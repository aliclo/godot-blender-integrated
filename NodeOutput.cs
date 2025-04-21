using System.Collections.Generic;
using Godot;

[Tool]
public partial class NodeOutput : Node, INodePipe
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

    private PipeContext _context;
    private NodePath _destination;

    private string _nodeName;
    private Node _node;

    public void Init(PipeContext context, string nodeName)
    {
        _context = context;
        _nodeName = nodeName;

        _node = GetNodeOrNull(_destination + "/" + nodeName);
        
        SetOrder();
    }

    public void Pipe(object obj)
    {
        if(obj is not Node) {
            return;
        }

        var node = (Node) obj;
        _node = node;

        if (_destination == null || _destination.IsEmpty) {
            return;
        }

        var owner = GetParent()?.Owner ?? GetParent();
        var parent = GetNode(_destination);
        parent.AddChild(node);
        node.Owner = owner;
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
        // var nodeAbsolutePath = _context.RootNode.GetPathTo(_node);
        
        _context.OrderOfCreation.Remove(this);
        
        if(AbsoluteDestination == null || AbsoluteDestination.IsEmpty) {
            _context.OrderOfCreation.Add(this);
            return;
        }

        int indexOfParentNode = _context.OrderOfCreation.FindIndex(no => no.AbsoluteDestinationIncludingNode == AbsoluteDestination);
        if(indexOfParentNode == -1) {
            int indexOfChildNode = _context.OrderOfCreation.FindIndex(no => no.AbsoluteDestination == AbsoluteDestinationIncludingNode);
            if(indexOfChildNode == -1) {
                _context.OrderOfCreation.Add(this);
            } else {
                _context.OrderOfCreation.Insert(indexOfChildNode, this);
            }
        } else {
            _context.OrderOfCreation.Insert(indexOfParentNode+1, this);
        }
    }

    public void PipeConnect()
    {
        
    }

    public void PipeDisconnect()
    {
        Clean();
        _context.OrderOfCreation.Remove(this);
    }
}