using System.Collections.Generic;

public class CloneablePipeValue : ICloneablePipeValue {
    
    
    public PipeValue PipeValue { get; set; }

    public PipeValue ClonePipeValue()
    {
        return new PipeValue()
        {
            Value = PipeValue?.Value?.Duplicate(),
            TouchedProperties = new List<string>(PipeValue.TouchedProperties)
        };
    } 

}