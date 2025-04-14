using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Godot.Mesh;

[Tool]
public partial class EdgeDetectPostImport : EditorScenePostImportPlus
{
    
    private class MeshEdge {
        public Vector3 StartVertex { get; set; }
        public Vector3 EndVertex { get; set; }
        public Vector3 FirstFaceVertex { get; set; }
        public Vector3? SecondFaceVertex { get; set; }
    }

    [Export]
    public double ThicknessX { get; set; } = 0.5f;

    [Export]
    public double ThicknessY { get; set; } = 0.5f;

    [Export]
    public double SharpnessThreshold { get; set; } = 30;

    [Export]
    public double MinYAngle { get; set; } = 0;

    [Export]
    public double MaxYAngle { get; set; } = 360;

    [Export]
    public double MinY { get; set; } = -1000;

    [Export]
    public double MaxY { get; set; } = 1000;

    public override GodotObject _PostImport(GodotObject godotObject)
    {
        float sharpnessThresholdRad = (float) (SharpnessThreshold*Math.PI/180);
        float minYAngleRad = (float) (MinYAngle*Math.PI/180-Math.PI);
        float maxYAngleRad = (float) (MaxYAngle*Math.PI/180-Math.PI);

        var node3d = (Node3D) godotObject;
        MeshInstance3D meshInstance;

        if(godotObject is MeshInstance3D meshInstance3d) {
            meshInstance = meshInstance3d;
        } else if (godotObject is Simple3D simple3D) {
            meshInstance = simple3D.Mesh;
        } else {
            return godotObject;
        }
        
        // If we want to get normals, we can use MeshDataTool

        // var arrayMesh = (ArrayMesh) meshInstance.Mesh;
        // var type = arrayMesh.SurfaceGetPrimitiveType(0);
        // GD.Print(type);
        // var mdt = new MeshDataTool();
        // mdt.CreateFromSurface(arrayMesh, 0);

        // var edges = new Dictionary<Vector3, MeshEdge>();

        // for (int fi = 0; fi < mdt.GetFaceCount(); fi++) {
        //     var vi1 = mdt.GetFaceVertex(fi, 0);
        //     var vi2 = mdt.GetFaceVertex(fi, 1);
        //     var vi3 = mdt.GetFaceVertex(fi, 2);

        //     var v1 = mdt.GetVertex(vi1);
        //     var v2 = mdt.GetVertex(vi2);
        //     var v3 = mdt.GetVertex(vi3);

        //     var e1Pos = (v1+v2)/2;

        //     var exists = edges.TryGetValue(e1Pos, out var e1);

        //     if(!exists) {
        //         e1 = new MeshEdge() {
        //             StartVertex = v1,
        //             EndVertex = v2,
        //             FirstFaceVertex = v3,
        //             SecondFaceVertex = null
        //         };

        //         edges[e1Pos] = e1;
        //     } else {
        //         e1.SecondFaceVertex = v3;
        //     }

        //     var e2Pos = (v1+v3)/2;

        //     exists = edges.TryGetValue(e2Pos, out var e2);

        //     if(!exists) {
        //         e2 = new MeshEdge() {
        //             StartVertex = v1,
        //             EndVertex = v3,
        //             FirstFaceVertex = v2,
        //             SecondFaceVertex = null
        //         };

        //         edges[e2Pos] = e2;
        //     } else {
        //         e2.SecondFaceVertex = v2;
        //     }

        //     var e3Pos = (v2+v3)/2;

        //     exists = edges.TryGetValue(e3Pos, out var e3);

        //     if(!exists) {
        //         e3 = new MeshEdge() {
        //             StartVertex = v2,
        //             EndVertex = v3,
        //             FirstFaceVertex = v1,
        //             SecondFaceVertex = null
        //         };

        //         edges[e3Pos] = e3;
        //     } else {
        //         e3.SecondFaceVertex = v1;
        //     }
        // }

        var translate = node3d.Position;
        var transform = node3d.Transform;

        // Get face vertices and transform according to node position
        var faces = meshInstance.Mesh.GetFaces().Select(v => (v+translate)*transform+translate).ToArray();
        
        int numFaces = faces.Length/3;

        // Another way to get edges is with MeshDataTool, however this seemed to also need to be done by faces as some edges would return 1 connected face even if there were 2
        // With triangulated meshes an edge can only have up to 2 faces connected
        var edges = new Dictionary<Vector3, MeshEdge>();
        
        for(int fi = 0; fi < numFaces; fi++) {
            var v1 = faces[fi*3];
            var v2 = faces[fi*3+1];
            var v3 = faces[fi*3+2];

            var e1Pos = (v1+v2)/2;

            var exists = edges.TryGetValue(e1Pos, out var e1);

            if(!exists) {
                e1 = new MeshEdge() {
                    StartVertex = v1,
                    EndVertex = v2,
                    FirstFaceVertex = v3,
                    SecondFaceVertex = null
                };

                edges[e1Pos] = e1;
            } else {
                e1.SecondFaceVertex = v3;
            }

            var e2Pos = (v1+v3)/2;

            exists = edges.TryGetValue(e2Pos, out var e2);

            if(!exists) {
                e2 = new MeshEdge() {
                    StartVertex = v1,
                    EndVertex = v3,
                    FirstFaceVertex = v2,
                    SecondFaceVertex = null
                };

                edges[e2Pos] = e2;
            } else {
                e2.SecondFaceVertex = v2;
            }

            var e3Pos = (v2+v3)/2;

            exists = edges.TryGetValue(e3Pos, out var e3);

            if(!exists) {
                e3 = new MeshEdge() {
                    StartVertex = v2,
                    EndVertex = v3,
                    FirstFaceVertex = v1,
                    SecondFaceVertex = null
                };

                edges[e3Pos] = e3;
            } else {
                e3.SecondFaceVertex = v1;
            }
        }

        var sharpEdges = edges.Values.Where(edge => {
            var center = (edge.StartVertex + edge.EndVertex)/2;
            if(center.Y < MinY || center.Y > MaxY) {
                return false;
            }

            // No face means it's super sharp!
            if(edge.SecondFaceVertex == null) {
                return true;
            }
            
            // Rotate so that the edge is along the Z axis (Y in Blender), this allows us to find the angle with only X and Y (Z in Blender) axis
            var direction = edge.EndVertex-edge.StartVertex;
            var xRot = Math.Atan2(direction.Y, direction.Z);
            direction = direction.Rotated(new Vector3(1, 0, 0), (float) xRot);
            var yRot = Math.Atan2(direction.X, direction.Z);

            if(yRot < minYAngleRad || yRot > maxYAngleRad) {
                return false;
            }

            // var basis = new Basis(new Vector3(0, 1, 0), (float) -yRot);
            // basis.Rotated(new Vector3(1, 0, 0), (float) -xRot);

            // Make everything relative to StartVertex and then rotate
            var e1 = edge.FirstFaceVertex-edge.StartVertex;
            var e2 = edge.SecondFaceVertex.Value-edge.StartVertex;

            // Rotate +X and -Y to align edge with Z
            e1 = e1.Rotated(new Vector3(1, 0, 0), (float) xRot).Rotated(new Vector3(0, 1, 0), (float) -yRot);
            e2 = e2.Rotated(new Vector3(1, 0, 0), (float) xRot).Rotated(new Vector3(0, 1, 0), (float) -yRot);

            // Only X and Y components needed after rotation, Z tells nothing about sharpness at this point
            var e1xy = new Vector2(e1.X, e1.Y);
            var e2xy = new Vector2(e2.X, e2.Y);
            var angle = Math.Abs(e1xy.AngleTo(e2xy));

            return angle > sharpnessThresholdRad && angle < (Math.PI-sharpnessThresholdRad);
        }).ToList();

        var edgeMeshsVertices = sharpEdges.Select(sharpEdge => {
            var direction = sharpEdge.StartVertex.DirectionTo(sharpEdge.EndVertex);
            var xRot = Math.Atan2(direction.Y, direction.Z);
            direction = direction.Rotated(new Vector3(1, 0, 0), (float) -xRot);
            var yRot = Math.Atan2(direction.X, direction.Z);

            var topLeft = new Vector3((float) (-1*ThicknessX), (float) (-1*ThicknessY), 0).Rotated(new Vector3(0, 1, 0), (float) yRot).Rotated(new Vector3(1, 0, 0), (float) xRot);
            var topRight = new Vector3((float) (1*ThicknessX), (float) (-1*ThicknessY), 0).Rotated(new Vector3(0, 1, 0), (float) yRot).Rotated(new Vector3(1, 0, 0), (float) xRot);
            var bottomLeft = new Vector3((float) (-1*ThicknessX), (float) (1*ThicknessY), 0).Rotated(new Vector3(0, 1, 0), (float) yRot).Rotated(new Vector3(1, 0, 0), (float) xRot);
            var bottomRight = new Vector3((float) (1*ThicknessX), (float) (1*ThicknessY), 0).Rotated(new Vector3(0, 1, 0), (float) yRot).Rotated(new Vector3(1, 0, 0), (float) xRot);

            var vertices = new List<Vector3>
            {
                sharpEdge.StartVertex + topLeft,
                sharpEdge.EndVertex + topLeft,
                sharpEdge.StartVertex + topRight,
                sharpEdge.EndVertex + topRight,
                sharpEdge.StartVertex + bottomRight,
                sharpEdge.EndVertex + bottomRight,
                sharpEdge.StartVertex + bottomLeft,
                sharpEdge.EndVertex + bottomLeft,
                sharpEdge.StartVertex + topLeft,
                sharpEdge.EndVertex + topLeft
            };

            return vertices;
        });


        var triangleStrips = new List<Vector3>();


        var edgeMeshVerticesEnumerator = edgeMeshsVertices.GetEnumerator();

        edgeMeshVerticesEnumerator.MoveNext();
        var edgeMeshVertices = edgeMeshVerticesEnumerator.Current;
        edgeMeshVerticesEnumerator.MoveNext();
        var nextEdgeMeshVertex = edgeMeshVerticesEnumerator.Current;

        while(nextEdgeMeshVertex != null) {
            triangleStrips.AddRange(edgeMeshVertices);

            var lastVertex = edgeMeshVertices.Last();
            var nextVertex = nextEdgeMeshVertex.First();

            triangleStrips.Add(lastVertex);
            triangleStrips.Add(nextVertex);

            edgeMeshVertices = nextEdgeMeshVertex;
            edgeMeshVerticesEnumerator.MoveNext();
            nextEdgeMeshVertex = edgeMeshVerticesEnumerator.Current;
        }

        triangleStrips.AddRange(edgeMeshVertices);


        var vertices = triangleStrips.ToArray();

        // Initialize the ArrayMesh.
        var arrMesh = new ArrayMesh();
        Godot.Collections.Array arrays = new Godot.Collections.Array {};
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;

        // Create the Mesh.
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.TriangleStrip, arrays);
        var edgeMeshInstance = new MeshInstance3D();
        edgeMeshInstance.Mesh = arrMesh;
        edgeMeshInstance.Name = "Edges";

        node3d.AddChild(edgeMeshInstance);
        edgeMeshInstance.Owner = node3d;


        return node3d;


        // var immediateMesh = new ImmediateMesh();

        // immediateMesh.SurfaceBegin(PrimitiveType.Triangles);

        // immediateMesh.SurfaceAddVertex(new Vector3(0, 0, 0));
        // immediateMesh.SurfaceAddVertex(new Vector3(10, 0, 0));
        // immediateMesh.SurfaceAddVertex(new Vector3(10, 10, 0));

        // immediateMesh.SurfaceEnd();

        // var edgeMeshInstance = new MeshInstance3D();
        // edgeMeshInstance.Name = "Edges";
        // edgeMeshInstance.Mesh = immediateMesh;
        // // node3d.AddChild(edgeMeshInstance);
        // // edgeMeshInstance.Owner = node3d;

        // foreach(var vertex in edgeMeshInstance.Mesh.GetFaces()) {
        //     GD.Print(vertex);
        // }

        // // return node3d;
        // return edgeMeshInstance;
    }
}
