using Godot;

public partial class MyNestedNumbers : Node
{
    [Export]
    public MyNestedNumbers Nested { get; set; }

    [Export]
    public int Value { get; set; }
}