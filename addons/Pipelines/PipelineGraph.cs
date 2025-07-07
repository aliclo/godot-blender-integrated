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

    private readonly PipeMapper _pipeMapper = new PipeMapper();
    private PipeContext _context { get; set; }
    private PopupMenu _popupMenu;

    private PackedScene _sceneModelNode;
    private PackedScene _edgifyModelNode;
    private PackedScene _outputNode;
    private Vector2 _lastMousePos = Vector2.Zero;

    public override void _Ready()
    {
        AddValidConnectionType((int)PipelineNodeTypes.Mesh, (int)PipelineNodeTypes.Any);

        _popupMenu = GetNode<PopupMenu>(PopupMenu);
        _sceneModelNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/SceneModelNode.tscn");
        _edgifyModelNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/EdgifyNode.tscn");
        _outputNode = GD.Load<PackedScene>("res://addons/Pipelines/Nodes/OutputNode.tscn");
        ConnectionRequest += HandleConnectionRequest;
        DisconnectionRequest += HandleDisconnectionRequest;
        DeleteNodesRequest += HandleDeleteNodesRequest;
    }

    public void OnLoadContext(PipeContext pipeContext)
    {
        _context = pipeContext;
        foreach (var pipelineNode in _context.PipelineNodeDict.Values)
        {
            AddChild(pipelineNode);
            for (int index = 0; index < pipelineNode.NodeConnections.Count; index++)
            {
                var portConnections = pipelineNode.NodeConnections[index];
                foreach (var portConnection in portConnections)
                {
                    // Eventually we can support receiver nodes with multiple input ports
                    ConnectNode(pipelineNode.Name, index, portConnection.Name, 0);
                }
            }
        }
    }

    public void Cleanup()
    {
        var existingConnections = GetConnectionList().Select(_pipeMapper.Map);
        foreach (var connection in existingConnections)
        {
            DisconnectNode(connection.FromNodeName, connection.FromPort, connection.ToNodeName, connection.ToPort);
        }

        foreach (var pipelineNode in _context.PipelineNodeDict.Values)
        {
            RemoveChild(pipelineNode);
            pipelineNode.Owner = null;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton inputMouseButton && inputMouseButton.IsPressed() && inputMouseButton.ButtonIndex == MouseButton.Right)
        {
            var mousePos = GetGlobalMousePosition();
            _lastMousePos = GetLocalMousePosition();
            _popupMenu.Popup(new Rect2I((int)mousePos.X, (int)mousePos.Y, _popupMenu.Size.X, _popupMenu.Size.Y));
        }
    }

    public void HandleConnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort)
    {
        var connected = GetConnectionList().SingleOrDefault(c => (string)c["to_node"] == toNodeName && (long)c["to_port"] == toPort);

        if (connected == null)
        {
            UndoRedo.CreateAction($"Connect {fromNodeName} to {toNodeName}");

            UndoRedo.AddDoMethod(this, MethodName.ConnectPipelineNode, fromNodeName, fromPort, toNodeName, toPort);

            UndoRedo.AddUndoMethod(this, MethodName.DisconnectPipelineNode, fromNodeName, fromPort, toNodeName, toPort);

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

        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    private void ReplaceNodeConnection(string oldFromNodeName, int oldFromPort, string newFromNodeName, int newFromPort, string toNodeName, int toPort)
    {
        DisconnectPipelineNode(oldFromNodeName, oldFromPort, toNodeName, toPort);
        ConnectPipelineNode(newFromNodeName, newFromPort, toNodeName, toPort);
    }

    private void ConnectPipelineNode(string fromNodeName, int fromPort, string toNodeName, int toPort)
    {
        var fromNode = _context.PipelineNodeDict[fromNodeName];
        var toNode = _context.PipelineNodeDict[toNodeName];

        if (toNode is not IReceivePipe receivePipe)
        {
            return;
        }

        ConnectNode(fromNode.Name, fromPort, toNode.Name, toPort);
        fromNode.Connect(fromPort, new List<IReceivePipe>() { receivePipe });
    }

    private void DisconnectPipelineNode(string fromNodeName, int fromPort, string toNodeName, int toPort)
    {
        var fromNode = _context.PipelineNodeDict[fromNodeName];
        var toNode = _context.PipelineNodeDict[toNodeName];

        if (toNode is not IReceivePipe receivePipe)
        {
            return;
        }

        DisconnectNode(fromNode.Name, fromPort, toNode.Name, toPort);
        fromNode.Disconnect(fromPort, new List<IReceivePipe>() { receivePipe });
    }

    public void RightClickMenuChosen(int id)
    {
        PackedScene packedSceneToCreate = null;

        switch (id)
        {
            case (int)RightClickMenuItems.AddSceneModelNode:
                packedSceneToCreate = _sceneModelNode;
                break;
            case (int)RightClickMenuItems.AddEdgifyNode:
                packedSceneToCreate = _edgifyModelNode;
                break;
            case (int)RightClickMenuItems.AddOutputNode:
                packedSceneToCreate = _outputNode;
                break;
        }

        if (packedSceneToCreate == null)
        {
            return;
        }

        var graphNode = packedSceneToCreate.Instantiate<PipelineNode>();
        graphNode.Name = EnsureUniqueNodeName(graphNode.Name);
        graphNode.PositionOffset = (_lastMousePos + ScrollOffset) / Zoom;
        graphNode.Init(_context);

        UndoRedo.CreateAction($"Add {graphNode.GetType().Name} node");
        UndoRedo.AddDoMethod(this, MethodName.AddPipelineNode, graphNode);
        UndoRedo.AddUndoMethod(this, MethodName.RemovePipelineNode, graphNode);
        UndoRedo.CommitAction();
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    private void AddPipelineNode(PipelineNode pipelineNode)
    {
        AddChild(pipelineNode);
        _context.PipelineNodeDict[pipelineNode.Name] = pipelineNode;
    }

    private void RemovePipelineNode(PipelineNode pipelineNode)
    {
        RemoveChild(pipelineNode);
        _context.PipelineNodeDict.Remove(pipelineNode.Name);
    }

    private string EnsureUniqueNodeName(string name)
    {
        if (GetNodeOrNull(name) == null)
        {
            return name;
        }

        int counter = 1;
        string uniqueName;

        do
        {
            uniqueName = name + counter;
            counter++;
        } while (GetNodeOrNull(uniqueName) != null);

        return uniqueName;
    }

    private void HandleDisconnectionRequest(StringName fromNodeName, long fromPort, StringName toNodeName, long toPort)
    {
        UndoRedo.CreateAction($"Disconnect {fromNodeName} from {toNodeName}");

        UndoRedo.AddDoMethod(this, MethodName.DisconnectPipelineNode, fromNodeName, fromPort, toNodeName, toPort);

        UndoRedo.AddUndoMethod(this, MethodName.ConnectPipelineNode, fromNodeName, fromPort, toNodeName, toPort);

        UndoRedo.CommitAction();
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    private void HandleDeleteNodesRequest(Array<StringName> nodeNames)
    {
        var nodes = new Array<PipelineNode>(nodeNames
            .Select(name => _context.PipelineNodeDict[(string)name]));

        var nodeStore = new Array<PipelineNodeStore>(nodes
            .Select(_pipeMapper.Map));

        var connections = new Array<PipelineConnectionStore>(GetConnectionList()
            .Select(_pipeMapper.Map)
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
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    private void DeleteNodes(Array<PipelineNode> nodes)
    {
        foreach (var node in nodes)
        {
            _context.PipelineNodeDict.Remove(node.Name);
            RemoveChild(node);
            node.QueueFree();
        }
    }

    private void AddNodesAndConnections(PipelineContextStore pipelineContextStore)
    {
        _context.AddNodesAndConnections(pipelineContextStore);
    }

}
