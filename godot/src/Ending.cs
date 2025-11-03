using Godot;
using System;

public partial class Ending : Node3D {
    public override void _Ready() {
        base._Ready();
        var tween = GetTree().CreateTween();
        tween.TweenCallback(Callable.From(() => {
            GetTree().ChangeSceneToFile("res://scenes/end_credits.tscn");
            Global.Instance.MusicPlayer.Play();
            Global.Instance.MusicPlayer.VolumeDb = 0;
        })).SetDelay(29f);
    }
}
