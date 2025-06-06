using Godot;

public partial class PipelineConnectionStore: GodotObject {
    [Export]
    public string FromNodeName { get; set; }
    [Export]
    public int FromPort { get; set; }
    [Export]
    public string ToNodeName { get; set; }
    [Export]
    public int ToPort { get; set; }
}