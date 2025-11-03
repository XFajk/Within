using Godot;
using System;

public partial class EndCredits : Node2D {
    public override void _Ready() {
        var tween = GetTree().CreateTween();
        tween.TweenCallback(Callable.From(() => {
            SaveSystem.Instance.ResetGame();
            GetTree().ChangeSceneToFile("res://scenes/game_starter.tscn");
        })).SetDelay(62f);
    }
}
