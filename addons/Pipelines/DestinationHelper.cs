#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public class DestinationHelper
{

    public void AddReceivePipes(PipeContext pipeContext, IList<PipelineNode> receiversToAdd, ICloneablePipeValue cloneableValue)
    {
        if (cloneableValue != null)
        {
            var valuePipes = new Array<ValuePipe>(receiversToAdd.Select(p => new ValuePipe() { Pipe = p, CloneablePipeValue = cloneableValue }));
            pipeContext.ReprocessPipe(valuePipes);
        }
    }

    public void RemoveReceivePipes(IList<PipelineNode> receiversToRemove)
    {
        foreach (var receiver in receiversToRemove)
        {
            receiver.PipeDisconnect();
        }
    }

}
#endif