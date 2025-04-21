using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[Tool]
public partial class BlenderImporter : Node
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
            if(_completedFirstImport) {
                Reimport();
            }
        }
    }

    [Export]
    public PackedScene Scene {
        get {
            return _scene;
        }
        set {
            _scene = value;
            if(_completedFirstImport) {
                Reimport();
            }
        }
    }

    [Export]
    public NodePath MeshPipePath {
        get {
            return _meshPipePath;
        }
        set {
            var meshPipe = GetNodeOrNull<INodePipe>(_meshPipePath);
            if(meshPipe != null) {
                meshPipe.PipeDisconnect();
            }

            _meshPipePath = value;

            if(!IsNodeReady()) {
                return;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }

            meshPipe = GetNodeOrNull<INodePipe>(_meshPipePath);
            if(meshPipe != null) {
                meshPipe.PipeConnect();
                meshPipe.Init(_context, MESH_INSTANCE_3D_NAME);
                meshPipe.Pipe(blendNodes.Mesh);
            }
        }
    }
    
    [Export]
    public NodePath PhysicsBodyPipePath {
        get {
            return _physicsBodyPipePath;
        }
        set {
            var physicsBodyPipe = GetNodeOrNull<INodePipe>(_physicsBodyPipePath);
            if(physicsBodyPipe != null) {
                physicsBodyPipe.PipeDisconnect();
            }

            _physicsBodyPipePath = value;

            if(!IsNodeReady()) {
                return;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }

            physicsBodyPipe = GetNodeOrNull<INodePipe>(_physicsBodyPipePath);
            if(physicsBodyPipe != null) {
                physicsBodyPipe.PipeConnect();
                physicsBodyPipe.Init(_context, PHYSICS_BODY_3D_NAME);
                physicsBodyPipe.Pipe(blendNodes.PhysicsBody);
            }
        }
    }
    
    [Export]
    public NodePath CollisionShapePipePath {
        get {
            return _collisionShapePipePath;
        }
        set {
            var collisionShapePipe = GetNodeOrNull<INodePipe>(_collisionShapePipePath);
            if(collisionShapePipe != null) {
                collisionShapePipe.PipeDisconnect();
            }

            _collisionShapePipePath = value;

            if(!IsNodeReady()) {
                return;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }

            collisionShapePipe = GetNodeOrNull<INodePipe>(_collisionShapePipePath);
            if(collisionShapePipe != null) {
                collisionShapePipe.PipeConnect();
                collisionShapePipe.Init(_context, COLLISION_SHAPE_3D_NAME);
                collisionShapePipe.Pipe(blendNodes.CollisionShape);
            }
        }
    }
    
    [Export]
    public NodePath AnimationPlayerPipePath {
        get {
            return _animationPlayerPipePath;
        }
        set {
            var animationPlayerPipe = GetNodeOrNull<INodePipe>(_animationPlayerPipePath);
            if(animationPlayerPipe != null) {
                animationPlayerPipe.PipeDisconnect();
            }

            _animationPlayerPipePath = value;

            if(!IsNodeReady()) {
                return;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }

            animationPlayerPipe = GetNodeOrNull<INodePipe>(_animationPlayerPipePath);
            if(animationPlayerPipe != null) {
                animationPlayerPipe.PipeConnect();
                animationPlayerPipe.Init(_context, ANIMATION_PLAYER_NAME);
                animationPlayerPipe.Pipe(blendNodes.AnimationPlayer);
            }
        }
    }

    private NodePath _meshPipePath;
    private NodePath _physicsBodyPipePath;
    private NodePath _collisionShapePipePath;
    private NodePath _animationPlayerPipePath;

    private PipeContext _context;
    private bool _completedFirstImport = false;
    private PackedScene _scene;
    private PhysicsTypes _physicsType;

    public override void _Ready()
    {
        Owner.Ready += () => {
            GD.Print("Ready!!");

            Reimport();
            _completedFirstImport = true;
        };
    }

    public void Reimport() {
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

        var meshPipe = GetNodeOrNull<INodePipe>(_meshPipePath);
        var physicsBodyPipe = GetNodeOrNull<INodePipe>(_physicsBodyPipePath);
        var collisionShapePipe = GetNodeOrNull<INodePipe>(_collisionShapePipePath);
        var animationPlayerPipe = GetNodeOrNull<INodePipe>(_animationPlayerPipePath);

        _context = new PipeContext() {
            RootNode = this,
            OrderOfCreation = new List<NodeOutput>()
        };

        if(meshPipe != null) {
            meshPipe.Init(_context, MESH_INSTANCE_3D_NAME);
        }

        if(physicsBodyPipe != null) {
            physicsBodyPipe.Init(_context, PHYSICS_BODY_3D_NAME);
        }

        if(collisionShapePipe != null) {
            collisionShapePipe.Init(_context, COLLISION_SHAPE_3D_NAME);
        }

        if(animationPlayerPipe != null) {
            animationPlayerPipe.Init(_context, ANIMATION_PLAYER_NAME);
        }
        
        for(int oi = _context.OrderOfCreation.Count-1; oi >= 0; oi--) {
            var outputNode = _context.OrderOfCreation[oi];
            outputNode.Clean();
        }

        for(int oi = 0; oi < _context.OrderOfCreation.Count; oi++) {
            var outputNode = _context.OrderOfCreation[oi];
            
            if(outputNode == meshPipe) {
                outputNode.Pipe(mesh);
            } else if (outputNode == physicsBodyPipe) {
                outputNode.Pipe(body);
            } else if (outputNode == collisionShapePipe) {
                outputNode.Pipe(collisionShape);
            } else {
                outputNode.Pipe(animationPlayer);
            }
        }
    }

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

}
