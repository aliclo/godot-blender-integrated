#if TOOLS
using Godot;

public partial class ValuePipe: Node
{
    public PipelineNode Pipe { get; set; }
    public ICloneablePipeValue CloneablePipeValue { get; set; }
}
#endif