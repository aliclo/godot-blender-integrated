using System.Collections.Generic;
using Godot;

public abstract partial class PipelineNode : GraphNode, IPipe, IStorable
{

    public static readonly List<List<IReceivePipe>> EMPTY_NODE_CONNECTIONS = new List<List<IReceivePipe>>();

    public abstract List<List<IReceivePipe>> NodeConnections { get; }
    public abstract void Init(PipeContext pipeContext);
    public abstract void AddConnection(int index, List<IReceivePipe> receivePipes);

    public abstract void Connect(int index, List<IReceivePipe> receivePipes);
    
    public abstract void Disconnect(int index, List<IReceivePipe> receivePipes);

    public abstract Variant GetData();

    public abstract void Load(Variant data);

}
