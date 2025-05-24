using Godot;
using Godot.Collections;

public partial class PipelineContextStore: GodotObject
{
    [Export]
    public string Name { get; set; }
    [Export]
    public Array<PipelineNodeStore> Nodes { get; set; }
    [Export]
    public Array<PipelineConnection> Connections { get; set; }
}