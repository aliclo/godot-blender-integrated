using System.Collections.Generic;

public interface IReceivePipe : IPipe {

    public List<IReceivePipe> NextPipes { get; }
    public void PreRegistration();
    public void Register(string nodeName);
    public PipeValue Pipe(PipeValue pipeValue);
    public void Clean();
    public void PipeDisconnect();

}