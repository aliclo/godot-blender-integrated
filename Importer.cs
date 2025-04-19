using Godot;
using System;

[Tool]
public partial class Importer : Node
{

    [Export]
    public PackedScene Scene { get; set; }

    [Export]
    public float Size {
        get { return _size; }
        set {
            _size = value;
            _Reimport();
        }
    }

    private float _size = 1;

    public override void _Ready()
    {
        _Reimport();
    }

    public void _Reimport() {
        var owner = GetParent()?.Owner ?? GetParent();

        if(owner == null) {
            return;
        }

        var importedScene = Scene.Instantiate<Node3D>();
        importedScene.Scale = new Vector3(1,1,1)*_size;
        var origNode = GetNodeOrNull(new NodePath(importedScene.Name));
        
        if(origNode != null) {
            origNode.GetParent().RemoveChild(origNode);
            origNode.QueueFree();
        }

        AddChild(importedScene);
        importedScene.Owner = owner;
    }


}
