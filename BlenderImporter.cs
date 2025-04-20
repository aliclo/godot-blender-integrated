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
    public NodePath MeshLocation {
        get {
            return _meshPath;
        }
        set {
            if(_meshPath != null && !_meshPath.IsEmpty) {
                var meshParent = GetNode(_meshPath);
                var existingMesh = meshParent.GetNode(MeshInstance3DPath);
                var existingMeshPath = GetPathTo(existingMesh);
                meshParent.RemoveChild(existingMesh);
                existingMesh.QueueFree();
                orderOfCreation.Remove(existingMeshPath);
                //_mesh = null;
            }

            _meshPath = value;

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

            var mesh = blendNodes.Mesh;

            var parentNodePath = value;
            if(!parentNodePath.IsEmpty && mesh != null) {
                var parentNode = GetNode<Node>(parentNodePath);
                parentNode.AddChild(mesh);
                var owner = GetParent()?.Owner ?? GetParent();
                mesh.Owner = owner;
                // _mesh = mesh;
                var meshPath = GetPathTo(mesh);
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNodePath);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(meshPath);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, meshPath);
                }
            }
        }
    }

    [Export]
    public NodePath PhysicsBodyLocation {
        get {
            return _physicsBodyPath;
        }
        set {
            if(_physicsBodyPath != null && !_physicsBodyPath.IsEmpty) {
                var bodyParent = GetNode(_physicsBodyPath);
                var existingPhysicsBody = bodyParent.GetNode(PhysicsBody3DPath);
                var existingPhysicsBodyPath = GetPathTo(existingPhysicsBody);
                bodyParent.RemoveChild(existingPhysicsBody);
                existingPhysicsBody.QueueFree();
                orderOfCreation.Remove(existingPhysicsBodyPath);
                // _physicsBody = null;
            }

            _physicsBodyPath = value;

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
            
            var body = blendNodes.PhysicsBody;

            var parentNodePath = value;
            if(!parentNodePath.IsEmpty && body != null) {
                var parentNode = GetNode<Node>(parentNodePath);
                parentNode.AddChild(body);
                var owner = GetParent()?.Owner ?? GetParent();
                body.Owner = owner;
                // _physicsBody = body;
                var physicsBodyPath = GetPathTo(body);
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNodePath);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(physicsBodyPath);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, physicsBodyPath);
                }
            }
        }
    }

    [Export]
    public NodePath CollisionShapeLocation {
        get {
            return _collisionShapePath;
        }
        set {
            if(_collisionShapePath != null && !_collisionShapePath.IsEmpty) {
                var collisionShapeParent = GetNode(_collisionShapePath);
                var existingCollisionShape = collisionShapeParent.GetNode(CollisionShape3DPath);
                var existingCollisionShapePath = GetPathTo(existingCollisionShape);
                collisionShapeParent.RemoveChild(existingCollisionShape);
                existingCollisionShape.QueueFree();
                orderOfCreation.Remove(existingCollisionShapePath);
                // _collisionShape = null;
            }

            _collisionShapePath = value;

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
            
            var collisionShape = blendNodes.CollisionShape;

            var parentNodePath = value;
            if(!parentNodePath.IsEmpty && collisionShape != null) {
                var parentNode = GetNode<Node>(parentNodePath);
                parentNode.AddChild(collisionShape);
                var owner = GetParent()?.Owner ?? GetParent();
                collisionShape.Owner = owner;
                // _collisionShape = collisionShape;
                var collisionShapePath = GetPathTo(collisionShape);
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNodePath);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(collisionShapePath);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, collisionShapePath);
                }
            }
        }
    }

    [Export]
    public NodePath AnimationPlayerLocation {
        get {
            return _animationPlayerPath;
        }
        set {
            if(_animationPlayerPath != null && !_animationPlayerPath.IsEmpty) {
                var animationPlayerParent = GetNode(_animationPlayerPath);
                var existingAnimationPlayer = animationPlayerParent.GetNode(AnimationPlayerPath);
                var existingAnimationPlayerPath = GetPathTo(existingAnimationPlayer);
                animationPlayerParent.RemoveChild(existingAnimationPlayer);
                existingAnimationPlayer.QueueFree();
                orderOfCreation.Remove(existingAnimationPlayerPath);
                // _animationPlayer = null;
            }

            _animationPlayerPath = value;

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
            
            var animationPlayer = blendNodes.AnimationPlayer;

            var parentNodePath = value;
            if(!parentNodePath.IsEmpty && animationPlayer != null) {
                var parentNode = GetNode<Node>(parentNodePath);
                parentNode.AddChild(animationPlayer);
                var owner = GetParent()?.Owner ?? GetParent();
                animationPlayer.Owner = owner;
                // _animationPlayer = animationPlayer;
                var animationPlayerPath = GetPathTo(animationPlayer);
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNodePath);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(animationPlayerPath);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, animationPlayerPath);
                }
            }
        }
    }

    private List<NodePath> orderOfCreation = new List<NodePath>();

    private bool _completedFirstImport = false;
    private PackedScene _scene;
    private PhysicsTypes _physicsType;

    private NodePath _meshPath;
    private NodePath _physicsBodyPath;
    private NodePath _collisionShapePath;
    private NodePath _animationPlayerPath;

    // private MeshInstance3D _mesh { get; set; }
    // private PhysicsBody3D _physicsBody { get; set; }
    // private CollisionShape3D _collisionShape { get; set; }
    // private AnimationPlayer _animationPlayer { get; set; }

    public override void _Ready()
    {
        GD.Print("Ready!!");
        Reimport();
        _completedFirstImport = true;
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

        var owner = GetParent()?.Owner ?? GetParent();

        // GD.Print(string.Join(", ", orderOfCreation.Select(n => n.Name)));

        var orderOfCreationWithParents = new List<(NodePath Node, NodePath ParentPath)>(orderOfCreation.Count);

        for(int ni = orderOfCreation.Count - 1; ni >= 0; ni--)
        {
            var nodePath = orderOfCreation[ni];
            var node = GetNode(nodePath);
            // GD.Print("Removing ", node.Name);
            var parent = node.GetParent();
            var parentPath = GetPathTo(parent);
            parent.RemoveChild(node);
            node.QueueFree();

            orderOfCreationWithParents.Add((nodePath, parentPath));
        }

        orderOfCreationWithParents.Reverse();

        // GD.Print("Removed all the stuff");
        // GD.Print("New order: ", string.Join(", ", orderOfCreationWithParents.Select(n => n.Node.Name)));

        for(int ni = 0; ni < orderOfCreationWithParents.Count; ni++) {
            var (nodePath, parentPath) = orderOfCreationWithParents[ni];
            var parent = GetNode(parentPath);
            Node node;

            if(nodePath == parentPath + "/" + MeshInstance3DPath) {
                node = mesh;
            } else if (nodePath == parentPath + "/" + PhysicsBody3DPath) {
                node = body;
            } else if (nodePath == parentPath + "/" + CollisionShape3DPath) {
                node = collisionShape;
            } else {
                node = animationPlayer;
            }

            parent.AddChild(node);
            node.Owner = owner;
        }

        // GD.Print("Final order: ", string.Join(", ", orderOfCreation.Select(n => n.Name)));
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
