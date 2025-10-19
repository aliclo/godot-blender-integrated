#if TOOLS
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class SceneModelNode : PipelineNode
{

    public enum Connections
    {
        Mesh,
        CollisionShape,
        AnimationPlayer
    }

    private class BlendNodes
    {
        public MeshInstance3D Mesh { get; set; }
        public CollisionShape3D CollisionShape { get; set; }
        public AnimationPlayer AnimationPlayer { get; set; }
    }

    public partial class SceneModelNodeStore : GodotObject
    {
        [Export]
        public string ChosenScene { get; set; }
    }

    private const string MESH_INSTANCE_3D_NAME = nameof(MeshInstance3D);
    private const string STATIC_BODY_3D_NAME = nameof(StaticBody3D);
    private const string PHYSICS_BODY_3D_NAME = nameof(PhysicsBody3D);
    private const string COLLISION_SHAPE_3D_NAME = nameof(CollisionShape3D);
    private const string ANIMATION_PLAYER_NAME = nameof(AnimationPlayer);

    private static readonly NodePath MeshInstance3DPath = new NodePath(MESH_INSTANCE_3D_NAME);
    private static readonly NodePath StaticBody3DPath = new NodePath(STATIC_BODY_3D_NAME);
    private static readonly NodePath PhysicsBody3DPath = new NodePath(PHYSICS_BODY_3D_NAME);
    private static readonly NodePath CollisionShape3DPath = new NodePath(COLLISION_SHAPE_3D_NAME);
    private static readonly NodePath AnimationPlayerPath = new NodePath(ANIMATION_PLAYER_NAME);

    // TODO: Use this from the context instead of having to provide it to the context
    private static readonly Array<Array<string>> MESH_TOUCHED_PROPERTIES = new Array<Array<string>>() {
        new Array<string> { nameof(MeshInstance3D.Mesh).ToLower() }
    };

    private static readonly Array<Array<string>> MESH_UNTOUCHED_PROPERTIES = new Array<Array<string>>() { };

    private static readonly Array<Array<string>> COLLISION_SHAPE_TOUCHED_PROPERTIES = new Array<Array<string>>() {
        new Array<string> { nameof(CollisionShape3D.Shape).ToLower() }
    };

    private static readonly Array<Array<string>> COLLISION_SHAPE_UNTOUCHED_PROPERTIES = new Array<Array<string>>() { };

    private static readonly Array<Array<string>> ANIMATION_PLAYER_TOUCHED_PROPERTIES = new Array<Array<string>>() { };

    private PipeContext _context;
    private EditorResourcePicker _sceneModelPicker;
    private SceneModelNodeStore _sceneModelNodeStore;

    private Array<PipelineNode> _meshPipes;
    private Array<PipelineNode> _collisionShapePipes;
    private Array<PipelineNode> _animationPlayerPipes;
    private ICloneablePipeValue _meshCloneablePipeValue;
    private ICloneablePipeValue _collisionShapeCloneablePipeValue;
    private ICloneablePipeValue _animationPlayerCloneablePipeValue;

    private Array<Array<PipelineNode>> _nodeConnections;
    public override Array<Array<PipelineNode>> NodeConnections => _nodeConnections;
    public override Array<NodePath> NodeDependencies => new Array<NodePath>();

    public override Array<PipelineNode> NextPipes => [];

    public override void _Ready()
    {
        PipelinesSingleton.Singleton(s => { GD.Print("Executing signal registration for SceneModelNode!"); s.SceneImportUpdated += SceneUpdated; });
    }


    public override void AddConnection(int index, Array<PipelineNode> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);

        var connection = (Connections)index;
        switch (connection)
        {
            case Connections.Mesh:
                AddMeshConnections(receivePipes);
                break;
            case Connections.CollisionShape:
                AddCollisionShapeConnections(receivePipes);
                break;
            case Connections.AnimationPlayer:
                AddAnimationPlayerConnections(receivePipes);
                break;
        }
    }

    public override void Connect(int index, Array<PipelineNode> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);

        var connection = (Connections)index;
        switch (connection)
        {
            case Connections.Mesh:
                ConnectMesh(receivePipes);
                break;
            case Connections.CollisionShape:
                ConnectCollisionShape(receivePipes);
                break;
            case Connections.AnimationPlayer:
                ConnectAnimationPlayer(receivePipes);
                break;
        }
    }

    public override void Disconnect(int index, Array<PipelineNode> receivePipes)
    {
        var nodePortConnections = _nodeConnections[index];
        foreach (var receivePipe in receivePipes)
        {
            nodePortConnections.Remove(receivePipe);
        }

        var connection = (Connections)index;
        switch (connection)
        {
            case Connections.Mesh:
                DisconnectMesh(receivePipes);
                break;
            case Connections.CollisionShape:
                DisconnectCollisionShape(receivePipes);
                break;
            case Connections.AnimationPlayer:
                DisconnectAnimationPlayer(receivePipes);
                break;
        }
    }

    private void AddMeshConnections(IEnumerable<PipelineNode> receivePipes)
    {
        _meshPipes.AddRange(receivePipes);
    }

    private void AddCollisionShapeConnections(IEnumerable<PipelineNode> receivePipes)
    {
        _collisionShapePipes.AddRange(receivePipes);
    }

    private void AddAnimationPlayerConnections(IEnumerable<PipelineNode> receivePipes)
    {
        _animationPlayerPipes.AddRange(receivePipes);
    }

    private void ConnectMesh(IList<PipelineNode> receivePipes)
    {
        _meshPipes.AddRange(receivePipes);

        if (_sceneModelPicker.EditedResource != null && _meshCloneablePipeValue != null)
        {
            var destinationHelper = new DestinationHelper();
            destinationHelper.AddReceivePipes(_context, receivePipes, _meshCloneablePipeValue);
        }
    }

    private void DisconnectMesh(IList<PipelineNode> receivePipes)
    {
        foreach (var receivePipe in receivePipes)
        {
            _meshPipes.Remove(receivePipe);
        }

        var destinationHelper = new DestinationHelper();
        destinationHelper.RemoveReceivePipes(receivePipes);
    }

    private void ConnectCollisionShape(IList<PipelineNode> receivePipes)
    {
        _collisionShapePipes.AddRange(receivePipes);

        if (_sceneModelPicker.EditedResource != null && _collisionShapeCloneablePipeValue != null)
        {
            var destinationHelper = new DestinationHelper();
            destinationHelper.AddReceivePipes(_context, receivePipes, _collisionShapeCloneablePipeValue);
        }
    }

    private void DisconnectCollisionShape(IList<PipelineNode> receivePipes)
    {
        foreach (var receivePipe in receivePipes)
        {
            _collisionShapePipes.Remove(receivePipe);
        }

        var destinationHelper = new DestinationHelper();
        destinationHelper.RemoveReceivePipes(receivePipes);
    }

    private void ConnectAnimationPlayer(IList<PipelineNode> receivePipes)
    {
        _animationPlayerPipes.AddRange(receivePipes);

        if (_sceneModelPicker.EditedResource != null && _animationPlayerCloneablePipeValue != null)
        {
            var destinationHelper = new DestinationHelper();
            destinationHelper.AddReceivePipes(_context, receivePipes, _animationPlayerCloneablePipeValue);
        }
    }

    private void DisconnectAnimationPlayer(IList<PipelineNode> receivePipes)
    {
        foreach (var receivePipe in receivePipes)
        {
            _animationPlayerPipes.Remove(receivePipe);
        }

        var destinationHelper = new DestinationHelper();
        destinationHelper.RemoveReceivePipes(receivePipes);
    }

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new SceneModelNodeStore()
        {
            ChosenScene = _sceneModelPicker.EditedResource?.ResourcePath
        });
    }

    public override void Load(Variant data)
    {
        _sceneModelNodeStore = GodotJsonParser.FromJsonType<SceneModelNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;

        _meshPipes = new Array<PipelineNode>();
        _collisionShapePipes = new Array<PipelineNode>();
        _animationPlayerPipes = new Array<PipelineNode>();

        _nodeConnections = new Array<Array<PipelineNode>>(Enumerable.Range(0, 3)
            .Select(n => new Array<PipelineNode>()));

        SetSlotEnabledRight(0, true);
        SetSlotTypeRight(0, (int)PipelineNodeTypes.Mesh);
        SetSlotColorRight(0, TypeConnectorColors.MESH);
        var meshLabel = new Label();
        meshLabel.Text = "Mesh";
        AddChild(meshLabel);

        SetSlotEnabledRight(1, true);
        SetSlotTypeRight(1, (int)PipelineNodeTypes.Any);
        SetSlotColorRight(1, TypeConnectorColors.ANY);
        var collisionShapeLabel = new Label();
        collisionShapeLabel.Text = "CollisionShape";
        AddChild(collisionShapeLabel);

        SetSlotEnabledRight(2, true);
        SetSlotTypeRight(2, (int)PipelineNodeTypes.Any);
        SetSlotColorRight(2, TypeConnectorColors.ANY);
        var animationPlayerLabel = new Label();
        animationPlayerLabel.Text = "AnimationPlayer";
        AddChild(animationPlayerLabel);

        _sceneModelPicker = new EditorResourcePicker();
        _sceneModelPicker.BaseType = nameof(PackedScene);
        _sceneModelPicker.ResourceChanged += SceneChanged;
        AddChild(_sceneModelPicker);

        if (_sceneModelNodeStore != null)
        {
            if (_sceneModelNodeStore.ChosenScene != null)
            {
                _sceneModelPicker.EditedResource = GD.Load<Resource>(_sceneModelNodeStore.ChosenScene);
            }
        }
    }

    public override void Register()
    {
        BlendNodes blendNodes;
        try
        {
            blendNodes = RetrieveBlendSceneNodes();
        }
        catch (InvalidOperationException e)
        {
            GD.PrintErr(e.Message);
            return;
        }

        var mesh = blendNodes.Mesh;
        var collisionShape = blendNodes.CollisionShape;
        var animationPlayer = blendNodes.AnimationPlayer;

        if (mesh != null)
        {
            var meshPipeValue = new PipeValue() { Value = mesh, TouchedProperties = MESH_TOUCHED_PROPERTIES, UntouchedProperties = MESH_UNTOUCHED_PROPERTIES };
            _meshCloneablePipeValue = new CloneablePipeValue() { PipeValue = meshPipeValue };

            foreach (var meshPipe in _meshPipes)
            {
                _context.RegisterPipe(new ValuePipe() { Pipe = meshPipe, CloneablePipeValue = _meshCloneablePipeValue });
            }
        }

        if (collisionShape != null)
        {
            var collisionShapePipeValue = new PipeValue() { Value = collisionShape, TouchedProperties = COLLISION_SHAPE_TOUCHED_PROPERTIES, UntouchedProperties = COLLISION_SHAPE_UNTOUCHED_PROPERTIES };
            _collisionShapeCloneablePipeValue = new CloneablePipeValue() { PipeValue = collisionShapePipeValue };

            foreach (var collisionShapePipe in _collisionShapePipes)
            {
                _context.RegisterPipe(new ValuePipe() { Pipe = collisionShapePipe, CloneablePipeValue = _collisionShapeCloneablePipeValue });
            }
        }

        if (animationPlayer != null)
        {
            var animationPlayerUntouchedProperties = GetUntouchedProps(animationPlayer);
            var animationPlayerPipeValue = new PipeValue() { Value = animationPlayer, TouchedProperties = ANIMATION_PLAYER_TOUCHED_PROPERTIES, UntouchedProperties = animationPlayerUntouchedProperties };
            _animationPlayerCloneablePipeValue = new CloneablePipeValue() { PipeValue = animationPlayerPipeValue };

            foreach (var animationPlayerPipe in _animationPlayerPipes)
            {
                _context.RegisterPipe(new ValuePipe() { Pipe = animationPlayerPipe, CloneablePipeValue = _animationPlayerCloneablePipeValue });
            }
        }
    }

    private BlendNodes RetrieveBlendSceneNodes()
    {
        // Get resource path and load without using cache
        var chosenResource = _sceneModelPicker.EditedResource;
        var resource = ResourceLoader.Load(chosenResource.ResourcePath, cacheMode: ResourceLoader.CacheMode.Replace);
        if (resource == null)
        {
            throw new InvalidOperationException($"No scene set for import for {Name}");
        }

        if (resource is not PackedScene scene)
        {
            throw new InvalidOperationException($"Chosen resource is not a scene for {Name}");
        }

        // By design, this just gets the first model to avoid any needed intermediate manual process of extracting each body that's needed
        var importedScene = scene.Instantiate<Node3D>();
        var mesh = importedScene.GetChild<MeshInstance3D>(0);
        var animationPlayer = importedScene.GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

        var staticBody = mesh.GetNodeOrNull<StaticBody3D>(StaticBody3DPath);
        var collisionShape = staticBody?.GetNodeOrNull<CollisionShape3D>(CollisionShape3DPath);


        if (mesh != null)
        {
            mesh.Name = MESH_INSTANCE_3D_NAME;
            importedScene.RemoveChild(mesh);
            mesh.Owner = null;

            if (staticBody != null)
            {
                staticBody.Name = PHYSICS_BODY_3D_NAME;
                mesh.RemoveChild(staticBody);
                staticBody.Owner = null;

                if (collisionShape != null)
                {
                    staticBody.RemoveChild(collisionShape);
                    collisionShape.Owner = null;
                }
            }
        }

        if (animationPlayer != null)
        {
            importedScene.RemoveChild(animationPlayer);
            animationPlayer.Owner = null;

            var animationLibraries = new List<AnimationLibrary>();

            foreach (var animationLibraryName in animationPlayer.GetAnimationLibraryList())
            {
                var animationLibrary = (AnimationLibrary)animationPlayer
                    .GetAnimationLibrary(animationLibraryName)
                    .Duplicate(true);

                foreach (var animationName in animationLibrary.GetAnimationList())
                {
                    var animation = animationLibrary.GetAnimation(animationName);

                    if (animation.GetTrackCount() > 0)
                    {
                        for (int ti = 0; ti < animation.GetTrackCount(); ti++)
                        {
                            animation.TrackSetImported(ti, false);
                        }
                    }
                    else
                    {
                        GD.PrintErr($"Animation {animation} has no tracks from scene {scene.ResourceName} for {nameof(SceneModelNode)} node {Name}");
                    }
                }

                animationPlayer.RemoveAnimationLibrary(animationLibraryName);
                animationPlayer.AddAnimationLibrary(animationLibraryName, animationLibrary);
            }
        }

        importedScene.QueueFree();

        return new BlendNodes()
        {
            Mesh = mesh,
            CollisionShape = collisionShape,
            AnimationPlayer = animationPlayer
        };
    }

    private Array<Array<string>> GetUntouchedProps(AnimationPlayer animationPlayer)
    {
        var untouchedProps = new Array<Array<string>>();

        var animationLibraryNames = animationPlayer.GetAnimationLibraryList();
        foreach (var animationLibraryName in animationLibraryNames)
        {
            var animationLibrary = animationPlayer.GetAnimationLibrary(animationLibraryName);
            var animationNames = animationLibrary.GetAnimationList();
            foreach (var animationName in animationNames)
            {
                untouchedProps.Add(["libraries", animationLibraryName, "_data", animationName, "loop_mode"]);
            }
        }

        untouchedProps.Add(["autoplay"]);
        untouchedProps.Add(["callback_mode_process"]);
        untouchedProps.Add(["callback_mode_method"]);
        untouchedProps.Add(["callback_mode_discrete"]);

        return untouchedProps;
    }

    private void SceneChanged(Resource scene)
    {
        _context?.Reprocess();
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    private void SceneUpdated(string fileName)
    {
        if (fileName == _sceneModelPicker.EditedResource.ResourcePath)
        {
            _context?.Reprocess();
        }
    }

    public override void _ExitTree()
    {
        _sceneModelPicker.ResourceChanged -= SceneChanged;
    }


    protected override void Dispose(bool disposing)
    {
        PipelinesSingleton.Singleton(s => s.SceneImportUpdated -= SceneUpdated);
        base.Dispose(disposing);
    }

    public override ICloneablePipeValue PipeValue(ICloneablePipeValue pipeValue)
    {
        GD.PrintErr($"{nameof(PipeValue)} unused, {nameof(SceneModelNode)} is an input node");
        return null;
    }

    public override void Clean()
    {
        GD.PrintErr($"{nameof(Clean)} unused, {nameof(SceneModelNode)} is an input node");
    }

    public override void PipeDisconnect()
    {
        GD.PrintErr($"{nameof(PipeDisconnect)} unused, {nameof(SceneModelNode)} is an input node");
    }

}
#endif