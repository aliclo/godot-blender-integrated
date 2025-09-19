using Godot;
using System;

public partial class Another : MeshInstance3D
{

    public override void _EnterTree()
    {
        GD.Print("Entered tree");
    }


}
