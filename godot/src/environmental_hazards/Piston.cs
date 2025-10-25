using Godot;
using System;

public partial class Piston : Node3D {
    [Export]
    public bool Broken = false;

    [Export]
    public float StartDelay = 0.0f;

    [Export]
    public float CrushPeriod = 5.0f;

    [Export]
    public float CrushDuration = 1.0f;

    private Area3D _killArea;

    private Timer _switchTimer;

    private PackedScene _brokenSparks = GD.Load<PackedScene>("res://scenes/VFX/sparks.tscn");

    private bool _isActive = false;

    public override void _Ready() {
        _killArea = GetNode<Area3D>("KillArea");

        _killArea.Position = new Vector3(0, 0, 1000f);

        _switchTimer = new Timer();
        _switchTimer.Timeout += () => {
            if (_isActive) {
                // Turn off
                _killArea.Position = new Vector3(0, 0, 1000f);
                _switchTimer.Start(CrushPeriod);

                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(this, "position", new Vector3(Position.X, -0.39f, Position.Z), CrushDuration);

            } else {
                // Turn on
                if (!Broken)
                    _killArea.Position = Vector3.Zero;
                else {
                    var sparks = _brokenSparks.Instantiate<Node3D>();
                    sparks.Position = new Vector3(0, -0.5f, 0);
                    AddChild(sparks);
                }
                _switchTimer.Start(CrushDuration);

                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(this, "position", new Vector3(Position.X, !Broken ? -1.2f : -0.5f, Position.Z), CrushDuration / 4);

            }
            _isActive = !_isActive;
        };
        AddChild(_switchTimer);
        _switchTimer.Start(StartDelay);
    }

}
