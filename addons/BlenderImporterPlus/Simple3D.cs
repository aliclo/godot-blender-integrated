using Godot;
using System;
using static Godot.Animation;

[Tool]
public partial class Simple3D : Node3D
{

    public const string BODY_NAME = nameof(PhysicsBody3D);
    public const string MESH_NAME = nameof(MeshInstance3D);
    public const string COLLISION_SHAPE_3D_NAME = nameof(CollisionShape3D);
    public const string ANIMATION_PLAYER_NAME = nameof(AnimationPlayer);
    public const string AREA_3D_NAME = nameof(Area3D);

    private static readonly NodePath BODY_PATH = new NodePath(BODY_NAME);
    private static readonly NodePath MESH_PATH = new NodePath($"{BODY_NAME}/{MESH_NAME}");
    private static readonly NodePath COLLISION_SHAPE_3D_PATH = new NodePath($"{BODY_NAME}/{COLLISION_SHAPE_3D_NAME}");
    private static readonly NodePath ANIMATION_PLAYER_PATH = new NodePath($"{ANIMATION_PLAYER_NAME}");
    private static readonly NodePath AREA_3D_PATH = new NodePath($"{BODY_NAME}/{AREA_3D_NAME}");

    [Signal]
    public delegate void BodyEnteredEventHandler(Node3D body);

    [Export]
    public LoopModeEnum LoopMode { get; set; }

    [Export]
    public bool AutoPlay { get; set; }

    public PhysicsBody3D Body { get; set; }
    public MeshInstance3D Mesh { get; set; }
    public CollisionShape3D CollisionShape { get; set; }
    public AnimationPlayer AnimationPlayer { get; set; }
    public string AnimationRef { get; set; }
    public Animation Animation { get; set; }
    public Area3D Area { get; set; }

    public override void _Ready()
    {
        Body = GetNodeOrNull<PhysicsBody3D>(BODY_PATH);
        Mesh = GetNodeOrNull<MeshInstance3D>(MESH_PATH);
        CollisionShape = GetNodeOrNull<CollisionShape3D>(COLLISION_SHAPE_3D_PATH);
        AnimationPlayer = GetNodeOrNull<AnimationPlayer>(ANIMATION_PLAYER_PATH);
        Area = GetNodeOrNull<Area3D>(AREA_3D_PATH);

        if(AnimationPlayer != null) {
            AnimationRef = AnimationPlayer.GetAnimationList()[0];
            Animation = AnimationPlayer.GetAnimation(AnimationRef);

            Animation.LoopMode = LoopMode;

            if(AutoPlay) {
                AnimationPlayer.Play(AnimationRef);
            }
        }

        if(Area != null) {
            Area.BodyEntered += (body) => EmitSignal(SignalName.BodyEntered, body);
        }
    }

}
