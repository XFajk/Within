using Godot;
using System;

public partial class FadeOutRect : ColorRect {
    public override void _Ready() {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, 6.0f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut).SetDelay(0.0f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}

