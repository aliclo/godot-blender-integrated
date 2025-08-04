#if TOOLS
using System.Collections.Generic;
using System.Linq;

public class DestinationHelper
{

    public void AddReceivePipes(PipeContext pipeContext, IList<IReceivePipe> receiversToAdd, ICloneablePipeValue cloneableValue)
    {
        if (cloneableValue != null)
        {
            var valuePipes = receiversToAdd.Select(p => new ValuePipe() { Pipe = p, CloneablePipeValue = cloneableValue }).ToList();
            pipeContext.ReprocessPipe(valuePipes);
        }
    }

    public void RemoveReceivePipes(IList<IReceivePipe> receiversToRemove)
    {
        foreach (var receiver in receiversToRemove)
        {
            receiver.PipeDisconnect();
        }
    }

}
#endif