using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class PipelineGraph : GraphEdit, IStorable
{

    [Export]
    public NodePath PopupMenu { get; set; }
    public string ContextName { get; set; }

    private PopupMenu _popupMenu;

    private PackedScene SceneModelNode;
    private PackedScene OutputNode;
    private Vector2 LastMousePos = Vector2.Zero;

    public override void _Ready()
    {
        AddValidConnectionType((int) PipelineNodeTypes.Mesh, (int) PipelineNodeTypes.Any);

        _popupMenu = GetNode<PopupMenu>(PopupMenu);
        SceneModelNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/SceneModelNode.tscn");
        OutputNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/OutputNode.tscn");
        ConnectionRequest += HandleConnectionRequest;
        DisconnectionRequest += HandleDisconnectionRequest;
        DeleteNodesRequest += HandleDeleteNodesRequest;
    }

    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventMouseButton inputMouseButton && inputMouseButton.IsPressed() && inputMouseButton.ButtonIndex == MouseButton.Right) {
            var mousePos = GetGlobalMousePosition();
            LastMousePos = GetLocalMousePosition();
            _popupMenu.Popup(new Rect2I((int) mousePos.X, (int) mousePos.Y, _popupMenu.Size.X, _popupMenu.Size.Y));
        }
    }

    public void HandleConnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort) {
        var connected = GetConnectionList().SingleOrDefault(c => (string) c["to_node"] == toNodeName && (long) c["to_port"] == toPort);

        if(connected != null) {
            var otherFromNodeName = connected["from_node"];
            var otherFromPort = connected["from_port"];
            DisconnectNode((string) otherFromNodeName, (int) otherFromPort, toNodeName, (int) toPort);
        }

        ConnectNode(fromNodeName, (int) fromPort, toNodeName, (int) toPort);
    }

    public void RightClickMenuChosen(int id) {
        PackedScene packedSceneToCreate = null;

        switch (id) {
            case (int) RightClickMenuItems.AddSceneModelNode:
                packedSceneToCreate = SceneModelNode;
                break;
            case (int) RightClickMenuItems.AddOutputNode:
                packedSceneToCreate = OutputNode;
                break;
        }

        if(packedSceneToCreate == null) {
            return;
        }

        var graphNode = packedSceneToCreate.Instantiate<GraphNode>();
        AddChild(graphNode);
        graphNode.PositionOffset = (LastMousePos+ScrollOffset)/Zoom;
    }

    public IEnumerable<PipelineNode> GetNodes() {
        return GetChildren().Where(c => c is PipelineNode).Select(c => (PipelineNode) c);
    }

    private void HandleDisconnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort)
    {
        DisconnectNode(fromNodeName, (int) fromPort, toNodeName, (int) toPort);
    }

    private void HandleDeleteNodesRequest(Array nodeNames) {
        foreach(StringName nodeName in nodeNames) {
            var node = GetNode<GraphNode>(new NodePath(nodeName));

            RemoveChild(node);
            node.QueueFree();
        }
    }

    public object GetData()
    {
        var nodes = GetNodes().Select(n => new PipelineNodeStore() {
            Name = n.Name,
            Type = n.GetType().Name,
            X = n.PositionOffset.X,
            Y = n.PositionOffset.Y,
            Data = n.GetData()
        });

        var nodeConnections = GetConnectionList().Select(connection => new PipelineConnection() {
            FromNodeName = (string) connection["from_node"],
            FromPort = (int) connection["from_port"],
            ToNodeName = (string) connection["to_node"],
            ToPort = (int) connection["to_port"]
        });

        var pipelineContextStore = new PipelineContextStore() {
            Name = ContextName,
            Nodes = nodes.ToList(),
            Connections = nodeConnections.ToList()
        };

        return pipelineContextStore;
    }

    public void Load(object data)
    {
        if(data == null) {
            return;
        }

        if(data is not PipelineContextStore pipelineContextStore) {
            return;
        }
        
        foreach(var pipelineNodeStore in pipelineContextStore.Nodes) {
            var nodeResourcePath = $"res://addons/Pipelines/Nodes/{pipelineNodeStore.Type}.tscn";
            var pipelineNode = GD.Load<PackedScene>(nodeResourcePath).Instantiate<PipelineNode>();

            pipelineNode.Name = pipelineNodeStore.Name;
            pipelineNode.Load(pipelineNodeStore.Data);
            AddChild(pipelineNode);
            pipelineNode.PositionOffset = new Vector2(pipelineNodeStore.X, pipelineNodeStore.Y);
        }

        foreach(var pipelineConnection in pipelineContextStore.Connections) {
            ConnectNode(pipelineConnection.FromNodeName, pipelineConnection.FromPort, pipelineConnection.ToNodeName, pipelineConnection.ToPort);
        }
    }

}
