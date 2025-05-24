using Godot;

[Tool]
public partial class OutputNode : PipelineNode
{

    private partial class OutputNodeStore: GodotObject {
        [Export]
        public string Destination { get; set; }
    }

    private OutputNodeStore _outputNodeStore;
    private TextEdit _outputNodePicker;

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new OutputNodeStore()
        {
            Destination = _outputNodePicker.Text
        }); 
    }

    public override void Load(Variant data)
    {
        _outputNodeStore = GodotJsonParser.FromJsonType<OutputNodeStore>(data);
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
