using Godot;
using Godot.Collections;

public class DestinationPropertyInfo {
    public PipeContext PipeContext { get; set; }
    public Node Node { get; set; }
    public string DestinationNodeName { get; set; }
    public Array<NodePath> PreviousDestinationPaths { get; set; }
    public Array<NodePath> NewDestinationPaths { get; set; }
    public ICloneablePipeValue CloneableValue { get; set; }
}