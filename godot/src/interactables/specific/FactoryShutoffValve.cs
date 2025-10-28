using Godot;
using Godot.Collections;
using System;

public partial class FactoryShutoffValve : Interactable, ISavable {

    [Export]
    public Array<Piston> PistonsToTurnOff = new();

    [Export]
    public Array<PulsatingSteam> SteamToTurnOff = new();

    private Node3D _valveMesh; 

    private bool _isTurnedOff = false;

    public override void _Ready() {
        base._Ready();
        _valveMesh = GetNode<Node3D>("ValveMesh");
        AddToGroup("Savable");
    }

    public override void _Process(double delta) {
        if (_isTurnedOff) {
            _player = null;
        }

        base._Process(delta);
    }

    protected override void Interact() {


        foreach (var piston in PistonsToTurnOff) {
            piston.Enabled = false;
        }

        foreach (var steam in SteamToTurnOff) {
            steam.Enabled = false;
        }

        var tween = GetTree().CreateTween();
        tween.TweenProperty(_valveMesh, "rotation_degrees:z", 90.0f, 3.0f);
        _isTurnedOff = true;
        _player = null;
    }

    public string GetSaveID() {
        return GetPath();
    }

    public Dictionary<string, Variant> SaveState() {
        var state = new Dictionary<string, Variant>();
        state["isTurnedOff"] = _isTurnedOff;
        return state;
    }
    
    public void LoadState(Dictionary<string, Variant> state) {
        GD.Print("Loading");
        if (state.ContainsKey("isTurnedOff")) {
            _isTurnedOff = (bool)state["isTurnedOff"];
            if (_isTurnedOff) {
                foreach (var piston in PistonsToTurnOff) {
                    piston.Enabled = false;
                }

                foreach (var steam in SteamToTurnOff) {
                    steam.Enabled = false;
                }

                _valveMesh.RotationDegrees = new Vector3(
                    _valveMesh.RotationDegrees.X,
                    _valveMesh.RotationDegrees.Y,
                    360.0f
                );
            }
        }
    }
}