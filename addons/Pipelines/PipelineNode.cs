using Godot;

public abstract partial class PipelineNode : GraphNode, IStorable
{
    public abstract Variant GetData();

    public abstract void Load(Variant data);

}
