using System.Collections.Generic;
using Godot;

public interface IPipe
{

    public StringName Name { get; }
    public List<List<IReceivePipe>> NodeConnections { get; }
    public List<NodePath> NodeDependencies { get; }
    public void Init(PipeContext pipeContext);
    public void AddConnection(int index, List<IReceivePipe> receivePipes);
    public void Connect(int index, List<IReceivePipe> receivePipes);
    public void Disconnect(int index, List<IReceivePipe> receivePipes);
    public void DisposePipe();

}