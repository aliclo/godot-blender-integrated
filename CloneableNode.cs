using Godot;

public class CloneableNode : ICloneableValue {
    
    
    public Node Node { get; set; }

    public object CloneValue()
    {
        return Node?.Duplicate();
    } 

}