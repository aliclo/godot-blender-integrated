public interface INodePipe {

    public void Init(PipeContext context, string nodeName);
    public void Pipe(object obj);
    public void Clean();
    public void PipeConnect();
    public void PipeDisconnect();

}