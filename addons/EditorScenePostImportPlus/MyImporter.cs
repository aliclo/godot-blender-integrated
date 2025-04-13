using Godot;
using System;
using System.Collections.Generic;

public partial class MyImporter : EditorScenePostImportPlus
{

    [Export]
    public bool Test { get; set; }

    public override GodotObject _PostImport(GodotObject godotObject)
    {
        GD.Print("Test: ", Test);
        return godotObject;
    }

}
