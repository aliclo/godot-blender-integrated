#if TOOLS
using Godot;

public partial class NodeDependency : Node
{
    public PipelineNode Node { get; set; }
    public PipelineNode Dependency { get; set; }
}
#endif