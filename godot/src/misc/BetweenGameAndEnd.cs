using Godot;
using System;

public partial class BetweenGameAndEnd : Control {

    [Export]
    public float WaitTime = 5.0f;

    public Timer _waitTimer = new();

    public override void _Ready() {
        _waitTimer.OneShot = true;
        _waitTimer.WaitTime = WaitTime;
        _waitTimer.Timeout += () => {
            GetTree().ChangeSceneToFile("res://scenes/levels/ending.tscn");
        };
        AddChild(_waitTimer);
        _waitTimer.Start();
    }
}
