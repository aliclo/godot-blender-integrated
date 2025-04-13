using Godot;
using System;

public partial class MySecondImporter : EditorScenePostImportPlus
{

    [Export]
    public double SomeNumber { get; set; }

    public override GodotObject _PostImport(GodotObject godotObject)
    {
        GD.Print("SomeNumber: ", SomeNumber);
        return godotObject;
    }

}
