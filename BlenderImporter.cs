using Godot;
using System;
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

    private PhysicsTypes _physicsType;

    [Export]
    public PhysicsTypes PhysicsType
    {
        get {
            return _physicsType;
        }
        set {
            _physicsType = value;
            Reimport();
        }
    }

    [Export]
    public PackedScene Scene {
        get {
            return _scene;
        }
        set {
            _scene = value;
            Reimport();
        }
    }

    [Export]
    public NodePath MeshLocation {
        get {
            return _meshLocation;
        }
        set {
            if(_meshLocation != null && !_meshLocation.IsEmpty) {
                var location = _meshLocation + "/" + MeshInstance3DPath;
                if (HasNode(location)) {
                    Remove(location);
                }
            }
            _meshLocation = value;
            Reimport();
        }
    }

    [Export]
    public NodePath PhysicsBodyLocation {
        get {
            return _physicsBodyLocation;
        }
        set {
            if(_physicsBodyLocation != null && !_physicsBodyLocation.IsEmpty) {
                var location = _physicsBodyLocation + "/" + PhysicsBody3DPath;
                if (HasNode(location)) {
                    Remove(location);
                }
            }
            _physicsBodyLocation = value;
            Reimport();
        }
    }

    [Export]
    public NodePath CollisionShapeLocation {
        get {
            return _collisionShapeLocation;
        }
        set {
            if(_collisionShapeLocation != null && !_collisionShapeLocation.IsEmpty) {
                var location = _collisionShapeLocation + "/" + CollisionShape3DPath;
                if (HasNode(location)) {
                    Remove(location);
                }
            }
            _collisionShapeLocation = value;
            Reimport();
        }
    }

    [Export]
    public NodePath AnimationPlayerLocation {
        get {
            return _animationPlayerLocation;
        }
        set {
            if(_animationPlayerLocation != null && !_animationPlayerLocation.IsEmpty) {
                var location = _animationPlayerLocation + "/" + AnimationPlayerPath;
                if (HasNode(location)) {
                    Remove(location);
                }
            }
            _animationPlayerLocation = value;
            Reimport();
        }
    }

    private PackedScene _scene;
    private NodePath _meshLocation { get; set; }
    private NodePath _physicsBodyLocation { get; set; }
    private NodePath _collisionShapeLocation { get; set; }
    private NodePath _animationPlayerLocation { get; set; }

    public override void _Ready()
    {
        Reimport();
    }

    public void Reimport() {
        if(_meshLocation != null && !_meshLocation.IsEmpty) {
            var location = _meshLocation + "/" + MeshInstance3DPath;
            if (HasNode(location)) {
                Remove(location);
            }
        }

        if(_physicsBodyLocation != null && !_physicsBodyLocation.IsEmpty) {
            var location = _physicsBodyLocation + "/" + PhysicsBody3DPath;
            GD.Print($"Checking location {location}");
            if (HasNode(location)) {
                GD.Print("It's there!");
                Remove(location);
            }
        }

        if(_collisionShapeLocation != null && !_collisionShapeLocation.IsEmpty) {
            var location = _collisionShapeLocation + "/" + CollisionShape3DPath;
            if (HasNode(location)) {
                Remove(location);
            }
        }

        if(_animationPlayerLocation != null && !_animationPlayerLocation.IsEmpty) {
            var location = _animationPlayerLocation + "/" + AnimationPlayerPath;
            if (HasNode(location)) {
                Remove(location);
            }
        }

        if(_scene == null) {
            GD.PrintErr($"No scene set for import for {Name}");
            return;
        }

        // By design, this just gets the first model to avoid any needed intermediate manual process of extracting each body that's needed
        var importedScene = _scene.Instantiate<Node3D>();
        var mesh = importedScene.GetChild<MeshInstance3D>(0);
        var animationPlayer = importedScene.GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

        var staticBody = mesh.GetNodeOrNull<StaticBody3D>(StaticBody3DPath);
        var collisionShape = staticBody?.GetNodeOrNull<CollisionShape3D>(CollisionShape3DPath);

        if(mesh != null) {
            importedScene.RemoveChild(mesh);

            if(staticBody != null) {
                mesh.RemoveChild(staticBody);

                if(collisionShape != null) {
                    staticBody.RemoveChild(collisionShape);
                }
            }
        }

        if(animationPlayer != null) {
            importedScene.RemoveChild(animationPlayer);
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

        var owner = GetParent()?.Owner ?? GetParent();

        if(mesh != null && _meshLocation != null && !_meshLocation.IsEmpty) {
            var parentNode = GetNode<Node>(_meshLocation);
            mesh.Name = MESH_INSTANCE_3D_NAME;
            mesh.Owner = null;
            parentNode.AddChild(mesh);
            mesh.Owner = owner;
        }

        if(body != null && _physicsBodyLocation != null && !_physicsBodyLocation.IsEmpty) {
            var parentNode = GetNode<Node>(_physicsBodyLocation);
            body.Name = PHYSICS_BODY_3D_NAME;
            body.Owner = null;
            parentNode.AddChild(body);
            body.Owner = owner;
            GD.Print("Adding physics body");
        }

        if(collisionShape != null && _collisionShapeLocation != null && !_collisionShapeLocation.IsEmpty) {
            var parentNode = GetNode<Node>(_collisionShapeLocation);
            collisionShape.Owner = null;
            parentNode.AddChild(collisionShape);
            collisionShape.Owner = owner;
        }

        if(animationPlayer != null && _animationPlayerLocation != null && !_animationPlayerLocation.IsEmpty) {
            var parentNode = GetNode<Node>(_animationPlayerLocation);
            animationPlayer.Owner = null;
            parentNode.AddChild(animationPlayer);
            animationPlayer.Owner = owner;
        }
    }

    private void Remove(NodePath nodePath) {
        var node = GetNodeOrNull(nodePath);

        if(node != null) {
            GD.Print("Removing: ", node);
            node.GetParent().RemoveChild(node);
            node.QueueFree();
        }
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
