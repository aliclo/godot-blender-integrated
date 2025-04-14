using Godot;
using System;
using static Godot.Mesh;

[Tool]
public partial class MyMesh : Node3D
{

    [Export]
    public bool Test {
        get {
            return true;
        }
        set {
            Draw();
        }
    }

    public void Draw() {
        var meshInstance = new MeshInstance3D();

        var immediateMesh = new ImmediateMesh();

        immediateMesh.SurfaceBegin(PrimitiveType.Triangles);

        immediateMesh.SurfaceAddVertex(new Vector3(0, 0, 0));
        immediateMesh.SurfaceAddVertex(new Vector3(10, 0, 0));
        immediateMesh.SurfaceAddVertex(new Vector3(20, 10, 0));

        immediateMesh.SurfaceEnd();

        var count = GetChildCount();

        meshInstance.Name = $"Mesh instance {count}";
        meshInstance.Mesh = immediateMesh;
        AddChild(meshInstance);
        meshInstance.Owner = GetParent();
    }


}
