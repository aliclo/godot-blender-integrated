#if TOOLS
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public abstract partial class PipelineNode : GraphNode, IStorable
{

    public static readonly Array<Array<PipelineNode>> EMPTY_NODE_CONNECTIONS = new Array<Array<PipelineNode>>();

    public abstract Array<Array<PipelineNode>> NodeConnections { get; }
    public abstract Array<NodePath> NodeDependencies { get; }
    public abstract void Init(PipeContext pipeContext);
    public abstract void AddConnection(int index, Array<PipelineNode> receivePipes);
    public abstract void Connect(int index, Array<PipelineNode> receivePipes);
    public abstract void Disconnect(int index, Array<PipelineNode> receivePipes);
    
    public abstract Array<PipelineNode> NextPipes { get; }
    public abstract void Register();
    public abstract ICloneablePipeValue PipeValue(ICloneablePipeValue pipeValue);
    public abstract void Clean();
    public abstract void PipeDisconnect();

    public abstract Variant GetData();
    public abstract void Load(Variant data);


}
#endif