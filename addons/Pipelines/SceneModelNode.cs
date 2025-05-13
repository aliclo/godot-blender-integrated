using Godot;
using System;

[Tool]
public partial class SceneModelNode : GraphNode
{

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

        var sceneModelPicker = new EditorResourcePicker();
        sceneModelPicker.BaseType = nameof(PackedScene);
        AddChild(sceneModelPicker);

        var physicsBodyOption = new OptionButton();
        physicsBodyOption.AddItem("Static");
        physicsBodyOption.AddItem("Animatable");
        physicsBodyOption.AddItem("Rigid");
        AddChild(physicsBodyOption);
    }


}
