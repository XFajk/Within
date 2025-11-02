using Godot;
using System;

public partial class AbilityUnlockerInMetro : AbilityUnlocker {

    [Export]
    public Node3D CameraTargetPositionNode;

    [Export]
    public AnimationPlayer AnimationPlayer;

    public override void _Ready() {
        base._Ready();
        BodyEntered += OnBodyEnteredInMetro;
    }
    
    private void OnBodyEnteredInMetro(Node body) {
        if (body is Player player) {
            player.OptionalCameraTargetPosition = CameraTargetPositionNode.GlobalPosition; 
            AnimationPlayer.Play("Metromania");
            var tween = GetTree().CreateTween();
            tween.TweenCallback(Callable.From(() => {
                player.OptionalCameraTargetPosition = null;
            })).SetDelay(6.5f);
        }
    }
}
