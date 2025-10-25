using Godot;
using System;

public partial class PulsatingSteam : Node3D {

    [Export]
    public float StartDelay = 2.0f;

    [Export]
    public float PeriodOn = 5.0f;

    [Export]
    public float PeriodOff = 5.0f;

    private GpuParticles3D _steamParticles;
    private EnvironmentalHazard _steamArea;

    private Timer _switchTimer;

    private bool _isActive = false;

    public override void _Ready() {
        _steamParticles = GetNode<GpuParticles3D>("SteamParticles");
        _steamArea = GetNode<EnvironmentalHazard>("SteamArea");

        _steamParticles.Emitting = false;
        _steamArea.Position = new Vector3(0, 0, 1000f);

        _switchTimer = new Timer();
        _switchTimer.Timeout += () => {
            if (_isActive) {
                // Turn off
                _steamParticles.Emitting = false;
                _steamArea.Position = new Vector3(0, 0, 1000f);
                _switchTimer.Start(PeriodOff);
            } else {
                // Turn on
                _steamParticles.Emitting = true;
                _steamArea.Position = Vector3.Zero;
                _switchTimer.Start(PeriodOn);
            }
            _isActive = !_isActive;
        };
        AddChild(_switchTimer);
        _switchTimer.Start(StartDelay);
    }

}
