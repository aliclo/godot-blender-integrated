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
            return _mesh?.GetPath();
        }
        set {
            if(_mesh != null) {
                var meshParent = _mesh.GetParent();
                meshParent.RemoveChild(_mesh);
                _mesh.QueueFree();
                orderOfCreation.Remove(_mesh);
                _mesh = null;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }

            var mesh = blendNodes.Mesh;

            if(!value.IsEmpty && mesh != null) {
                var parentNode = GetNode<Node>(value);
                parentNode.AddChild(mesh);
                var owner = GetParent()?.Owner ?? GetParent();
                mesh.Owner = owner;
                _mesh = mesh;
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNode);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(mesh);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, mesh);
                }
            }
        }
    }

    [Export]
    public NodePath PhysicsBodyLocation {
        get {
            return _physicsBody?.GetPath();
        }
        set {
            if(_physicsBody != null) {
                var bodyParent = _physicsBody.GetParent();
                bodyParent.RemoveChild(_physicsBody);
                _physicsBody.QueueFree();
                orderOfCreation.Remove(_physicsBody);
                _physicsBody = null;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }
            
            var body = blendNodes.PhysicsBody;

            if(!value.IsEmpty && body != null) {
                var parentNode = GetNode<Node>(value);
                parentNode.AddChild(body);
                var owner = GetParent()?.Owner ?? GetParent();
                body.Owner = owner;
                _physicsBody = body;
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNode);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(body);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, body);
                }
            }
        }
    }

    [Export]
    public NodePath CollisionShapeLocation {
        get {
            return _collisionShape?.GetPath();
        }
        set {
            if(_collisionShape != null) {
                var collisionShapeParent = _collisionShape.GetParent();
                collisionShapeParent.RemoveChild(_collisionShape);
                _collisionShape.QueueFree();
                orderOfCreation.Remove(_collisionShape);
                _collisionShape = null;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }
            
            var collisionShape = blendNodes.CollisionShape;

            if(!value.IsEmpty && collisionShape != null) {
                var parentNode = GetNode<Node>(value);
                parentNode.AddChild(collisionShape);
                var owner = GetParent()?.Owner ?? GetParent();
                collisionShape.Owner = owner;
                _collisionShape = collisionShape;
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNode);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(collisionShape);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, collisionShape);
                }
            }
        }
    }

    [Export]
    public NodePath AnimationPlayerLocation {
        get {
            return _animationPlayer?.GetPath();
        }
        set {
            if(_animationPlayer != null) {
                var animationPlayerParent = _animationPlayer.GetParent();
                animationPlayerParent.RemoveChild(_animationPlayer);
                _animationPlayer.QueueFree();
                orderOfCreation.Remove(_animationPlayer);
                _animationPlayer = null;
            }

            BlendNodes blendNodes;
            try {
                blendNodes = RetrieveBlendSceneNodes();
            } catch (InvalidOperationException e) {
                GD.PrintErr(e.Message);
                return;
            }
            
            var animationPlayer = blendNodes.AnimationPlayer;

            if(!value.IsEmpty && animationPlayer != null) {
                var parentNode = GetNode<Node>(value);
                parentNode.AddChild(animationPlayer);
                var owner = GetParent()?.Owner ?? GetParent();
                animationPlayer.Owner = owner;
                _animationPlayer = animationPlayer;
                int indexOfParentNode = orderOfCreation.FindIndex(n => n == parentNode);
                if(indexOfParentNode == -1) {
                    orderOfCreation.Add(animationPlayer);
                } else {
                    orderOfCreation.Insert(indexOfParentNode+1, animationPlayer);
                }
            }
        }
    }

    private List<Node> orderOfCreation = new List<Node>();

    private bool _completedFirstImport = false;
    private PackedScene _scene;
    private PhysicsTypes _physicsType;

    private MeshInstance3D _mesh { get; set; }
    private PhysicsBody3D _physicsBody { get; set; }
    private CollisionShape3D _collisionShape { get; set; }
    private AnimationPlayer _animationPlayer { get; set; }

    public override void _Ready()
    {
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

        GD.Print(string.Join(", ", orderOfCreation.Select(n => n.Name)));

        var orderOfCreationWithParents = new List<(Node Node, NodePath ParentPath)>(orderOfCreation.Count);

        for(int ni = orderOfCreation.Count - 1; ni >= 0; ni--)
        {
            var node = orderOfCreation[ni];
            GD.Print("Removing ", node.Name);
            var parent = node.GetParent();
            parent.RemoveChild(node);
            node.QueueFree();

            if(node == _mesh) {
                _mesh = null;

                if(mesh != null) {
                    _mesh = mesh;
                    orderOfCreationWithParents.Add((_mesh, parent.GetPath()));
                }
            } else if (node == _physicsBody) {
                _physicsBody = null;

                if(body != null) {
                    _physicsBody = body;
                    orderOfCreationWithParents.Add((_physicsBody, parent.GetPath()));
                }
            } else if (node == _collisionShape) {
                _collisionShape = null;

                if(collisionShape != null) {
                    _collisionShape = collisionShape;
                    orderOfCreationWithParents.Add((_collisionShape, parent.GetPath()));
                }
            } else if (node == _animationPlayer) {
                _animationPlayer = null;

                if(animationPlayer != null) {
                    _animationPlayer = animationPlayer;
                    orderOfCreationWithParents.Add((_animationPlayer, parent.GetPath()));
                }
            }
        }

        orderOfCreationWithParents.Reverse();
        orderOfCreation = new List<Node>();

        GD.Print("Removed all the stuff");
        GD.Print("New order: ", string.Join(", ", orderOfCreationWithParents.Select(n => n.Node.Name)));

        for(int ni = 0; ni < orderOfCreationWithParents.Count; ni++) {
            var (node, parentPath) = orderOfCreationWithParents[ni];
            var parent = GetNode(parentPath);
            parent.AddChild(node);
            node.Owner = owner;
            orderOfCreation.Add(node);
        }

        GD.Print("Final order: ", string.Join(", ", orderOfCreation.Select(n => n.Name)));
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
