using Godot;
using System;
using System.Linq;
using System.Reflection;

public partial class BlenderImporterPlusImportPlugin : EditorScenePostImportPlus
{

	public override GodotObject _PostImport(GodotObject godotObject)
    {
        // By design, this just gets the first model to avoid any needed intermediate manual process of extracting each body that's needed
        var scene = (Node) godotObject;
        var mesh = scene.GetChild<MeshInstance3D>(0);
        AnimationPlayer animationPlayer = null;
        if(scene.GetChildren().Count > 1) {
            animationPlayer = scene.GetChild<AnimationPlayer>(1);
        }
        string modelName = mesh.Name;

        Simple3D simple3D;
        Basis meshBasis;

        // No children mean it's just a mesh
        if(mesh.GetChildren().Count == 0) {
            if(animationPlayer == null) {
                scene.RemoveChild(mesh);
                mesh.Owner = null;
                scene.Free();

                return mesh;
            } else {
                simple3D = new Simple3D();
                simple3D.Name = modelName;

                simple3D.Transform = mesh.Transform;

                meshBasis = new Basis(
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1));

                mesh.Transform = new Transform3D(meshBasis, Vector3.Zero);

                scene.RemoveChild(mesh);
                mesh.Owner = null;

                scene.RemoveChild(animationPlayer);
                animationPlayer.Owner = null;

                simple3D.AddChild(mesh);
                mesh.Owner = simple3D;
                mesh.Name = Simple3D.MESH_NAME;

                simple3D.AddChild(animationPlayer);
                animationPlayer.Owner = simple3D;
                animationPlayer.Name = Simple3D.ANIMATION_PLAYER_NAME;

                return simple3D;
            }
        }

        var staticBody = mesh.GetChild<StaticBody3D>(0);
        var collisionShape = staticBody.GetChild<CollisionShape3D>(0);

        if(modelName.EndsWith("-nobody")) {
            mesh.RemoveChild(collisionShape);
            collisionShape.Owner = null;
            scene.RemoveChild(mesh);
            mesh.Owner = null;
            scene.Free();
            mesh.Free();

            string name = modelName.Substring(0, modelName.Length-"-nobody".Length);
            collisionShape.Name = name;

            return collisionShape;
        }

        mesh.RemoveChild(staticBody);
        staticBody.Owner = null;
        scene.RemoveChild(mesh);
        mesh.Owner = null;
        if(animationPlayer != null) {
            scene.RemoveChild(animationPlayer);
            animationPlayer.Owner = null;
        }
        scene.Free();
        

        Transform3D transform = mesh.Transform;

        meshBasis = new Basis(
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1));

        mesh.Transform = new Transform3D(meshBasis, Vector3.Zero);

        PhysicsBody3D body = staticBody;
        Area3D area3D = null;
        CollisionShape3D areaCollisionShape = null;
        bool noMoreFlags = false;

        while(!noMoreFlags) {
            if(modelName.EndsWith("-ani")) {
                modelName = modelName.Substring(0, modelName.Length-"-ani".Length);

                var animatableBody3D = Replace<StaticBody3D, AnimatableBody3D>(staticBody);
                body = animatableBody3D;

                if(animationPlayer == null) {
                    animatableBody3D.SyncToPhysics = false;
                } else {
                    animatableBody3D.SyncToPhysics = true;
                    var animationRef = animationPlayer.GetAnimationList()[0];
                    var animation = animationPlayer.GetAnimation(animationRef);
                    animation.TrackSetPath(0, new NodePath(Simple3D.BODY_NAME));
                }
            } else if(modelName.EndsWith("-rigid")) {
                modelName = modelName.Substring(0, modelName.Length-"-rigid".Length);

                body = Replace<PhysicsBody3D, RigidBody3D>(staticBody);
            } else if (modelName.EndsWith("-area")) {
                modelName = modelName.Substring(0, modelName.Length-"-area".Length);

                area3D = new Area3D();
                area3D.Name = Simple3D.AREA_3D_NAME;
                areaCollisionShape = new CollisionShape3D();
                areaCollisionShape.Name = Simple3D.COLLISION_SHAPE_3D_NAME;
                areaCollisionShape.Shape = collisionShape.Shape;
                area3D.AddChild(areaCollisionShape);
                body.AddChild(area3D);
            } else {
                noMoreFlags = true;
            }
        }

        simple3D = new Simple3D();
        simple3D.Name = modelName;
        simple3D.Transform = transform;
        simple3D.AddChild(body);

        body.AddChild(mesh);
        mesh.Owner = simple3D;
        
        body.Owner = simple3D;
        
        mesh.Name = Simple3D.MESH_NAME;
        body.Name = Simple3D.BODY_NAME;

        if(collisionShape != null) {
            collisionShape.Owner = simple3D;
            collisionShape.Name = Simple3D.COLLISION_SHAPE_3D_NAME;
        }

        if(animationPlayer != null) {
            simple3D.AddChild(animationPlayer);
            animationPlayer.Owner = simple3D;
            animationPlayer.Name = Simple3D.ANIMATION_PLAYER_NAME;
        }

        if(area3D != null) {
            area3D.Owner = simple3D;
            areaCollisionShape.Owner = simple3D;
        }

        return simple3D;
    }

    private TReplaceNode Replace<TOriganalNode, TReplaceNode>(TOriganalNode original)
            where TOriganalNode : Node
            where TReplaceNode : TOriganalNode {
        
        original.Owner = null;

        var replaceNode = Activator.CreateInstance<TReplaceNode>();

        var originalProps = typeof(TOriganalNode)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !p.Name.StartsWith("Global"))
            .ToList();
        
        foreach(var originalProp in originalProps) {
            originalProp.SetValue(replaceNode, originalProp.GetValue(original));
        }

        foreach(var child in original.GetChildren()) {
            original.RemoveChild(child);
            child.Owner = null;
            replaceNode.AddChild(child);
            child.Owner = replaceNode;
        }
        
        original.Free();

        return replaceNode;
    }

}
