using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Reflection;

[Tool]
public partial class BlenderImporter : Node, IInputPipe
{

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

    public enum PhysicsTypes {
        Static,
        Animatable,
        Rigid
    }

    private class BlendNodes {
        public MeshInstance3D Mesh { get; set; }
        public CollisionShape3D CollisionShape { get; set; }
        public PhysicsBody3D PhysicsBody { get; set; }
        public AnimationPlayer AnimationPlayer { get; set; }
    }


    [Export]
    public PhysicsTypes PhysicsType
    {
        get {
            return _physicsType;
        }
        set {
            _physicsType = value;
            _context?.Reprocess();
        }
    }

    [Export]
    public PackedScene Scene {
        get {
            return _scene;
        }
        set {
            _scene = value;
            _context?.Reprocess();
        }
    }

    [Export]
    public Array<NodePath> MeshPipePaths {
        get {
            return _meshPipePaths;
        }
        set {
            BlendNodes blendNodes = null;

            if(IsNodeReady()) {
                try {
                    blendNodes = RetrieveBlendSceneNodes();
                } catch (InvalidOperationException e) {
                    GD.PrintErr(e.Message);
                    return;
                }
            }
            
            var previousMeshPipePaths = _meshPipePaths ?? new Array<NodePath>();
            _meshPipePaths = value ?? new Array<NodePath>();

            var destinationHelper = new DestinationHelper();
            destinationHelper.HandleDestinationChange(new DestinationPropertyInfo() {
                PipeContext = _context,
                Node = this,
                DestinationNodeName = MESH_INSTANCE_3D_NAME,
                PreviousDestinationPaths = previousMeshPipePaths,
                NewDestinationPaths = _meshPipePaths,
                CloneableValue = blendNodes?.Mesh == null ? null : new CloneableNode() { Node = blendNodes.Mesh }
            });
        }
    }
    
    [Export]
    public Array<NodePath> PhysicsBodyPipePaths {
        get {
            return _physicsBodyPipePaths;
        }
        set {
            BlendNodes blendNodes = null;

            if(IsNodeReady()) {
                try {
                    blendNodes = RetrieveBlendSceneNodes();
                } catch (InvalidOperationException e) {
                    GD.PrintErr(e.Message);
                    return;
                }
            }
            
            var previousPhysicsBodyPipePaths = _physicsBodyPipePaths ?? new Array<NodePath>();
            _physicsBodyPipePaths = value ?? new Array<NodePath>();

            var destinationHelper = new DestinationHelper();
            destinationHelper.HandleDestinationChange(new DestinationPropertyInfo() {
                PipeContext = _context,
                Node = this,
                DestinationNodeName = PHYSICS_BODY_3D_NAME,
                PreviousDestinationPaths = previousPhysicsBodyPipePaths,
                NewDestinationPaths = _physicsBodyPipePaths,
                CloneableValue = blendNodes?.PhysicsBody == null ? null : new CloneableNode() { Node = blendNodes.PhysicsBody }
            });
        }
    }
    
    [Export]
    public Array<NodePath> CollisionShapePipePaths {
        get {
            return _collisionShapePipePaths;
        }
        set {
            BlendNodes blendNodes = null;

            if(IsNodeReady()) {
                try {
                    blendNodes = RetrieveBlendSceneNodes();
                } catch (InvalidOperationException e) {
                    GD.PrintErr(e.Message);
                    return;
                }
            }
            
            var previousCollisionShapePipePaths = _collisionShapePipePaths ?? new Array<NodePath>();
            _collisionShapePipePaths = value ?? new Array<NodePath>();

            var destinationHelper = new DestinationHelper();
            destinationHelper.HandleDestinationChange(new DestinationPropertyInfo() {
                PipeContext = _context,
                Node = this,
                DestinationNodeName = COLLISION_SHAPE_3D_NAME,
                PreviousDestinationPaths = previousCollisionShapePipePaths,
                NewDestinationPaths = _collisionShapePipePaths,
                CloneableValue = blendNodes?.CollisionShape == null ? null : new CloneableNode() { Node = blendNodes.CollisionShape }
            });
        }
    }
    
    [Export]
    public Array<NodePath> AnimationPlayerPipePaths {
        get {
            return _animationPlayerPipePaths;
        }
        set {
            BlendNodes blendNodes = null;

            if(IsNodeReady()) {
                try {
                    blendNodes = RetrieveBlendSceneNodes();
                } catch (InvalidOperationException e) {
                    GD.PrintErr(e.Message);
                    return;
                }
            }
            
            var previousAnimationPlayerPipePaths = _animationPlayerPipePaths ?? new Array<NodePath>();
            _animationPlayerPipePaths = value ?? new Array<NodePath>();

            var destinationHelper = new DestinationHelper();
            destinationHelper.HandleDestinationChange(new DestinationPropertyInfo() {
                PipeContext = _context,
                Node = this,
                DestinationNodeName = ANIMATION_PLAYER_NAME,
                PreviousDestinationPaths = previousAnimationPlayerPipePaths,
                NewDestinationPaths = _animationPlayerPipePaths,
                CloneableValue = blendNodes?.AnimationPlayer == null ? null : new CloneableNode() { Node = blendNodes.AnimationPlayer }
            });
        }
    }

    private Array<NodePath> _meshPipePaths;
    private Array<NodePath> _physicsBodyPipePaths;
    private Array<NodePath> _collisionShapePipePaths;
    private Array<NodePath> _animationPlayerPipePaths;

    private PipeContext _context;
    private PackedScene _scene;
    private PhysicsTypes _physicsType;

    private BlendNodes RetrieveBlendSceneNodes() {
        if(_scene == null) {
            throw new InvalidOperationException($"No scene set for import for {Name}");
        }

        // By design, this just gets the first model to avoid any needed intermediate manual process of extracting each body that's needed
        var importedScene = _scene.Instantiate<Node3D>();
        var mesh = importedScene.GetChild<MeshInstance3D>(0);
        var animationPlayer = importedScene.GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

        var staticBody = mesh.GetNodeOrNull<StaticBody3D>(StaticBody3DPath);
        var collisionShape = staticBody?.GetNodeOrNull<CollisionShape3D>(CollisionShape3DPath);


        if(mesh != null) {
            mesh.Name = MESH_INSTANCE_3D_NAME;
            importedScene.RemoveChild(mesh);
            mesh.Owner = null;

            if(staticBody != null) {
                staticBody.Name = PHYSICS_BODY_3D_NAME;
                mesh.RemoveChild(staticBody);
                staticBody.Owner = null;

                if(collisionShape != null) {
                    staticBody.RemoveChild(collisionShape);
                    collisionShape.Owner = null;
                }
            }
        }

        if(animationPlayer != null) {
            importedScene.RemoveChild(animationPlayer);
            animationPlayer.Owner = null;
        }

        importedScene.QueueFree();


        PhysicsBody3D body;

        if(_physicsType == PhysicsTypes.Static) {
            body = staticBody;
        } else if (_physicsType == PhysicsTypes.Animatable) {
            var animatableBody3D = Replace<StaticBody3D, AnimatableBody3D>(staticBody);
            body = animatableBody3D;
        } else {
            body = Replace<PhysicsBody3D, RigidBody3D>(staticBody);
        }

        return new BlendNodes() {
            Mesh = mesh,
            CollisionShape = collisionShape,
            PhysicsBody = body,
            AnimationPlayer = animationPlayer
        };
    }

    private TReplaceNode Replace<TOriganalNode, TReplaceNode>(TOriganalNode original)
            where TOriganalNode : Node
            where TReplaceNode : TOriganalNode {
        
        original.Owner = null;

        var replaceNode = Activator.CreateInstance<TReplaceNode>();

        var originalProps = typeof(TOriganalNode)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !p.Name.StartsWith("Global"))
            .ToList();
        
        foreach(var originalProp in originalProps) {
            originalProp.SetValue(replaceNode, originalProp.GetValue(original));
        }

        foreach(var child in original.GetChildren()) {
            original.RemoveChild(child);
            child.Owner = null;
            replaceNode.AddChild(child);
            child.Owner = replaceNode;
        }
        
        original.Free();

        return replaceNode;
    }

    public void Register()
    {
        BlendNodes blendNodes;
        try {
            blendNodes = RetrieveBlendSceneNodes();
        } catch (InvalidOperationException e) {
            GD.PrintErr(e.Message);
            return;
        }
        
        var mesh = blendNodes.Mesh;
        var collisionShape = blendNodes.CollisionShape;
        var body = blendNodes.PhysicsBody;
        var animationPlayer = blendNodes.AnimationPlayer;

        var meshPipes = Enumerable.Empty<IReceivePipe>();
        var physicsBodyPipes = Enumerable.Empty<IReceivePipe>();
        var collisionShapePipes = Enumerable.Empty<IReceivePipe>();
        var animationPlayerPipes = Enumerable.Empty<IReceivePipe>();

        if(_meshPipePaths != null) {
            meshPipes = _meshPipePaths.Select(p => GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);
        }

        if(_physicsBodyPipePaths != null) {
            physicsBodyPipes = _physicsBodyPipePaths.Select(p => GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);
        }
        
        if(_collisionShapePipePaths != null) {
            collisionShapePipes = _collisionShapePipePaths.Select(p => GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);
        }

        if(_animationPlayerPipePaths != null) {
            animationPlayerPipes = _animationPlayerPipePaths.Select(p => GetNodeOrNull<IReceivePipe>(p)).Where(p => p != null);
        }

        foreach(var meshPipe in meshPipes) {
            meshPipe.Register(_context, MESH_INSTANCE_3D_NAME);
            _context.RegisterPipe(new ValuePipe() { Pipe = meshPipe, CloneableValue = new CloneableNode() { Node = mesh }});
        }

        foreach(var physicsBodyPipe in physicsBodyPipes) {
            physicsBodyPipe.Register(_context, PHYSICS_BODY_3D_NAME);
            _context.RegisterPipe(new ValuePipe() { Pipe = physicsBodyPipe, CloneableValue = new CloneableNode() { Node = body }});
        }

        foreach(var collisionShapePipe in collisionShapePipes) {
            collisionShapePipe.Register(_context, COLLISION_SHAPE_3D_NAME);
            _context.RegisterPipe(new ValuePipe() { Pipe = collisionShapePipe, CloneableValue = new CloneableNode() { Node = collisionShape }});
        }

        foreach(var animationPlayerPipe in animationPlayerPipes) {
            animationPlayerPipe.Register(_context, ANIMATION_PLAYER_NAME);
            _context.RegisterPipe(new ValuePipe() { Pipe = animationPlayerPipe, CloneableValue = new CloneableNode() { Node = animationPlayer }});
        }
    }

    public override void _EnterTree()
    {
        var ascendant = GetParent();

        while(ascendant != null && ascendant is not PipeContext) {
            ascendant = ascendant.GetParent();
        }

        if(ascendant == null) {
            GD.PrintErr($"{Name} is not in a pipe context and cannot be processed");
        }

        var pipeContext = (PipeContext) ascendant;
        _context = pipeContext;
    }



}
