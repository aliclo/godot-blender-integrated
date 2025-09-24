using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class NodePipes : Node
{
    public PipelineNode CurrentNodePipe => Pipes[CurrentProgress];
    public ICloneablePipeValue CurrentValue { get; set; }
    public Array<PipelineNode> Pipes { get; set; }
    public int CurrentProgress { get; set; } = 0;
}
