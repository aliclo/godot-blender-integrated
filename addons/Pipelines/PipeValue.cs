using System.Collections.Generic;
using Godot;

public class PipeValue
{
    public Node Value { get; set; }
    public List<string[]> TouchedProperties { get; set; }
    public List<string[]> UntouchedProperties { get; set; }
}