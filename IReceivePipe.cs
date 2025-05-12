using System.Collections.Generic;

public interface IReceivePipe : IPipe {

    public List<IReceivePipe> NextPipes { get; }
    public void Register(PipeContext context, string nodeName);
    public PipeValue Pipe(PipeValue pipeValue);
    public void Init();
    public void Clean();
    public void PipeDisconnect();

}