using Godot;
using System;

public partial class GroundZero : Level {
    public override void _Ready() {
        base._Ready();
        Global.Instance.SwitchMusic("GroundZero");
        var musicBusIndex = AudioServer.GetBusIndex("Music");
        var lowPassFilterEffect = AudioServer.GetBusEffect(musicBusIndex, 0) as AudioEffectLowPassFilter;
        var musicTween = GetTree().CreateTween();
        musicTween.SetPauseMode(Tween.TweenPauseMode.Process);
        musicTween.TweenProperty(lowPassFilterEffect, "cutoff_hz", 20500, 1.0);
    }
}
