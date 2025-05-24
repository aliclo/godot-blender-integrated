using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class PipelineGraph : GraphEdit
{

    [Export]
    public NodePath PopupMenu { get; set; }
    public EditorUndoRedoManager UndoRedo { get; set; }
    public string ContextName { get; set; }

    private PopupMenu _popupMenu;

    private PackedScene _sceneModelNode;
    private PackedScene _outputNode;
    private Vector2 _lastMousePos = Vector2.Zero;

    public override void _Ready()
    {
        AddValidConnectionType((int)PipelineNodeTypes.Mesh, (int)PipelineNodeTypes.Any);

        _popupMenu = GetNode<PopupMenu>(PopupMenu);
        _sceneModelNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/SceneModelNode.tscn");
        _outputNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/OutputNode.tscn");
        ConnectionRequest += HandleConnectionRequest;
        DisconnectionRequest += HandleDisconnectionRequest;
        DeleteNodesRequest += HandleDeleteNodesRequest;
    }

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseButton inputMouseButton && inputMouseButton.IsPressed() && inputMouseButton.ButtonIndex == MouseButton.Right) {
            var mousePos = GetGlobalMousePosition();
            _lastMousePos = GetLocalMousePosition();
            _popupMenu.Popup(new Rect2I((int) mousePos.X, (int) mousePos.Y, _popupMenu.Size.X, _popupMenu.Size.Y));
        }
    }

    public void HandleConnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort) {
        var connected = GetConnectionList().SingleOrDefault(c => (string) c["to_node"] == toNodeName && (long) c["to_port"] == toPort);

        if (connected == null)
        {
            UndoRedo.CreateAction($"Connect {fromNodeName} to {toNodeName}");

            UndoRedo.AddDoMethod(this, GraphEdit.MethodName.ConnectNode, fromNodeName, fromPort, toNodeName, toPort);

            UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNodeName, fromPort, toNodeName, toPort);

            UndoRedo.CommitAction();
        }
        else
        {
            var otherFromNodeName = connected["from_node"];
            var otherFromPort = connected["from_port"];

            UndoRedo.CreateAction($"Connect {fromNodeName} to {toNodeName} instead of {otherFromNodeName} to {toNodeName}");

            UndoRedo.AddDoMethod(this, MethodName.ReplaceNodeConnection, otherFromNodeName, otherFromPort, fromNodeName, fromPort, toNodeName, toPort);
            UndoRedo.AddUndoMethod(this, MethodName.ReplaceNodeConnection, fromNodeName, fromPort, otherFromNodeName, otherFromPort, toNodeName, toPort);

            UndoRedo.CommitAction();
        }
    }

    private void ReplaceNodeConnection(string oldFromNodeName, int oldFromPort, string newFromNodeName, int newFromPort, string toNodeName, int toPort)
    {
        DisconnectNode(oldFromNodeName, oldFromPort, toNodeName, toPort);
        ConnectNode(newFromNodeName, newFromPort, toNodeName, toPort);
    }

    public void RightClickMenuChosen(int id)
    {
        PackedScene packedSceneToCreate = null;

        switch (id)
        {
            case (int)RightClickMenuItems.AddSceneModelNode:
                packedSceneToCreate = _sceneModelNode;
                break;
            case (int)RightClickMenuItems.AddOutputNode:
                packedSceneToCreate = _outputNode;
                break;
        }

        if (packedSceneToCreate == null)
        {
            return;
        }

        var graphNode = packedSceneToCreate.Instantiate<GraphNode>();
        graphNode.PositionOffset = (_lastMousePos + ScrollOffset) / Zoom;

        UndoRedo.CreateAction($"Add {graphNode.GetType().Name} node");
        UndoRedo.AddDoMethod(this, Node.MethodName.AddChild, graphNode);
        UndoRedo.AddUndoMethod(this, Node.MethodName.RemoveChild, graphNode);
        UndoRedo.CommitAction();
    }

    public IEnumerable<PipelineNode> GetNodes()
    {
        return GetChildren().Where(c => c is PipelineNode).Select(c => (PipelineNode)c);
    }

    private void HandleDisconnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort)
    {
        UndoRedo.CreateAction($"Disconnect {fromNodeName} from {toNodeName}");

        UndoRedo.AddDoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNodeName, fromPort, toNodeName, toPort);

        UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.ConnectNode, fromNodeName, fromPort, toNodeName, toPort);

        UndoRedo.CommitAction();
    }

    private void HandleDeleteNodesRequest(Array nodeNames)
    {
        var nodes = new Array<PipelineNode>(nodeNames
            .Select(name => GetNode<PipelineNode>(new NodePath((StringName)name))));

        var nodeStore = new Array<PipelineNodeStore>(nodes
            .Select(Map));

        var connections = new Array<PipelineConnection>(GetConnectionList()
            .Select(Map)
            .Where(c => nodeNames.Contains(c.FromNodeName) || nodeNames.Contains(c.ToNodeName)));

        var pipelineContextStore = new PipelineContextStore()
        {
            Nodes = nodeStore,
            Connections = connections
        };

        UndoRedo.CreateAction($"Delete nodes");
        UndoRedo.AddDoMethod(this, MethodName.DeleteNodes, nodes);
        UndoRedo.AddUndoMethod(this, MethodName.AddNodesAndConnections, pipelineContextStore);
        UndoRedo.CommitAction();
    }

    private void DeleteNodes(Array<PipelineNode> nodes)
    {
        foreach (var node in nodes)
        {
            RemoveChild(node);
            node.QueueFree();
        }
    }
    
    private void AddNodesAndConnections(PipelineContextStore pipelineContextStore)
    {
        foreach (var pipelineNodeStore in pipelineContextStore.Nodes)
        {
            var nodeResourcePath = $"res://addons/Pipelines/Nodes/{pipelineNodeStore.Type}.tscn";
            var pipelineNode = GD.Load<PackedScene>(nodeResourcePath).Instantiate<PipelineNode>();

            pipelineNode.Name = pipelineNodeStore.Name;
            pipelineNode.Load(pipelineNodeStore.Data);
            AddChild(pipelineNode);
            pipelineNode.PositionOffset = new Vector2(pipelineNodeStore.X, pipelineNodeStore.Y);
        }

        foreach (var pipelineConnection in pipelineContextStore.Connections)
        {
            ConnectNode(pipelineConnection.FromNodeName, pipelineConnection.FromPort, pipelineConnection.ToNodeName, pipelineConnection.ToPort);
        }
    }

    public Variant GetData()
    {
        var nodes = GetNodes().Select(Map);

        var nodeConnections = GetConnectionList().Select(Map);

        var pipelineContextStore = new PipelineContextStore()
        {
            Name = ContextName,
            Nodes = new Array<PipelineNodeStore>(nodes),
            Connections = new Array<PipelineConnection>(nodeConnections)
        };

        return pipelineContextStore;
    }

    public PipelineNodeStore Map(PipelineNode pipelineNode)
    {
        return new PipelineNodeStore()
        {
            Name = pipelineNode.Name,
            Type = pipelineNode.GetType().Name,
            X = pipelineNode.PositionOffset.X,
            Y = pipelineNode.PositionOffset.Y,
            Data = pipelineNode.GetData()
        };
    }

    public PipelineConnection Map(Dictionary connection)
    {
        return new PipelineConnection()
        {
            FromNodeName = (string)connection["from_node"],
            FromPort = (int)connection["from_port"],
            ToNodeName = (string)connection["to_node"],
            ToPort = (int)connection["to_port"]
        };
    }

    public void Load(object data)
    {
        if (data == null)
        {
            return;
        }

        if (data is not PipelineContextStore pipelineContextStore)
        {
            return;
        }

        AddNodesAndConnections(pipelineContextStore);
    }

}
