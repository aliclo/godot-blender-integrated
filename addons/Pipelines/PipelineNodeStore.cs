using Godot;

public partial class PipelineNodeStore: GodotObject {

    [Export]
    public string Name { get; set; }
    [Export]
    public string Type { get; set; }
    [Export]
    public float X { get; set; }
    [Export]
    public float Y { get; set; }
    [Export]
    public Variant Data { get; set; }

}