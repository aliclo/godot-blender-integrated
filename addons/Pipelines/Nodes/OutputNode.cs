using Godot;
using System;
using System.Text.Json;

[Tool]
public partial class OutputNode : PipelineNode
{

    private class OutputNodeStore {
        public string Destination { get; set; }
    }

    private OutputNodeStore _outputNodeStore;
    private TextEdit _outputNodePicker;

    public override object GetData()
    {
        return new OutputNodeStore()
        {
            Destination = _outputNodePicker.Text
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

        var outputNodeStore = jsonElement.Deserialize<OutputNodeStore>();

        _outputNodeStore = outputNodeStore;
    }


    public override void _Ready()
    {
        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0, (int)PipelineNodeTypes.Any);
        SetSlotColorLeft(0, TypeConnectorColors.ANY);
        var nodeLabel = new Label();
        nodeLabel.Text = "Node";
        AddChild(nodeLabel);

        _outputNodePicker = new TextEdit();
        _outputNodePicker.ScrollFitContentHeight = true;
        AddChild(_outputNodePicker);

        if (_outputNodeStore != null)
        {
            _outputNodePicker.Text = _outputNodeStore.Destination;
        }
    }

}
