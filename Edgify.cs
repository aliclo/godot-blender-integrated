using Godot;
using System;

[Tool]
public partial class Edgify : Node, IReceivePipe
{

    [Export]
    public NodePath Destination { get; set; }
    public IReceivePipe NextPipe => GetNodeOrNull<IReceivePipe>(Destination);

    public void Register(PipeContext context, string nodeName)
    {
        var nextPipe = NextPipe;
        if(nextPipe == null) {
            return;
        }

        nextPipe.Register(context, nodeName);
    }

    public void Init()
    {
        var nextPipe = NextPipe;
        if(nextPipe == null) {
            return;
        }

        nextPipe.Init();
    }

    public object Pipe(object obj)
    {
        if(obj is not MeshInstance3D) {
            return null;
        }

        var meshInstance = (MeshInstance3D) obj;

        return meshInstance;
    }

    public void Clean()
    {
        var nextPipe = NextPipe;
        if(nextPipe == null) {
            return;
        }

        nextPipe.Clean();
    }

    public void PipeDisconnect()
    {
        Clean();
    }

}