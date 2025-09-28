using Godot;

public partial class DuplicateTest : Node3D
{

    private CsgSphere3D _sphere;

    public override void _Ready()
    {
        _sphere = new CsgSphere3D();
        var incomingConnections = _sphere.GetSignalList().Count;
        GD.Print("Incoming connections before adding one: ", incomingConnections);
        _sphere.TreeEntered += HandleTreeEntered;
        incomingConnections = _sphere.GetSignalList().Count;
        GD.Print("Incoming connections after adding one: ", incomingConnections);
        var dupSphere = _sphere.Duplicate();
        AddChild(dupSphere);
    }

    private void HandleTreeEntered()
    {
        GD.Print(_sphere, " has entered the scene");
        _sphere.TreeEntered -= HandleTreeEntered;
    }


}
