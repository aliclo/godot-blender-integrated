using Godot;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class PipelineGraph : GraphEdit
{

    [Export]
    public NodePath PopupMenu { get; set; }
    private PopupMenu _popupMenu;

    private PackedScene SceneModelNode;
    private PackedScene OutputNode;
    private Vector2 LastMousePos = Vector2.Zero;

    public override void _Ready()
    {
        AddValidConnectionType((int) PipelineNodeTypes.Mesh, (int) PipelineNodeTypes.Any);

        _popupMenu = GetNode<PopupMenu>(PopupMenu);
        SceneModelNode = GD.Load<PackedScene>("res://addons/Pipelines/SceneModelNode.tscn");
        OutputNode = GD.Load<PackedScene>("res://addons/Pipelines/OutputNode.tscn");
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


}
