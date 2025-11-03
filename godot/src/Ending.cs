using Godot;
using System;

public partial class Ending : Node3D {
    public override void _Ready() {
        base._Ready();
        var tween = GetTree().CreateTween();
        tween.TweenCallback(Callable.From(() => {
            GetTree().ChangeSceneToFile("res://scenes/end_credits.tscn");
            Global.Instance.MusicPlayer.Play();
            Global.Instance.MusicPlayer.VolumeDb = 0f;
            var musicBusIndex = AudioServer.GetBusIndex("Music");
            var lowPassFilterEffect = AudioServer.GetBusEffect(musicBusIndex, 0) as AudioEffectLowPassFilter;
            lowPassFilterEffect.CutoffHz = 20500;
        })).SetDelay(29f);
    }
}
