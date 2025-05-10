using Godot;
using Godot.Collections;
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

    private class NodePipes {
        public INodePipe CurrentNodePipe => Pipes[CurrentProgress];
        public object CurrentValue { get; set; }
        public List<INodePipe> Pipes { get; set; }
        public int CurrentProgress { get; set; } = 0;
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
    public Array<NodePath> MeshPipePaths {
        get {
            return _meshPipePaths;
        }
        set {
            var previousMeshPipePaths = _meshPipePaths ?? new Array<NodePath>();
            _meshPipePaths = value ?? new Array<NodePath>();

            var meshPipesPathsToRemove = previousMeshPipePaths.Except(value);
            var meshPipesToRemove = meshPipesPathsToRemove.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var meshPipeToRemove in meshPipesToRemove) {
                meshPipeToRemove.PipeDisconnect();
            }

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

            var meshPipesPathsToAdd = value.Except(previousMeshPipePaths);
            var meshPipesToAdd = meshPipesPathsToAdd.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var meshPipeToAdd in meshPipesToAdd) {
                meshPipeToAdd.PipeConnect();
                meshPipeToAdd.Register(_context, MESH_INSTANCE_3D_NAME);
                meshPipeToAdd.Init();
                var mesh = blendNodes.Mesh.Duplicate();
                meshPipeToAdd.Pipe(mesh);
            }
        }
    }
    
    [Export]
    public Array<NodePath> PhysicsBodyPipePaths {
        get {
            return _physicsBodyPipePaths;
        }
        set {
            var previousPhysicsBodyPipePaths = _physicsBodyPipePaths ?? new Array<NodePath>();
            _physicsBodyPipePaths = value ?? new Array<NodePath>();

            var physicsBodyPipesPathsToRemove = previousPhysicsBodyPipePaths.Except(value);
            var physicsBodyPipesToRemove = physicsBodyPipesPathsToRemove.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var physicsBodyPipeToRemove in physicsBodyPipesToRemove) {
                physicsBodyPipeToRemove.PipeDisconnect();
            }

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

            var physicsBodyPipesPathsToAdd = value.Except(previousPhysicsBodyPipePaths);
            var physicsBodyPipesToAdd = physicsBodyPipesPathsToAdd.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var physicsBodyPipeToAdd in physicsBodyPipesToAdd) {
                physicsBodyPipeToAdd.PipeConnect();
                physicsBodyPipeToAdd.Register(_context, MESH_INSTANCE_3D_NAME);
                physicsBodyPipeToAdd.Init();
                var physicsBody = blendNodes.PhysicsBody.Duplicate();
                physicsBodyPipeToAdd.Pipe(physicsBody);
            }
        }
    }
    
    [Export]
    public Array<NodePath> CollisionShapePipePaths {
        get {
            return _collisionShapePipePaths;
        }
        set {
            var previousCollisionShapePipePaths = _collisionShapePipePaths ?? new Array<NodePath>();
            _collisionShapePipePaths = value ?? new Array<NodePath>();

            var collisionShapePipesPathsToRemove = previousCollisionShapePipePaths.Except(value);
            var collisionShapePipesToRemove = collisionShapePipesPathsToRemove.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var collisionShapePipeToRemove in collisionShapePipesToRemove) {
                collisionShapePipeToRemove.PipeDisconnect();
            }

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

            var collisionShapePipesPathsToAdd = value.Except(previousCollisionShapePipePaths);
            var collisionShapePipesToAdd = collisionShapePipesPathsToAdd.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var collisionShapePipeToAdd in collisionShapePipesToAdd) {
                collisionShapePipeToAdd.PipeConnect();
                collisionShapePipeToAdd.Register(_context, MESH_INSTANCE_3D_NAME);
                collisionShapePipeToAdd.Init();
                var collisionShape = blendNodes.CollisionShape.Duplicate();
                collisionShapePipeToAdd.Pipe(collisionShape);
            }
        }
    }
    
    [Export]
    public Array<NodePath> AnimationPlayerPipePaths {
        get {
            return _animationPlayerPipePaths;
        }
        set {
            var previousAnimationPlayerPipePaths = _animationPlayerPipePaths ?? new Array<NodePath>();
            _animationPlayerPipePaths = value ?? new Array<NodePath>();

            var animationPlayerPipesPathsToRemove = previousAnimationPlayerPipePaths.Except(value);
            var animationPlayerPipesToRemove = animationPlayerPipesPathsToRemove.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var animationPlayerPipeToRemove in animationPlayerPipesToRemove) {
                animationPlayerPipeToRemove.PipeDisconnect();
            }

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

            var animationPlayerPipesPathsToAdd = value.Except(previousAnimationPlayerPipePaths);
            var animationPlayerPipesToAdd = animationPlayerPipesPathsToAdd.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);

            foreach(var animationPlayerPipeToAdd in animationPlayerPipesToAdd) {
                animationPlayerPipeToAdd.PipeConnect();
                animationPlayerPipeToAdd.Register(_context, MESH_INSTANCE_3D_NAME);
                animationPlayerPipeToAdd.Init();
                var animationPlayer = blendNodes.AnimationPlayer.Duplicate();
                animationPlayerPipeToAdd.Pipe(animationPlayer);
            }
        }
    }

    private Array<NodePath> _meshPipePaths;
    private Array<NodePath> _physicsBodyPipePaths;
    private Array<NodePath> _collisionShapePipePaths;
    private Array<NodePath> _animationPlayerPipePaths;

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

        var meshPipes = Enumerable.Empty<INodePipe>();
        var physicsBodyPipes = Enumerable.Empty<INodePipe>();
        var collisionShapePipes = Enumerable.Empty<INodePipe>();
        var animationPlayerPipes = Enumerable.Empty<INodePipe>();

        if(_meshPipePaths != null) {
            meshPipes = _meshPipePaths.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);
        }

        if(_physicsBodyPipePaths != null) {
            physicsBodyPipes = _physicsBodyPipePaths.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);
        }
        
        if(_collisionShapePipePaths != null) {
            collisionShapePipes = _collisionShapePipePaths.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);
        }

        if(_animationPlayerPipePaths != null) {
            animationPlayerPipes = _animationPlayerPipePaths.Select(p => GetNodeOrNull<INodePipe>(p)).Where(p => p != null);
        }

        _context = new PipeContext() {
            RootNode = this,
            ContextData = new System.Collections.Generic.Dictionary<string, object>(),
            OrderOfCreation = new List<NodeOutput>()
        };

        var nodePipesList = new List<NodePipes>();

        if(mesh != null) {
            foreach(var meshPipe in meshPipes) {
                meshPipe.Register(_context, MESH_INSTANCE_3D_NAME);
                var nodePipes = new List<INodePipe>();
                var nextPipeNode = meshPipe;
                while(nextPipeNode != null) {
                    nodePipes.Add(nextPipeNode);
                    nextPipeNode = nextPipeNode.NextPipe;
                }
                nodePipesList.Add(new NodePipes() {
                    CurrentValue = mesh,
                    Pipes = nodePipes,
                    CurrentProgress = 0
                });
            }
        }

        if(body != null) {
            foreach(var physicsBodyPipe in physicsBodyPipes) {
                physicsBodyPipe.Register(_context, PHYSICS_BODY_3D_NAME);
                var nodePipes = new List<INodePipe>();
                var nextPipeNode = physicsBodyPipe;
                while(nextPipeNode != null) {
                    nodePipes.Add(nextPipeNode);
                    nextPipeNode = nextPipeNode.NextPipe;
                }
                nodePipesList.Add(new NodePipes() {
                    CurrentValue = body,
                    Pipes = nodePipes,
                    CurrentProgress = 0
                });
            }
        }

        if(collisionShape != null) {
            foreach(var collisionShapePipe in collisionShapePipes) {
                collisionShapePipe.Register(_context, COLLISION_SHAPE_3D_NAME);
                var nodePipes = new List<INodePipe>();
                var nextPipeNode = collisionShapePipe;
                while(nextPipeNode != null) {
                    nodePipes.Add(nextPipeNode);
                    nextPipeNode = nextPipeNode.NextPipe;
                }
                nodePipesList.Add(new NodePipes() {
                    CurrentValue = collisionShape,
                    Pipes = nodePipes,
                    CurrentProgress = 0
                });
            }
        }

        if(animationPlayer != null) {
            foreach(var animationPlayerPipe in animationPlayerPipes) {
                animationPlayerPipe.Register(_context, ANIMATION_PLAYER_NAME);
                var nodePipes = new List<INodePipe>();
                var nextPipeNode = animationPlayerPipe;
                while(nextPipeNode != null) {
                    nodePipes.Add(nextPipeNode);
                    nextPipeNode = nextPipeNode.NextPipe;
                }
                nodePipesList.Add(new NodePipes() {
                    CurrentValue = animationPlayer,
                    Pipes = nodePipes,
                    CurrentProgress = 0
                });
            }
        }

        foreach(var pipeList in nodePipesList) {
            foreach(var pipe in pipeList.Pipes) {
                pipe.Init();
            }
        }

        // When determining processing, there are two types:
        // - 1. Ones that can be done directly without any dependency as they are done singly
        // - 2. Ones that are done together and need to be done within an order
        // For (2) we can therefore generate this ordering for cleanup and piping
        
        var nodePipesOrdering = new List<NodePipes>();
        var evaluateNodePipes = new List<NodePipes>(nodePipesList);
        while(evaluateNodePipes.Any()) {
            if(evaluateNodePipes.Any(p => _context.OrderOfCreation.Contains(p.CurrentNodePipe))) {
                if(_context.OrderOfCreation.All(ooc => evaluateNodePipes.Select(eoop => eoop.CurrentNodePipe).Contains(ooc))) {
                    var orderOfEvaluation = _context.OrderOfCreation
                        .Select(oocp => evaluateNodePipes.Single(eoop => eoop.CurrentNodePipe == oocp))
                        .ToList();
                    
                    foreach(var p in orderOfEvaluation) {
                        nodePipesOrdering.Add(p);
                        p.CurrentProgress++;
                    }
                } else {
                    var pipesToProcess = evaluateNodePipes
                        .Where(p => !_context.OrderOfCreation.Contains(p.CurrentNodePipe));
                    
                    foreach(var p in pipesToProcess) {
                        nodePipesOrdering.Add(p);
                        p.CurrentProgress++;
                    }
                }
            } else {
                foreach(var p in evaluateNodePipes) {
                    nodePipesOrdering.Add(p);
                    p.CurrentProgress++;
                }
            }

            evaluateNodePipes.RemoveAll(p => p.CurrentProgress == p.Pipes.Count);
        }

        foreach(var p in nodePipesOrdering.Reverse<NodePipes>()) {
            p.CurrentProgress--;
            p.CurrentNodePipe.Clean();
        }

        foreach(var p in nodePipesOrdering) {
            p.CurrentValue = p.CurrentNodePipe.Pipe(p.CurrentValue);
            p.CurrentProgress++;
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
