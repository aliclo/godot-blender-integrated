using Godot;

[Tool]
public partial class PipelineEditor : Control
{

    private PipelineGraph _pipelineGraph;
    public PipelineGraph PipelineGraph => _pipelineGraph;

    public override void _Ready()
    {
        _pipelineGraph = GetChild<PipelineGraph>(0);
    }


}
