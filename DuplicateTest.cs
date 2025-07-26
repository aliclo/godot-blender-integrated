using Godot;
using System;

public partial class DuplicateTest : Node3D
{

    public override void _Ready()
    {
        var sphere = new CsgSphere3D();
        var incomingConnections = sphere.GetSignalList().Count;
        GD.Print("Incoming connections before adding one: ", incomingConnections);
        sphere.TreeEntered += () => GD.Print(sphere, " has entered the scene");
        incomingConnections = sphere.GetSignalList().Count;
        GD.Print("Incoming connections after adding one: ", incomingConnections);
        var dupSphere = sphere.Duplicate();
        AddChild(dupSphere);
    }


}
