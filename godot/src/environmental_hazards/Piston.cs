using Godot;
using System;

public partial class Piston : Node3D {
    private bool _enabled = true;

    [Export]
    public bool Enabled = true;

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

    private AudioStreamPlayer3D _pistonSound;

    public override void _Ready() {
        _killArea = GetNode<Area3D>("KillArea");

        _killArea.Position = new Vector3(0, 0, 1000f);

        _pistonSound = GetNode<AudioStreamPlayer3D>("PistonSound");

        _switchTimer = new Timer();
        _switchTimer.Timeout += () => {

            if (_isActive) {
                // Turn off
                _killArea.Position = new Vector3(0, 0, 1000f);
                if (Enabled)
                    _switchTimer.Start(CrushPeriod);
                else
                    _switchTimer.Stop();

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
                tween.TweenCallback(Callable.From(() => {
                    if (!Broken) _pistonSound.Play();
                }));

            }
            _isActive = !_isActive;
        };
        AddChild(_switchTimer);
        CallDeferred(nameof(StartTimer));
    }

    private void StartTimer() {
        if (Enabled)
            _switchTimer.Start(StartDelay);
    }

}
