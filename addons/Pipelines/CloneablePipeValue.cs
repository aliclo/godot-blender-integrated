using System.Linq;
using Godot;
using Godot.Collections;

public partial class CloneablePipeValue : GodotObject, ICloneablePipeValue {
    
    
    [Signal]
    public delegate void OnCloneEventHandler(PipeValue value);

    private PipeValue _pipeValue;
    public PipeValue PipeValue
    {
        set
        {
            _pipeValue = value;
        }
    }

    public PipeValue ClonePipeValue()
    {
        var duplicateValue = _pipeValue?.Value?.Duplicate();

        var dupPipeValue = new PipeValue()
        {
            Value = duplicateValue,
            TouchedProperties = new Array<Array<string>>(_pipeValue.TouchedProperties.Select(tp => new Array<string>(tp))),
            UntouchedProperties = new Array<Array<string>>(_pipeValue.UntouchedProperties.Select(up => new Array<string>(up))),
        };

        EmitSignal(SignalName.OnClone, dupPipeValue);

        return dupPipeValue;
    } 

}