using System.Collections.Generic;
using Godot;

public class PipeContext {

    public Node RootNode { get; set; }
    public List<NodeOutput> OrderOfCreation { get; set; }
    public Dictionary<string, object> ContextData { get; set; }
    
}