using Godot;
using System;

public partial class MyScript : MeshInstance3D
{

    [Export]
    public string Testing { get; set; }

    [Export]
    public int Another { get; set; }

}
