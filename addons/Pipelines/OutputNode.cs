using Godot;
using System;

[Tool]
public partial class OutputNode : GraphNode
{

    public override void _Ready()
    {
        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0 , (int) PipelineNodeTypes.Any);
        SetSlotColorLeft(0, TypeConnectorColors.ANY);
        var nodeLabel = new Label();
        nodeLabel.Text = "Node";
        AddChild(nodeLabel);

        var outputNodePicker = new TextEdit();
        outputNodePicker.ScrollFitContentHeight = true;
        AddChild(outputNodePicker);
    }

}
