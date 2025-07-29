using System.Collections.Generic;

public interface IReceivePipe : IPipe {

    public List<IReceivePipe> NextPipes { get; }
    public void Register();
    public ICloneablePipeValue Pipe(ICloneablePipeValue pipeValue);
    public void Clean();
    public void PipeDisconnect();

}