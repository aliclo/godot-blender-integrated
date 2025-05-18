using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

[Tool]
public partial class SceneModelNode : PipelineNode
{

    private class SceneModelNodeStore {
        public string ChosenScene { get; set; }
        public int PhysicsOption { get; set; }
    }

    private EditorResourcePicker _sceneModelPicker;
    private OptionButton _physicsBodyOption;
    private SceneModelNodeStore _sceneModelNodeStore;

    public override object GetData()
    {
        return new SceneModelNodeStore() {
            ChosenScene = _sceneModelPicker.EditedResource?.ResourcePath,
            PhysicsOption = _physicsBodyOption.GetSelectedId()
        };
    }

    public override void Load(object data)
    {
        if(data == null) {
            return;
        }

        if(data is not JsonElement jsonElement) {
            return;
        }

        var sceneModelNodeStore = jsonElement.Deserialize<SceneModelNodeStore>();

        _sceneModelNodeStore = sceneModelNodeStore;
    }

    public override void _Ready()
    {
        SetSlotEnabledRight(0, true);
        SetSlotTypeRight(0 , (int) PipelineNodeTypes.Mesh);
        SetSlotColorRight(0, TypeConnectorColors.MESH);
        var meshLabel = new Label();
        meshLabel.Text = "Mesh";
        AddChild(meshLabel);

        SetSlotEnabledRight(1, true);
        SetSlotTypeRight(1 , (int) PipelineNodeTypes.Any);
        SetSlotColorRight(1, TypeConnectorColors.ANY);
        var physicsBody = new Label();
        physicsBody.Text = "PhysicsBody";
        AddChild(physicsBody);

        SetSlotEnabledRight(2, true);
        SetSlotTypeRight(2 , (int) PipelineNodeTypes.Any);
        SetSlotColorRight(2, TypeConnectorColors.ANY);
        var collisionShapeLabel = new Label();
        collisionShapeLabel.Text = "CollisionShape";
        AddChild(collisionShapeLabel);

        SetSlotEnabledRight(3, true);
        SetSlotTypeRight(3 , (int) PipelineNodeTypes.Any);
        SetSlotColorRight(3, TypeConnectorColors.ANY);
        var animationPlayerLabel = new Label();
        animationPlayerLabel.Text = "AnimationPlayer";
        AddChild(animationPlayerLabel);

        _sceneModelPicker = new EditorResourcePicker();
        _sceneModelPicker.BaseType = nameof(PackedScene);
        AddChild(_sceneModelPicker);

        var physicsBodyOptions = new List<string>() {
            "Static",
            "Animatable",
            "Rigid"
        };

        _physicsBodyOption = new OptionButton();

        foreach(var option in physicsBodyOptions) {
            _physicsBodyOption.AddItem(option);
        }

        AddChild(_physicsBodyOption);

        if(_sceneModelNodeStore != null) {
            if (_sceneModelNodeStore.ChosenScene != null)
            {
                _sceneModelPicker.EditedResource = GD.Load<Resource>(_sceneModelNodeStore.ChosenScene);
            }
            
            _physicsBodyOption.Select(_sceneModelNodeStore.PhysicsOption);
        }
    }


}
