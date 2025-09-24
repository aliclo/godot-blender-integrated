using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class PipeValue: GodotObject
{
    public Node Value { get; set; }
    public Array<Array<string>> TouchedProperties { get; set; }
    public Array<Array<string>> UntouchedProperties { get; set; }
}