using System.Collections.Generic;
using System.Linq;

public class CloneablePipeValue : ICloneablePipeValue {
    
    
    public PipeValue PipeValue { get; set; }

    public PipeValue ClonePipeValue()
    {
        return new PipeValue()
        {
            Value = PipeValue?.Value?.Duplicate(),
            TouchedProperties = PipeValue.TouchedProperties.Select(tp => tp.ToArray()).ToList(),
            UntouchedProperties = PipeValue.UntouchedProperties.Select(up => up.ToArray()).ToList()
        };
    } 

}