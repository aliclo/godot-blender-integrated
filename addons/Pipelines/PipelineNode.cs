using Godot;
using System;

public abstract partial class PipelineNode : GraphNode, IStorable
{
    public abstract object GetData();

    public abstract void Load(object data);

}
