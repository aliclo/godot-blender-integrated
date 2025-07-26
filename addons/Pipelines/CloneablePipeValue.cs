using System.Linq;
using Godot;

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
            TouchedProperties = _pipeValue.TouchedProperties.Select(tp => tp.ToArray()).ToList(),
            UntouchedProperties = _pipeValue.UntouchedProperties.Select(up => up.ToArray()).ToList(),
        };

        EmitSignal(SignalName.OnClone, duplicateValue);

        return dupPipeValue;
    } 

}