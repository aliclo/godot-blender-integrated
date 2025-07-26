using System.Collections.Generic;
using Godot;

public partial class PipeValue: GodotObject
{
    public Node Value { get; set; }
    public List<string[]> TouchedProperties { get; set; }
    public List<string[]> UntouchedProperties { get; set; }
}