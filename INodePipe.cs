using System.Collections.Generic;

public interface INodePipe {

    public INodePipe NextPipe { get; }

    public void Register(PipeContext context, string nodeName);
    public void Init();
    public object Pipe(object obj);
    public void Clean();
    public void PipeConnect();
    public void PipeDisconnect();

}